using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord.CX.Nodes;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public sealed class CXGraph : IEquatable<CXGraph>
{
    public string Key { get; }
    public EquatableArray<GraphNode> RootNodes { get; }
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }
    public IReadOnlyDictionary<ICXNode, GraphNode> NodeMap { get; }
    public CXDoc Document { get; }

    public EquatableArray<DesignerInterpolationInfo> InterpolationInfos => _state.CX.InterpolationInfos;

    public Compilation Compilation => _state.Compilation;

    private readonly GraphGeneratorState _state;

    private CXGraph(
        string key,
        EquatableArray<GraphNode> rootNodes,
        EquatableArray<DiagnosticInfo> diagnostics,
        IReadOnlyDictionary<ICXNode, GraphNode> nodeMap,
        GraphGeneratorState state,
        CXDoc document
    )
    {
        _state = state;
        Document = document;
        Diagnostics = diagnostics;
        NodeMap = nodeMap;
        Key = key;
        RootNodes = rootNodes;
    }

    public static CXGraph Create(
        GraphGeneratorState state,
        CXGraph? old,
        CancellationToken token
    )
    {
        // TODO: support inc. parsing
        var reader = new CXSourceReader(
            new CXSourceText.StringSource(state.CX.Designer),
            state.CX.Location.TextSpan,
            state.CX.InterpolationInfos.Select(x => x.Span).ToArray(),
            state.CX.QuoteCount
        );

        var doc = CXParser.Parse(reader, token);

        var parserDiagnostics = doc.Diagnostics.Select(x => new DiagnosticInfo(
            CX.Diagnostics.CreateParsingDiagnostic(x),
            x.Span
        ));

        if (doc.HasErrors)
        {
            return new CXGraph(
                state.Key,
                [],
                [..parserDiagnostics],
                ImmutableDictionary<ICXNode, GraphNode>.Empty,
                state,
                doc
            );
        }

        var map = new Dictionary<ICXNode, GraphNode>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        diagnostics.AddRange(parserDiagnostics);

        var context = new GraphInitializationContext(
            state,
            doc,
            [],
            old,
            map,
            diagnostics,
            null
        );

        return new CXGraph(
            state.Key,
            [
                ..doc.RootNodes
                    .SelectMany(x =>
                        CreateNodes(
                            x,
                            parent: null,
                            context
                        )
                    )
            ],
            diagnostics.ToImmutable(),
            map,
            state,
            doc
        );
    }

    public CXGraph UpdateFromCompilation(Compilation compilation, CancellationToken token)
    {
        var context = new ComponentContext(
            this
        );

        var diagnostics = new List<DiagnosticInfo>();


        var hasUpdatedState = false;

        foreach (var rootNode in RootNodes)
        {
            hasUpdatedState |= rootNode.UpdateState(context, diagnostics);
        }

        if (hasUpdatedState || diagnostics.Count is not 0)
            return new CXGraph(
                Key,
                RootNodes,
                [..Diagnostics, ..diagnostics],
                NodeMap,
                _state,
                Document
            );

        return this;
    }

    public void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        foreach (var node in RootNodes)
            node.Validate(context, diagnostics);
    }

    public RenderedGraph Render(IComponentContext? context = null, CancellationToken token = default)
    {
        context ??= new ComponentContext(this);

        var diagnostics = new List<DiagnosticInfo>(Diagnostics);

        Validate(context, diagnostics);

        var result = RootNodes
            .Select(x => x.Render(context))
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));

        diagnostics.AddRange(result.Diagnostics);

        return new RenderedGraph(
            Key,
            _state.CX.Designer,
            result.GetValueOrDefault(),
            [..diagnostics]
        );
    }


    private static IEnumerable<GraphNode> CreateNodes(
        CXNode cxNode,
        GraphNode? parent,
        GraphInitializationContext context
    )
    {
        if (context.TryReuse(cxNode, parent, out var reused))
            return [reused];

        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
                return CreateInterpolationNodes(
                    interpolation,
                    context.Interpolations[interpolation.InterpolationIndex],
                    parent,
                    context
                );
            case CXValue.Multipart multipart:
            {
                var parts = new List<GraphNode>();

                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                    {
                        parts.AddRange(CreateInterpolationNodes(
                            token,
                            context.Interpolations[index],
                            parent,
                            context
                        ));
                    }
                    else
                    {
                        context.Diagnostics.Add(
                            new DiagnosticInfo(
                                CX.Diagnostics.UnknownComponent(token.Kind.ToString()),
                                cxNode.Span
                            )
                        );
                    }
                }

                return parts;
            }
            case CXElement element:
                return CreateElementNodes(element, parent, context);
            default:
                context.Diagnostics.Add(
                    new(
                        CX.Diagnostics.UnknownComponent(cxNode.GetType().ToString()),
                        cxNode.Span
                    )
                );
                return [];
        }

        static IEnumerable<GraphNode> CreateElementNodes(CXElement element, GraphNode? parent,
            GraphInitializationContext context)
        {
            if (element.IsFragment)
            {
                return element.Children.SelectMany(x =>
                    CreateNodes(x, parent, context)
                );
            }

            if (
                !ComponentNode.TryGetNode(element.Identifier, out var componentNode) &&
                !ComponentNode.TryGetProviderNode(
                    context.State.CX.SemanticModel,
                    context.State.CX.Location.TextSpan.Start,
                    element.Identifier,
                    out componentNode
                )
            )
            {
                context.Diagnostics.Add(
                    new(
                        CX.Diagnostics.UnknownComponent(element.Identifier),
                        element.Span
                    )
                );

                return [];
            }

            var initContext = new ComponentGraphInitializationContext(
                element,
                parent,
                context
            );

            componentNode.AddGraphNode(initContext);

            return initContext
                .Initializations
                .Select(x =>
                    CreateFromInitialization(x, context, parent)
                )
                .Where(x => x is not null)!;
        }

        static IEnumerable<GraphNode> CreateInterpolationNodes(
            ICXNode cxNode,
            DesignerInterpolationInfo info,
            GraphNode? parent,
            GraphInitializationContext context
        )
        {
            if (InterleavedComponentNode.TryCreate(info.Symbol, context.Compilation, out var inner))
            {
                var node = new GraphNode(
                    inner,
                    null,
                    [],
                    [],
                    parent
                );

                var state = inner.Create(
                    new ComponentStateInitializationContext(
                        cxNode,
                        node,
                        [],
                        context
                    ),
                    context.Diagnostics
                );

                if (state is null) return [];

                node.State = state;

                return
                [
                    context.AddToMap(
                        cxNode,
                        node
                    )
                ];
            }

            context.Diagnostics.Add(
                new(
                    CX.Diagnostics.UnknownComponent(
                        info.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        ?? "<interleaved>"
                    ),
                    cxNode.Span
                )
            );

            return [];
        }
    }

    public static GraphNode? CreateFromInitialization(
        ComponentInitializationGraphNode init,
        GraphInitializationContext context,
        GraphNode? parent
    )
    {
        // var state = init.State;

        var nodeChildren = new List<GraphNode>();
        var attributeNodes = new List<GraphNode>();

        var node = new GraphNode(
            init.Node,
            null,
            nodeChildren,
            attributeNodes,
            parent
        );

        List<CXNode> children = [..init.Children];

        var stateContext = new ComponentStateInitializationContext(
            init.CXNode,
            node,
            children,
            context
        );

        var state = init.Node.Create(stateContext, context.Diagnostics);

        if (state is null) return null;

        node.State = state;

        context.AddToMap(
            state.Source,
            node
        );

        if (state?.Source is CXElement { Attributes: { Count: > 0 } attributes })
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Value is CXValue.Element nestedElement)
                {
                    attributeNodes.AddRange(
                        CreateNodes(
                            nestedElement.Value,
                            node,
                            context
                        )
                    );
                }
            }
        }

        nodeChildren.AddRange(
            children
                .SelectMany(x => CreateNodes(
                        x,
                        node,
                        context
                    )
                )
        );

        return node;
    }

    public bool Equals(CXGraph other)
        => Key == other.Key &&
           RootNodes.Equals(other.RootNodes) &&
           Diagnostics.Equals(other.Diagnostics);
}