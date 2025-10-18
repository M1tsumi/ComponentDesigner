using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components;

public readonly struct ComponentStateInitializationContext
{
    public IReadOnlyList<CXNode> Children => _children;
    
    public readonly ICXNode Node;
    
    private readonly List<CXNode> _children;

    public ComponentStateInitializationContext(ICXNode node, List<CXNode> children)
    {
        Node = node;
        _children = children;
    }

    public void AddChildren(params IEnumerable<CXNode> children)
        => _children.AddRange(children);
}