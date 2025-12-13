using Discord.CX.Nodes;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX;

public readonly struct CXGraph
{
    public readonly CXGraphManager Manager;
    public readonly ImmutableArray<Node> RootNodes;
    public readonly IReadOnlyDictionary<ICXNode, Node> NodeMap;

    private readonly ImmutableArray<DiagnosticInfo> _diagnostics;

    public CXGraph(
        CXGraphManager manager,
        ImmutableArray<Node> rootNodes,
        ImmutableArray<DiagnosticInfo> diagnostics,
        IReadOnlyDictionary<ICXNode, Node> nodeMap
    )
    {
        Manager = manager;
        RootNodes = rootNodes;
        _diagnostics = diagnostics;
        NodeMap = nodeMap;
    }

    public CXGraph Update(
        CXGraphManager manager,
        IncrementalParseResult parseResult,
        CXDoc document
    )
    {
        /*
         * Update semantics:
         *
         * We try to reuse any ComponentNodes who's source is a reused node within the 'parseResult' AND contain no
         * diagnostics. Any nodes with errors are not reused.
         */

        if (manager == Manager) return this;

        var map = new Dictionary<ICXNode, Node>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        var rootNodes = ImmutableArray.CreateBuilder<Node>();

        var context = new GraphInitializationContext(
            manager,
            parseResult.ReusedNodes,
            this,
            map,
            diagnostics,
            parseResult
        );

        foreach (var cxNode in document.RootNodes)
        {
            rootNodes.AddRange(
                CreateNodes(
                    cxNode,
                    null,
                    context
                )
            );
        }

        return new(manager, rootNodes.ToImmutable(), diagnostics.ToImmutable(), map);
    }

    public static CXGraph Create(
        CXGraphManager manager
    )
    {
        var map = new Dictionary<ICXNode, Node>();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        var context = new GraphInitializationContext(
            manager,
            [],
            null,
            map,
            diagnostics,
            null
        );

        var rootNodes = manager.Document
            .RootNodes
            .SelectMany(x =>
                CreateNodes(
                    x,
                    null,
                    context
                )
            )
            .ToImmutableArray();

        return new(manager, rootNodes!, diagnostics.ToImmutable(), map);
    }

    public readonly record struct GraphInitializationContext(
        CXGraphManager Manager,
        IReadOnlyList<ICXNode> ReusedNodes,
        CXGraph? OldGraph,
        Dictionary<ICXNode, Node> Map,
        ImmutableArray<DiagnosticInfo>.Builder Diagnostics,
        IncrementalParseResult? IncrementalParseResult
    )
    {
        public GeneratorOptions Options => Manager.Options;
        public Compilation Compilation => Manager.Compilation;
        public bool IsIncremental => HasOldGraph && HasIncrementalParseResult;

        public bool HasIncrementalParseResult => IncrementalParseResult is not null;
        public bool HasOldGraph => OldGraph is not null;

        public bool TryReuse(ICXNode node, Node? parent, out Node graphNode)
        {
            if (
                IsIncremental &&
                ReusedNodes.Contains(node) &&
                OldGraph!.Value.NodeMap.TryGetValue(node, out graphNode)
            )
            {
                graphNode = AddToMap(node, graphNode.Reuse(parent, IncrementalParseResult!.Value, Manager));
                return true;
            }

            graphNode = null!;
            return false;
        }

        public Node AddToMap(ICXNode? node, Node graphNode)
        {
            if (node is null) return graphNode;

            return Map[node] = graphNode;
        }
    }

    private static IEnumerable<Node> CreateNodes(
        CXNode cxNode,
        Node? parent,
        GraphInitializationContext context
    )
    {
        if (context.TryReuse(cxNode, parent, out var existing))
        {
            return [existing];
        }

        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
            {
                var info = context.Manager.InterpolationInfos[interpolation.InterpolationIndex];
                return FromInterpolation(cxNode, info);
            }
            case CXValue.Multipart multipart:
                var parts = new List<Node>();
                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                        parts.AddRange(FromInterpolation(token, context.Manager.InterpolationInfos[index]));
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
            case CXElement element:
            {
                if (element.IsFragment)
                {
                    return element.Children.SelectMany(x =>
                        CreateNodes(
                            x,
                            parent,
                            context
                        )
                    );
                }

                /*
                 * TODO: A 2 staged approach for any late-bound compilation/semantic introspection is needed here
                 * since we need to lookup provider and custom components. A first stage pass would build the graph, and
                 * the second stage would map them to a custom node
                 */
                if (
                    !ComponentNode.TryGetNode(element.Identifier, out var componentNode) &&
                    !ComponentNode.TryGetProviderNode(
                        context.Compilation.GetSemanticModel(context.Manager.SyntaxTree),
                        context.Manager.CXDesignerLocation.TextSpan.Start,
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
                        CreateNodeFromInitialization(
                            x,
                            context,
                            parent
                        )
                    );
            }
            default:
                context.Diagnostics.Add(
                    new(
                        CX.Diagnostics.UnknownComponent(cxNode.GetType().ToString()),
                        cxNode.Span
                    )
                );
                return [];
        }

        IEnumerable<Node> FromInterpolation(ICXNode node, DesignerInterpolationInfo info)
        {
            if (
                InterleavedComponentNode.TryCreate(
                    info.Symbol,
                    context.Compilation,
                    out var inner
                )
            )
            {
                var state = inner.Create(new(node, context.Options, []));

                if (state is null) return [];

                return
                [
                    context.AddToMap(
                        node,
                        new(
                            inner,
                            state,
                            [],
                            [],
                            parent
                        )
                    )
                ];
            }

            return [];
        }
    }

    public static CXGraph.Node CreateNodeFromInitialization(
        ComponentInitializationGraphNode initialization,
        GraphInitializationContext context,
        Node? parent
    )
    {
        var state = initialization.State;

        var nodeChildren = new List<Node>();
        var attributeNodes = new List<Node>();

        var node = state.OwningNode = context.AddToMap(
            state.Source,
            new(
                initialization.Node,
                state,
                nodeChildren,
                attributeNodes,
                parent
            )
        );


        if (state.Source is CXElement { Attributes: { Count: > 0 } attributes })
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
            initialization
                .Children
                .SelectMany(x => CreateNodes(
                        x,
                        node,
                        context
                    )
                )
        );

        return node;
    }

    public void Validate(ComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        foreach (var node in RootNodes) node.Validate(context, diagnostics);
    }

    public Result<string> Render(ComponentContext context)
        => RootNodes
            .Select(x => x.Render(context))
            .FlattenAll()
            .Map(x => string.Join(",\n", x));

    public sealed class Node
    {
        public ComponentNode Inner { get; }
        public ComponentState State => _state;
        public List<Node> Children { get; }
        public List<Node> AttributeNodes { get; }
        public Node? Parent { get; }

        public bool Incremental { get; }

        private Result<string>? _render;

        private ComponentState _state;

        public Node(
            ComponentNode inner,
            ComponentState state,
            List<Node> children,
            List<Node> attributeNodes,
            Node? parent = null,
            bool incremental = false,
            Result<string>? render = null
        )
        {
            Inner = inner;
            _state = state;
            Children = children;
            AttributeNodes = attributeNodes;
            Parent = parent;
            _render = render;
            Incremental = incremental;
        }

        public void UpdateState(ComponentContext context)
        {
            foreach (var attributeNode in AttributeNodes)
                attributeNode.UpdateState(context);

            foreach (var node in Children)
                node.UpdateState(context);

            Inner.UpdateState(ref _state, context);
        }

        public Result<string> Render(IComponentContext context, ComponentRenderingOptions options = default)
            => (_render ??= Inner.Render(State, context, options));

        public void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
        {
            Inner.Validate(State, context, diagnostics);
            foreach (var attributeNode in AttributeNodes) attributeNode.Validate(context, diagnostics);
            foreach (var child in Children) child.Validate(context, diagnostics);
        }

        public Node Reuse(Node? parent, IncrementalParseResult parseResult, CXGraphManager manager)
        {
            // TODO: rewrite this
            
            return new(
                Inner,
                State,
                Children,
                AttributeNodes,
                parent,
                true,
                _render
            );
            
            // var diagnostics = new List<DiagnosticInfo>(Diagnostics);
            //
            // if (diagnostics.Count > 0)
            // {
            //     var offset = 0;
            //     var changeQueue = new Queue<TextChange>(parseResult.Changes);
            //     for (var i = 0; i < diagnostics.Count; i++)
            //     {
            //         var diagnostic = diagnostics[i];
            //         var diagnosticSpan = diagnostic.Span;
            //
            //         while (changeQueue.Count > 0)
            //         {
            //             TextChangeRange change = changeQueue.Peek();
            //
            //             if (change.Span.Start < diagnosticSpan.Start)
            //             {
            //                 offset += (change.NewLength - change.Span.Length);
            //                 changeQueue.Dequeue();
            //             }
            //         }
            //
            //         if (offset is not 0)
            //         {
            //             diagnostics[i] = new(
            //                 diagnostic.Descriptor,
            //                 new(diagnosticSpan.Start + offset, diagnosticSpan.Length)
            //             );
            //         }
            //     }
            // }
            //
            // return new(
            //     Inner,
            //     State,
            //     Children,
            //     AttributeNodes,
            //     parent,
            //     diagnostics,
            //     true,
            //     _render
            // );
        }
    }
}