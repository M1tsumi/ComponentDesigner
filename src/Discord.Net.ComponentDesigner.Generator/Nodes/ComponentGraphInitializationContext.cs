using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly struct ComponentGraphInitializationContext
{
    public GeneratorOptions Options => _graphContext.Options;
    public ICXNode CXNode { get; }
    public GraphNode? ParentGraphNode { get; }

    public IReadOnlyList<ComponentInitializationGraphNode> Initializations => _inits;

    private readonly List<ComponentInitializationGraphNode> _inits;
    private readonly GraphInitializationContext _graphContext;

    public ComponentGraphInitializationContext(
        ICXNode cxNode,
        GraphNode? parentGraphNode,
        GraphInitializationContext graphContext,
        List<ComponentInitializationGraphNode>? inits = null
    )
    {
        _graphContext = graphContext;
        ParentGraphNode = parentGraphNode;
        CXNode = cxNode;
        _inits = inits ?? [];
    }

    public void Push(ComponentInitializationGraphNode init, GraphNode? parent = null)
    {
        if (parent is not null)
        {
            if(CXGraph.CreateFromInitialization(init, _graphContext, parent) is {} child)
                parent.Children.Add(child);
        }
        else
        {
            _inits.Add(init);
        }
    }


    public void Push<T>(
        T node,
        ICXNode? cxNode = null,
        IEnumerable<CXNode>? children = null,
        GraphNode? parent = null
    ) where T : ComponentNode
    {
        Push(
            new ComponentInitializationGraphNode(
                node,
                cxNode ?? CXNode,
                [..children ?? []]
            ),
            parent
        );
    }
}