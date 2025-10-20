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

    public bool HasErrors => Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);
    public IReadOnlyList<Diagnostic> Diagnostics
        => [.._diagnostics, ..RootNodes.SelectMany(x => x.Diagnostics)];
    
    public readonly CXGraphManager Manager;
    public readonly ImmutableArray<Node> RootNodes;
    public readonly IReadOnlyDictionary<ICXNode, Node> NodeMap;

    private readonly ImmutableArray<Diagnostic> _diagnostics;
    
    public CXGraph(
        CXGraphManager manager,
        ImmutableArray<Node> rootNodes,
        ImmutableArray<Diagnostic> diagnostics,
        IReadOnlyDictionary<ICXNode, Node> nodeMap
    )
    {
        Manager = manager;
        RootNodes = rootNodes;
        _diagnostics = diagnostics;
        NodeMap = nodeMap;
    }

    public Location GetLocation(ICXNode node) => GetLocation(Manager, node);
    public Location GetLocation(TextSpan span) => GetLocation(Manager, span);

    public static Location GetLocation(CXGraphManager manager, ICXNode node)
        => GetLocation(manager, node.Span);

    public static Location GetLocation(CXGraphManager manager, TextSpan span)
        => manager.SyntaxTree.GetLocation(span);

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
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var rootNodes = ImmutableArray.CreateBuilder<Node>();

        foreach (var cxNode in document.RootNodes)
        {
            rootNodes.AddRange(
                CreateNodes(
                    manager,
                    cxNode,
                    null,
                    parseResult.ReusedNodes,
                    this,
                    map,
                    diagnostics,
                    parseResult
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
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var rootNodes = manager.Document
            .RootNodes
            .SelectMany(x =>
                CreateNodes(
                    manager,
                    x,
                    null,
                    [],
                    null,
                    map,
                    diagnostics,
                    null
                )
            )
            .ToImmutableArray();

        return new(manager, rootNodes!, diagnostics.ToImmutable(), map);
    }

    private static IEnumerable<Node> CreateNodes(
        CXGraphManager manager,
        CXNode cxNode,
        Node? parent,
        IReadOnlyList<ICXNode> reusedNodes,
        CXGraph? oldGraph,
        Dictionary<ICXNode, Node> map,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        IncrementalParseResult? incrementalParseResult
    )
    {
        if (
            oldGraph.HasValue &&
            incrementalParseResult.HasValue &&
            reusedNodes.Contains(cxNode) &&
            oldGraph.Value.NodeMap.TryGetValue(cxNode, out var existing) && 
            existing.Diagnostics.Count is 0
        )
        {
            var node = map[cxNode] = existing.Reuse(parent, incrementalParseResult.Value, manager);
            return [node];
        }

        switch (cxNode)
        {
            case CXValue.Interpolation interpolation:
            {
                var info = manager.InterpolationInfos[interpolation.InterpolationIndex];

                if (
                    InterleavedComponentNode.TryCreate(
                        info.Symbol,
                        manager.Compilation,
                        out var inner
                    )
                )
                {
                    var state = inner.Create(new ComponentStateInitializationContext(interpolation, []));

                    if (state is null) return [];

                    return
                    [
                        map[interpolation] = new(
                            inner,
                            state,
                            [],
                            parent
                        )
                    ];
                }

                return [];
            }
            case CXElement element:
            {
                if (element.IsFragment)
                {
                    return element.Children.SelectMany(x =>
                        CreateNodes(
                            manager,
                            x,
                            parent,
                            reusedNodes,
                            oldGraph,
                            map,
                            diagnostics,
                            incrementalParseResult
                        )
                    );
                }

                if (
                    !ComponentNode.TryGetNode(element.Identifier, out var componentNode) &&
                    !ComponentNode.TryGetProviderNode(
                        manager.Compilation.GetSemanticModel(manager.SyntaxTree),
                        manager.ArgumentExpressionSyntax.SpanStart,
                        element.Identifier,
                        out componentNode
                    )
                )
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            CX.Diagnostics.UnknownComponent,
                            GetLocation(manager, element),
                            element.Identifier
                        )
                    );

                    return [];
                }

                var children = new List<CXNode>();

                var state = componentNode.Create(new(element, children));

                if (state is null) return [];

                var nodeChildren = new List<Node>();
                var node = map[element] = state.OwningNode = new(
                    componentNode,
                    state,
                    nodeChildren,
                    parent
                );

                nodeChildren.AddRange(
                    children
                        .SelectMany(x => CreateNodes(
                                manager,
                                x,
                                node,
                                reusedNodes,
                                oldGraph,
                                map,
                                diagnostics,
                                incrementalParseResult
                            )
                        )
                );

                return [node];
            }
            default: return [];
        }
    }

    public void Validate(ComponentContext context)
    {
        foreach (var node in RootNodes) node.Validate(context);
    }

    public string Render(ComponentContext context)
        => string.Join(",\n", RootNodes.Select(x => x.Render(context)));

    public sealed class Node
    {
        public ComponentNode Inner { get; }
        public ComponentState State => _state;
        public IReadOnlyList<Node> Children { get; }
        public Node? Parent { get; }

        public IReadOnlyList<Diagnostic> Diagnostics
            => [.._diagnostics, ..Children.SelectMany(x => x.Diagnostics)];

        public bool Incremental { get; }

        private readonly List<Diagnostic> _diagnostics;
        private string? _render;

        private ComponentState _state;

        public Node(
            ComponentNode inner,
            ComponentState state,
            IReadOnlyList<Node> children,
            Node? parent = null,
            IReadOnlyList<Diagnostic>? diagnostics = null,
            bool incremental = false,
            string? render = null
        )
        {
            Inner = inner;
            _state = state;
            Children = children;
            Parent = parent;
            _diagnostics = [..diagnostics ?? []];
            _render = render;
            Incremental = incremental;
        }

        public void UpdateState(ComponentContext context)
        {
            // update children first
            foreach (var node in Children)
                node.UpdateState(context);
            
            Inner.UpdateState(ref _state, context);
        }
        
        public string Render(ComponentContext context)
        {
            if (_render is not null) return _render;

            using (context.CreateDiagnosticScope(_diagnostics))
            {
                return _render = Inner.Render(State, context);
            }
        }

        public void Validate(ComponentContext context)
        {
            using (context.CreateDiagnosticScope(_diagnostics))
            {
                Inner.Validate(State, context);
                foreach (var child in Children) child.Validate(context);
            }
        }

        public Node Reuse(Node? parent, IncrementalParseResult parseResult, CXGraphManager manager)
        {
            var diagnostics = new List<Diagnostic>(Diagnostics);

            if (diagnostics.Count > 0)
            {
                var offset = 0;
                var changeQueue = new Queue<TextChange>(parseResult.Changes);
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    var diagnostic = diagnostics[i];
                    var diagnosticSpan = diagnostic.Location.SourceSpan;

                    while (changeQueue.Count > 0)
                    {
                        TextChangeRange change = changeQueue.Peek();

                        if (change.Span.Start < diagnosticSpan.Start)
                        {
                            offset += (change.NewLength - change.Span.Length);
                            changeQueue.Dequeue();
                        }
                    }

                    if (offset is not 0)
                    {
                        // we love roslyn making 'WithLocation' internal *smile*
                        diagnostics[i] = Diagnostic.Create(
                            diagnostic.Descriptor.Id,
                            diagnostic.Descriptor.Category,
                            diagnostic.GetMessage(),
                            diagnostic.Severity,
                            diagnostic.Descriptor.DefaultSeverity,
                            diagnostic.Descriptor.IsEnabledByDefault,
                            diagnostic.WarningLevel,
                            diagnostic.Descriptor.Title,
                            diagnostic.Descriptor.Description,
                            diagnostic.Descriptor.HelpLinkUri,
                            GetLocation(
                                manager,
                                new TextSpan(diagnosticSpan.Start + offset, diagnosticSpan.Length)
                            ),
                            null,
                            diagnostic.Descriptor.CustomTags,
                            diagnostic.Properties
                        );
                    }
                }
            }

            return new(
                Inner,
                State,
                Children,
                parent,
                diagnostics,
                true,
                _render
            );
        }
    }
}