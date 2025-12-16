using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Discord.CX.Util;
using Discord.CX.Utils;

namespace Discord.CX;

[Generator]
public sealed class SourceGenerator : IIncrementalGenerator
{
    internal sealed class Glue(
        string key,
        InterceptableLocation interceptLocation,
        LocationInfo location,
        bool usesDesigner,
        SyntaxTree syntaxTree
    ) : IEquatable<Glue>
    {
        public string Key { get; init; } = key;
        public InterceptableLocation InterceptLocation { get; init; } = interceptLocation;
        public LocationInfo Location { get; init; } = location;
        public bool UsesDesigner { get; init; } = usesDesigner;
        public SyntaxTree SyntaxTree { get; init; } = syntaxTree;

        public bool Equals(Glue other)
            => Key == other.Key &&
               InterceptLocation.Equals(other.InterceptLocation) &&
               Location.Equals(other.Location) &&
               UsesDesigner == other.UsesDesigner;

        public override bool Equals(object? obj)
            => obj is Glue other && Equals(other);

        public override int GetHashCode()
            => Hash.Combine(Key, InterceptLocation, Location, UsesDesigner);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targetProvider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsComponentDesignerCall,
                MapPossibleComponentDesignerCall
            )
            .WithTrackingName(TrackingNames.INITIAL_TARGET)
            .Collect();

        var keyMapperProvider = targetProvider
            .Select(GetKeys)
            .WithTrackingName(TrackingNames.MAP_KEYS);

        var combinedKeys = targetProvider
            .Combine(keyMapperProvider);

        var glueProvider =
            combinedKeys
                .SelectMany(CreateClue)
                .WithTrackingName(TrackingNames.CREATE_GLUE);

        var graphProvider = combinedKeys
            .Combine(GeneratorOptions.CreateProvider(context))
            .SelectMany(CreateGraphState)
            .WithTrackingName(TrackingNames.CREATE_GRAPH_STATE)
            .Select(CreateGraph)
            .WithTrackingName(TrackingNames.CREATE_GRAPH);

        // inject compilation
        graphProvider = graphProvider
            .Combine(context.CompilationProvider)
            .Select(UpdateFromCompilation)
            .WithTrackingName(TrackingNames.UPDATE_GRAPH_STATE);

        var finalProvider = graphProvider
            .Select(RenderGraph)
            .WithTrackingName(TrackingNames.RENDER_GRAPH)
            .Collect()
            .Combine(glueProvider.Collect())
            .WithTrackingName(TrackingNames.RENDER_AND_GLUE_EMIT);

        var graph = finalProvider.ToDOTTree();

        context.RegisterSourceOutput(
            finalProvider,
            Generate
        );
    }

    internal static IEnumerable<Glue> CreateClue(
        (ImmutableArray<ComponentDesignerTarget?> Targets, ImmutableArray<string?> Keys) tuple,
        CancellationToken token
    )
    {
        var (targets, keys) = tuple;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var key = keys[i];

            if (target is null || key is null) continue;

            yield return new Glue(
                key,
                target.InterceptLocation,
                target.CX.Location,
                target.CX.UsesDesignerParameter,
                target.SyntaxTree
            );
        }
    }

    internal static RenderedGraph RenderGraph(CXGraph graph, CancellationToken token)
        => graph.Render(token: token);

    internal static CXGraph UpdateFromCompilation(
        (CXGraph Graph, Compilation compilation) tuple,
        CancellationToken token
    ) => tuple.Graph.UpdateFromCompilation(tuple.compilation, token);

    internal static CXGraph CreateGraph(GraphGeneratorState state, CancellationToken token)
    {
        // TODO: incremental parsing
        return CXGraph.Create(state, old: null, token);
    }

    internal static IEnumerable<GraphGeneratorState> CreateGraphState(
        ((ImmutableArray<ComponentDesignerTarget?> Left, ImmutableArray<string?> Right) Left, GeneratorOptions Right)
            tuple,
        CancellationToken token)
    {
        var ((targets, keys), options) = tuple;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var key = keys[i];

            if (target is null || key is null) continue;

            yield return CreateGraphState(key, target, options);
        }
    }

    internal static GraphGeneratorState CreateGraphState(
        string key,
        ComponentDesignerTarget target,
        GeneratorOptions options
    ) => new(
        key,
        options,
        target.CX
    );

    internal void Generate(
        SourceProductionContext context,
        (ImmutableArray<RenderedGraph> Renders, ImmutableArray<Glue> Glues) tuple
    )
    {
        var (renders, glues) = tuple;

        if (renders.IsEmpty) return;

        Debug.Assert(renders.Length == glues.Length);

        var sb = new StringBuilder();

        for (var i = 0; i < renders.Length; i++)
        {
            var render = renders[i];
            var glue = glues[i];

            Debug.Assert(render.Key == glue.Key);

            foreach (var diagnostic in render.Diagnostics)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        diagnostic.Descriptor,
                        glue.SyntaxTree.GetLocation(diagnostic.Span)
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(render.EmittedSource)) continue;

            if (i > 0)
                sb.AppendLine();

            var parameter = glue.UsesDesigner
                ? $"global::{Constants.COMPONENT_DESIGNER_QUALIFIED_NAME} designer"
                : "global::System.String cx";

            sb.AppendLine(
                $$"""
                  /*
                  {{glue.Location}}

                  {{render.CX.NormalizeIndentation()}}
                  */
                  [global::System.Runtime.CompilerServices.InterceptsLocation(version: {{glue.InterceptLocation.Version}}, data: "{{glue.InterceptLocation.Data}}")]
                  public static global::Discord.CXMessageComponent _{{Math.Abs(glue.InterceptLocation.GetHashCode())}}(
                      {{parameter}}
                  ) => new global::Discord.CXMessageComponent([
                      {{render.EmittedSource!.WithNewlinePadding(4)}}
                  ]);
                  """
            );
        }

        context.AddSource(
            "Interceptors.g.cs",
            $$"""
              using Discord;

              namespace System.Runtime.CompilerServices
              {
                  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
                  sealed file class InterceptsLocationAttribute(int version, string data) : Attribute;
              }

              namespace InlineComponent
              {
                  static file class Interceptors
                  {
                      {{sb.ToString().WithNewlinePadding(8)}}
                  }
              }
              """
        );
    }

    private static ImmutableArray<string?> GetKeys(
        ImmutableArray<ComponentDesignerTarget?> target,
        CancellationToken token
    )
    {
        var result = new string?[target.Length];

        var map = new Dictionary<string, int>();
        var globalCount = 0;

        for (var i = 0; i < target.Length; i++)
        {
            var targetItem = target[i];

            if (targetItem is null) continue;

            string key;
            if (targetItem.ParentKey is null)
            {
                key = $"<global>:{globalCount++}";
            }
            else
            {
                map.TryGetValue(targetItem.ParentKey, out var index);

                key = $"{targetItem.ParentKey}:{index}";
                map[targetItem.ParentKey] = index + 1;
            }

            result[i] = key;
        }

        return [..result];
    }

    private static ComponentDesignerTarget? MapPossibleComponentDesignerCall(
        GeneratorSyntaxContext context,
        CancellationToken token
    ) => MapPossibleComponentDesignerCall(context.SemanticModel, context.Node, token);

    public static ComponentDesignerTarget? MapPossibleComponentDesignerCall(
        SemanticModel semanticModel,
        SyntaxNode node,
        CancellationToken token
    )
    {
        if (
            !TryGetValidDesignerCall(
                out var operation,
                out var invocationSyntax,
                out var interceptLocation,
                out var argumentSyntax
            )
        ) return null;

        if (
            !TryGetCXDesigner(
                argumentSyntax,
                semanticModel,
                out var cxDesigner,
                out var locationInfo,
                out var interpolationInfos,
                out var quoteCount,
                token
            )
        ) return null;


        return new ComponentDesignerTarget(
            interceptLocation,
            semanticModel.GetEnclosingSymbol(invocationSyntax.SpanStart, token)
                ?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            new CXDesignerGeneratorState(
                cxDesigner,
                locationInfo,
                quoteCount,
                operation.TargetMethod.Parameters[0].Type.SpecialType is not SpecialType.System_String,
                [..interpolationInfos],
                semanticModel,
                invocationSyntax.SyntaxTree
            )
        );

        static bool TryGetCXDesigner(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            out string content,
            out LocationInfo locationInfo,
            out DesignerInterpolationInfo[] interpolations,
            out int quoteCount,
            CancellationToken token
        )
        {
            switch (expression)
            {
                case LiteralExpressionSyntax { Token.Text: { } literalContent } literal:
                    content = PrepareRawLiteral(
                        literalContent,
                        out var startQuoteCount,
                        out var endQuoteCount
                    );

                    quoteCount = startQuoteCount;
                    interpolations = [];
                    locationInfo = LocationInfo.CreateFrom(
                        expression.SyntaxTree.GetLocation(
                            TextSpan.FromBounds(
                                literal.Token.Span.Start + startQuoteCount,
                                literal.Token.Span.End - endQuoteCount
                            )
                        )
                    )!;
                    return true;

                case InterpolatedStringExpressionSyntax interpolated:
                    content = interpolated.Contents.ToString();
                    interpolations = interpolated.Contents
                        .OfType<InterpolationSyntax>()
                        .Select((x, i) =>
                        {
                            var typeInfo = semanticModel.GetTypeInfo(x.Expression, token);

                            return new DesignerInterpolationInfo(
                                i,
                                x.FullSpan,
                                typeInfo.Type ?? typeInfo.ConvertedType,
                                semanticModel.GetConstantValue(x.Expression, token)
                            );
                        })
                        .ToArray();
                    locationInfo = LocationInfo.CreateFrom(
                        expression.SyntaxTree.GetLocation(interpolated.Contents.Span)
                    )!;
                    quoteCount = interpolated.StringEndToken.Span.Length;
                    return true;
                default:
                    content = string.Empty;
                    locationInfo = null!;
                    interpolations = [];
                    quoteCount = 0;
                    return false;
            }
        }

        static string PrepareRawLiteral(
            string literal,
            out int startQuoteCount,
            out int endQuoteCount
        )
        {
            for (startQuoteCount = 0; startQuoteCount < literal.Length; startQuoteCount++)
            {
                if (literal[startQuoteCount] is not '"') break;
            }

            endQuoteCount = 0;
            if (literal.Length == startQuoteCount)
            {
                return string.Empty;
            }

            for (var i = literal.Length - 1; i >= startQuoteCount; i--, endQuoteCount++)
                if (literal[i] is not '"')
                    break;

            return literal.Substring(
                startQuoteCount, literal.Length - startQuoteCount - endQuoteCount
            );
        }

        bool TryGetValidDesignerCall(
            out IInvocationOperation operation,
            out InvocationExpressionSyntax invocationSyntax,
            out InterceptableLocation interceptLocation,
            out ExpressionSyntax argumentExpressionSyntax
        )
        {
            var localOperation = semanticModel.GetOperation(node, token)!;
            interceptLocation = null!;
            argumentExpressionSyntax = null!;
            invocationSyntax = null!;

            checkOperation:
            switch (localOperation)
            {
                case IInvalidOperation invalid:
                    localOperation = invalid.ChildOperations.OfType<IInvocationOperation>().FirstOrDefault()!;
                    goto checkOperation;
                case IInvocationOperation invocation:
                    if (
                        invocation
                            .TargetMethod
                            .ContainingType
                            .ToDisplayString()
                        is "Discord.ComponentDesigner"
                    )
                    {
                        operation = invocation;
                        break;
                    }

                    goto default;

                default:
                {
                    operation = null!;
                    return false;
                }
            }

            if (node is not InvocationExpressionSyntax syntax) return false;

            invocationSyntax = syntax;

            if (semanticModel.GetInterceptableLocation(invocationSyntax, token) is not { } location)
                return false;

            interceptLocation = location;

            if (invocationSyntax.ArgumentList.Arguments.Count is not 1) return false;

            argumentExpressionSyntax = invocationSyntax.ArgumentList.Arguments[0].Expression;

            return true;
        }
    }

    private static bool IsComponentDesignerCall(SyntaxNode node, CancellationToken token)
        => node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name: { Identifier.Value: "Create" or "cx" }
            } or IdentifierNameSyntax
            {
                Identifier.ValueText: "cx"
            }
        };
}