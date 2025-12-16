using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Nodes;

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

    public bool UpdateState(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var result = false;

        foreach (var attributeNode in AttributeNodes)
            result |= attributeNode.UpdateState(context, diagnostics);

        foreach (var child in Children)
            result |= child.UpdateState(context, diagnostics);

        if (State is null)
        {
            goto updateSelf;
        }

        var newState = Inner.UpdateState(State, context, diagnostics);

        result |= !newState.Equals(State);

        State = newState;

        updateSelf:

        if (result)
        {
            // clear cached render
            _render = null;
        }

        return result;
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
}