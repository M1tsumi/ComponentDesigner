using System;
using System.Collections.Generic;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public readonly struct ComponentStateInitializationContext
{
    public CXDesignerGeneratorState CX => GraphContext.State.CX;
    public Compilation Compilation => GraphContext.Compilation;
    public GeneratorOptions Options => GraphContext.Options;
    public GraphInitializationContext GraphContext { get; }
    public IReadOnlyList<CXNode> Children => _children;

    public ICXNode CXNode { get; }
    public GraphNode GraphNode { get; }

    private readonly List<CXNode> _children;

    public ComponentStateInitializationContext(
        ICXNode cxNode,
        GraphNode graphNode,
        List<CXNode> children,
        GraphInitializationContext graphContext
    )
    {
        GraphContext = graphContext;
        CXNode = cxNode;
        GraphNode = graphNode;
        _children = children;
    }

    public void AddChildren(params IEnumerable<CXNode> children)
        => _children.AddRange(children);
}