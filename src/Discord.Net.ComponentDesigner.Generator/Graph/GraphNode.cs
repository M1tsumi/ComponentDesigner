using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Nodes;
using Discord.CX.Util;

namespace Discord.CX;

public sealed class GraphNode : IEquatable<GraphNode>
{
    public ComponentNode Inner { get; }

    public ComponentState? State { get; set; }

    public List<GraphNode> Children { get; }

    public List<GraphNode> AttributeNodes { get; }

    public GraphNode? Parent { get; private set; }

    private Result<string>? _render;

    public GraphNode(
        ComponentNode inner,
        ComponentState? state,
        List<GraphNode> children,
        List<GraphNode> attributeNodes,
        GraphNode? parent = null,
        Result<string>? render = null
    )
    {
        Inner = inner;
        State = state;
        Children = children;
        AttributeNodes = attributeNodes;
        Parent = parent;
        _render = render;
    }

    public GraphNode Reuse(GraphNode? parent)
    {
        Parent = parent;
        return this;
    }

    public GraphNode UpdateState(
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var recreate = false;
        var children = new GraphNode[Children.Count];
        var attrNodes = new GraphNode[AttributeNodes.Count];

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            children[i] = child.UpdateState(context, diagnostics);
            recreate |= !ReferenceEquals(child, children[i]);
        }

        for (var i = 0; i < AttributeNodes.Count; i++)
        {
            var child = AttributeNodes[i];
            attrNodes[i] = child.UpdateState(context, diagnostics);
            recreate |= !ReferenceEquals(child, attrNodes[i]);
        }
        
        var newState = State is null ? State : Inner.UpdateState(State, context, diagnostics);
        recreate |= (!newState?.Equals(State) ?? State is not null);
        
        if (recreate)
        {
            return new(
                Inner,
                newState,
                [..children],
                [..attrNodes],
                Parent
            );
        }

        return this;
    }

    public Result<string> Render(IComponentContext context, ComponentRenderingOptions options = default)
    {
        // state is late-bound
        Debug.Assert(State is not null);

        return _render ??= Inner.Render(State!, context, options);
    }

    public void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (State is not null)
            Inner.Validate(State, context, diagnostics);

        foreach (var attributeNode in AttributeNodes) attributeNode.Validate(context, diagnostics);
        foreach (var child in Children) child.Validate(context, diagnostics);
    }

    public bool Equals(GraphNode other)
        => (State?.Equals(other.State) ?? other.State is null) &&
           Inner.Equals(other.Inner) &&
           Children.SequenceEqual(other.Children) &&
           AttributeNodes.SequenceEqual(other.AttributeNodes);

    public override bool Equals(object? obj)
        => obj is GraphNode other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(Inner, State, Children.Aggregate(0, Hash.Combine), AttributeNodes.Aggregate(0, Hash.Combine));
}