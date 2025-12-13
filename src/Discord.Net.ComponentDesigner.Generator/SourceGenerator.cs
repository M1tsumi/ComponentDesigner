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
using System.Linq;
using System.Text;
using System.Threading;

namespace Discord.CX;

[Generator]
public sealed class SourceGenerator : IIncrementalGenerator
{
    private readonly Dictionary<string, CXGraphManager> _cache = [];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                IsComponentDesignerCall,
                MapPossibleComponentDesignerCall
            )
            .Collect();

        context.RegisterSourceOutput(
            provider
                .Combine(provider.Select(GetKeysAndUpdateCachedEntries))
                .Combine(GeneratorOptions.CreateProvider(context))
                .SelectMany(MapManagers)
                .Select((manager, token) => manager.Render(token))
                .Collect(),
            Generate
        );
    }

    private void Generate(SourceProductionContext context, ImmutableArray<RenderedInterceptor> interceptors)
    {
        if (interceptors.Length is 0) return;

        var sb = new StringBuilder();

        for (var i = 0; i < interceptors.Length; i++)
        {
            var interceptor = interceptors[i];
            foreach (var diagnostic in interceptor.Diagnostics)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        diagnostic.Descriptor,
                        interceptor.SyntaxTree.GetLocation(diagnostic.Span)
                    )
                );
            }

            if (i > 0)
                sb.AppendLine();

            var parameter = interceptor.UsesDesigner
                ? $"global::{Constants.COMPONENT_DESIGNER_QUALIFIED_NAME} designer"
                : "global::System.String cx";

            sb.AppendLine(
                $$"""
                  /*
                  {{interceptor.Location}}

                  {{interceptor.CX.NormalizeIndentation()}}
                  */
                  [global::System.Runtime.CompilerServices.InterceptsLocation(version: {{interceptor.InterceptLocation.Version}}, data: "{{interceptor.InterceptLocation.Data}}")]
                  public static global::Discord.CXMessageComponent _{{Math.Abs(interceptor.GetHashCode())}}(
                      {{parameter}}
                  ) => new global::Discord.CXMessageComponent([
                      {{interceptor.Source.WithNewlinePadding(4)}}
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

    private IEnumerable<CXGraphManager> MapManagers(
        ((ImmutableArray<ComponentDesignerTarget?>, ImmutableArray<string?>), GeneratorOptions) tuple,
        CancellationToken token
    )
    {
        var ((targets, keys), options) = tuple;

        for (var i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            var key = keys[i];

            if (target is null || key is null) continue;

            // TODO: handle key updates

            if (_cache.TryGetValue(key, out var manager))
            {
                manager = _cache[key] = manager.OnUpdate(key, target, options, token);
            }
            else
            {
                manager = _cache[key] = CXGraphManager.Create(
                    this,
                    key,
                    target,
                    options,
                    token
                );
            }

            yield return manager;
        }
    }

    private ImmutableArray<string?> GetKeysAndUpdateCachedEntries(
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

        foreach (var key in _cache.Keys.Except(result))
        {
            if (key is not null) _cache.Remove(key);
        }

        return [..result];
    }

    private static ComponentDesignerTarget? MapPossibleComponentDesignerCall(GeneratorSyntaxContext context,
        CancellationToken token)
        => MapPossibleComponentDesignerCall(context.SemanticModel, context.Node, token);

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
            semanticModel.Compilation,
            invocationSyntax.SyntaxTree,
            interceptLocation,
            semanticModel.GetEnclosingSymbol(invocationSyntax.SpanStart, token)
                ?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            cxDesigner,
            locationInfo,
            [..interpolationInfos],
            operation.TargetMethod.Parameters[0].Type.SpecialType is not SpecialType.System_String,
            quoteCount
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