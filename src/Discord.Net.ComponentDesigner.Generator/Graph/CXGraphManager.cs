using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;

namespace Discord.CX;

public sealed record CXGraphManager(
    SourceGenerator Generator,
    string Key,
    Target Target,
    CXDoc Document
)
{
    public const bool FORCE_NO_INCREMENTAL = true;
    public const bool ALWAYS_REPARSE = true;

    public SyntaxTree SyntaxTree => InvocationSyntax.SyntaxTree;
    public InterceptableLocation InterceptLocation => Target.InterceptLocation;
    public InvocationExpressionSyntax InvocationSyntax => Target.InvocationSyntax;
    public ExpressionSyntax ArgumentExpressionSyntax => Target.ArgumentExpressionSyntax;
    public IInvocationOperation Operation => Target.Operation;
    public Compilation Compilation => Target.Compilation;

    public string CXDesigner => Target.CXDesigner;
    public DesignerInterpolationInfo[] InterpolationInfos => Target.Interpolations;

    public TextSpan CXDesignerSpan => Target.CXDesignerSpan;

    public CXParser Parser => Document.Parser;

    public CXGraph Graph
    {
        get => _graph ??= CXGraph.Create(this);
        init => _graph = value;
    }

    public string SimpleSource => _simpleSource ??= (
        GetCXWithoutInterpolations(
            CXDesignerSpan.Start,
            CXDesigner,
            InterpolationInfos
        )
    );

    public bool UsesDesigner
        => Operation.TargetMethod.Parameters[0].Type.SpecialType is not SpecialType.System_String;

    private string? _simpleSource;

    private CXGraph? _graph;

    public CXGraphManager(CXGraphManager other)
    {
        _graph = other.Graph;
        Generator = other.Generator;
        Key = other.Key;
        Target = other.Target;
        Document = other.Document;
    }

    public static CXGraphManager Create(SourceGenerator generator, string key, Target target, CancellationToken token)
    {
        var reader = new CXSourceReader(
            new CXSourceText.StringSource(target.CXDesigner),
            target.CXDesignerSpan,
            target.Interpolations.Select(x => x.Span).ToArray(),
            target.CXQuoteCount
        );

        var doc = CXParser.Parse(reader, token);

        return new CXGraphManager(
            generator,
            key,
            target,
            doc
        );
    }

    public CXGraphManager OnUpdate(string key, Target target, CancellationToken token)
    {
        var result = this with { Key = key, Target = target };

        result.DoReparse(target, this, ref result, token);
        
        return result;
    }

    private void DoReparse(Target target, CXGraphManager old, ref CXGraphManager result, CancellationToken token)
    {
        var reader = new CXSourceReader(
            new CXSourceText.StringSource(target.CXDesigner),
            target.CXDesignerSpan,
            target.Interpolations.Select(x => x.Span).ToArray(),
            target.CXQuoteCount
        );

        var parseResult = IncrementalParseResult.Empty;

        var document = FORCE_NO_INCREMENTAL
            ? CXParser.Parse(reader, token)
            : Document.IncrementalParse(
                reader,
                target
                    .SyntaxTree
                    .GetChanges(old.SyntaxTree)
                    .Where(x => CXDesignerSpan.IntersectsWith(x.Span))
                    .ToArray(),
                out parseResult,
                token
            );

        result = result with
        {
            Graph = result.Graph.Update(result, parseResult, document),
            Document = document
        };
    }

    private RenderedInterceptor? _render;

    public RenderedInterceptor Render(CancellationToken token = default)
    {
        _render ??= CreateRender(token);
        return _render.Value;
    }

    private RenderedInterceptor CreateRender(CancellationToken token = default)
    {
        var parserDiagnostics = Document
            .Diagnostics
            .Select(x =>
                Diagnostics.CreateParsingDiagnostic(x, Graph.GetLocation(x.Span))
            )
            .ToArray();

        if (parserDiagnostics.Length > 0)
        {
            return new RenderedInterceptor(
                InterceptLocation,
                InvocationSyntax.GetLocation(),
                CXDesigner,
                string.Empty,
                [..parserDiagnostics],
                UsesDesigner
            );
        }

        if (Graph.HasErrors)
        {
            return new RenderedInterceptor(
                InterceptLocation,
                InvocationSyntax.GetLocation(),
                CXDesigner,
                string.Empty,
                [..parserDiagnostics, ..Graph.Diagnostics],
                UsesDesigner
            );
        }

        var context = new ComponentContext(Graph);

        Graph.Validate(context);

        if (context.HasErrors || Graph.HasErrors)
        {
            return new(
                this.InterceptLocation,
                InvocationSyntax.GetLocation(),
                CXDesigner,
                string.Empty,
                [..parserDiagnostics, ..Graph.Diagnostics, ..context.GlobalDiagnostics],
                UsesDesigner
            );
        }

        var source = Graph.Render(context);

        return new(
            this.InterceptLocation,
            InvocationSyntax.GetLocation(),
            CXDesigner,
            source,
            [
                ..parserDiagnostics,
                ..Graph.Diagnostics,
                ..context.GlobalDiagnostics
            ],
            UsesDesigner
        );
    }

    private static string GetCXWithoutInterpolations(
        int offset,
        string cx,
        DesignerInterpolationInfo[] interpolations
    )
    {
        if (interpolations.Length is 0) return cx;

        var builder = new StringBuilder(cx);

        var rmDelta = 0;
        for (var i = 0; i < interpolations.Length; i++)
        {
            var interpolation = interpolations[i];
            builder.Remove(interpolation.Span.Start - offset - rmDelta, interpolation.Span.Length);
            rmDelta += interpolation.Span.Length;
        }

        return builder.ToString();
    }
}