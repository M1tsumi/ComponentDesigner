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
    public GeneratorOptions Options { get; }
    public ICXNode CXNode { get; }
    public CXGraph.Node? ParentGraphNode { get; }

    public IReadOnlyList<ComponentInitializationGraphNode> Initializations => _inits;

    private readonly List<ComponentInitializationGraphNode> _inits;

    public ComponentGraphInitializationContext(
        ICXNode cxNode,
        CXGraph.Node? parentGraphNode, 
        GeneratorOptions options, 
        List<ComponentInitializationGraphNode>? inits = null
    )
    {
        Options = options;
        ParentGraphNode = parentGraphNode;
        CXNode = cxNode;
        _inits = inits ?? [];
    }


    public void Push(ComponentInitializationGraphNode init)
        => _inits.Add(init);

    public void Push<T, S>(T node, S? state, IReadOnlyList<CXNode> children)
        where T : ComponentNode
        where S : ComponentState
    {
        if (state is null) return;
        
        Push(new(node, state, children));
    }
    
    public void Push<T>(T node) where T : ComponentNode
    {
        var context = new ComponentStateInitializationContext(
            CXNode,
            Options,
            []
        );
        
        Push(node, node.Create(context), context.Children);
    }
    
    public void Push<T>() where T : ComponentNode
    {
        if (ComponentNode.TryGetComponentNode<T>(out var node))
            Push(node);
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

    public readonly ICXNode Node;

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