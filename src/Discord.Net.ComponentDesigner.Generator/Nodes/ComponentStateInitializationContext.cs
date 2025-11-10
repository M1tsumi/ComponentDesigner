using System;
using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly record struct ComponentInitializationGraphNode(
    ComponentNode Node,
    ComponentState State,
    IReadOnlyList<CXNode> Children
);

public readonly struct ComponentGraphInitializationContext
{
    public GeneratorOptions Options => _graphContext.Options;
    public ICXNode CXNode { get; }
    public CXGraph.Node? ParentGraphNode { get; }

    public IReadOnlyList<ComponentInitializationGraphNode> Initializations => _inits;

    private readonly List<ComponentInitializationGraphNode> _inits;
    private readonly CXGraph.GraphInitializationContext _graphContext;

    public ComponentGraphInitializationContext(
        ICXNode cxNode,
        CXGraph.Node? parentGraphNode,
        CXGraph.GraphInitializationContext graphContext,
        List<ComponentInitializationGraphNode>? inits = null
    )
    {
        _graphContext = graphContext;
        ParentGraphNode = parentGraphNode;
        CXNode = cxNode;
        _inits = inits ?? [];
    }

    public void Push(ComponentInitializationGraphNode init, CXGraph.Node? parent = null)
    {
        if (parent is not null)
        {
            parent.Children.Add(
                CXGraph.CreateNodeFromInitialization(init, _graphContext, parent)
            );
        }
        else
        {
            _inits.Add(init);
        }
    }

    public void Push<T, S>(
        T node,
        S? state,
        IReadOnlyList<CXNode>? children = null,
        CXGraph.Node? parent = null
    )
        where T : ComponentNode
        where S : ComponentState
    {
        if (state is null) return;

        Push(new(node, state, children ?? []), parent);
    }

    public void Push<T>(
        T node,
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        CXGraph.Node? parent = null
    ) where T : ComponentNode
    {
        var context = new ComponentStateInitializationContext(
            cxNode ?? CXNode,
            Options,
            [..children ?? []]
        );

        Push(node, node.Create(context), context.Children, parent);
    }

    public void Push<T>(
        ICXNode? cxNode = null,
        IReadOnlyList<CXNode>? children = null,
        CXGraph.Node? parent = null
    ) where T : ComponentNode
    {
        if (ComponentNode.TryGetComponentNode<T>(out var node))
            Push(node, cxNode, children, parent);
        else
            throw new InvalidOperationException(
                $"{typeof(T).Name} is not a statically known node!"
            );
    }
}

public readonly struct ComponentStateInitializationContext
{
    public GeneratorOptions Options { get; }
    public IReadOnlyList<CXNode> Children => _children;

    public ICXNode Node { get; }

    private readonly List<CXNode> _children;

    public ComponentStateInitializationContext(
        ICXNode node,
        GeneratorOptions options,
        List<CXNode> children
    )
    {
        Options = options;
        Node = node;
        _children = children;
    }

    public void AddChildren(params IEnumerable<CXNode> children)
        => _children.AddRange(children);
}