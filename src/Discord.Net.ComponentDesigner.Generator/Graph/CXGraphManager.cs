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
    ComponentDesignerTarget Target,
    GeneratorOptions Options,
    CXDoc Document
)
{
    public const bool FORCE_NO_INCREMENTAL = true;
    public const bool ALWAYS_REPARSE = true;

    public InterceptableLocation InterceptLocation => Target.InterceptLocation;
    public LocationInfo CXDesignerLocation => Target.CXDesignerLocation;
    public Compilation Compilation => Target.Compilation;
    public SyntaxTree SyntaxTree => Target.SyntaxTree;

    public string CXDesigner => Target.CXDesigner;
    public EquatableArray<DesignerInterpolationInfo> InterpolationInfos => Target.Interpolations;

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
        => Target.UsesDesigner;

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

    public static CXGraphManager Create(
        SourceGenerator generator,
        string key,
        ComponentDesignerTarget target,
        GeneratorOptions options,
        CancellationToken token
    )
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
            options,
            doc
        );
    }

    public CXGraphManager OnUpdate(
        string key,
        ComponentDesignerTarget target,
        GeneratorOptions options,
        CancellationToken token
    )
    {
        var result = this with
        {
            Key = key,
            Target = target,
            Options = options
        };

        result.DoReparse(target, this, ref result, token);

        return result;
    }

    private void DoReparse(ComponentDesignerTarget target, CXGraphManager old, ref CXGraphManager result,
        CancellationToken token)
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
            : throw new NotImplementedException();
        // : Document.IncrementalParse(
        //     reader,
        //     target
        //         .SyntaxTree
        //         .GetChanges(old.SyntaxTree)
        //         .Where(x => CXDesignerSpan.IntersectsWith(x.Span))
        //         .ToArray(),
        //     out parseResult,
        //     token
        // );

        result = result with
        {
            Graph = result.Graph.Update(result, parseResult, document),
            Document = document
        };
    }

    private RenderedInterceptor? _render;

    public RenderedInterceptor Render(CancellationToken token = default)
        => _render ??= CreateRender(token);

    private RenderedInterceptor CreateRender(CancellationToken token = default)
    {
        var diagnostics = Document
            .Diagnostics
            .Select(x =>
                new DiagnosticInfo(
                    Diagnostics.CreateParsingDiagnostic(x),
                    x.Span
                )
            )
            .ToList();

        if (diagnostics.Count > 0)
        {
            return new RenderedInterceptor(
                Target.SyntaxTree,
                InterceptLocation,
                CXDesignerLocation,
                CXDesigner,
                "// omitted, contains parser errors.",
                [..diagnostics],
                UsesDesigner
            );
        }

        var context = new ComponentContext(Graph);

        foreach (var node in Graph.RootNodes)
        {
            node.UpdateState(context);
        }

        Graph.Validate(context, diagnostics);

        // if (context.HasErrors || Graph.HasErrors)
        // {
        //     return new(
        //         this.InterceptLocation,
        //         CXDesignerLocation,
        //         CXDesigner,
        //         "// omitted, contains validation errors.",
        //         [..diagnostics, ..Graph.Diagnostics, ..context.GlobalDiagnostics],
        //         UsesDesigner
        //     );
        // }

        var source = Graph.Render(context);

        diagnostics.AddRange(source.Diagnostics);

        return new(
            Target.SyntaxTree,
            InterceptLocation,
            CXDesignerLocation,
            CXDesigner,
            source.GetValueOrDefault("// omitted, contains errors")!,
            [..diagnostics],
            UsesDesigner
        );
    }

    private static string GetCXWithoutInterpolations(
        int offset,
        string cx,
        EquatableArray<DesignerInterpolationInfo> interpolations
    )
    {
        if (interpolations.IsEmpty) return cx;

        var builder = new StringBuilder(cx);

        var rmDelta = 0;

        for (var i = 0; i < interpolations.Count; i++)
        {
            var interpolation = interpolations[i];
            builder.Remove(interpolation.Span.Start - offset - rmDelta, interpolation.Span.Length);
            rmDelta += interpolation.Span.Length;
        }

        return builder.ToString();
    }
}