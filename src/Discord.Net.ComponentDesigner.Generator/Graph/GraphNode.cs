using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Discord.CX.Util;

namespace Discord.CX;

public sealed class GraphNode : IEquatable<GraphNode>
{
    public ComponentNode Inner { get; }

    public ComponentState State
    {
        get => _state ?? throw new InvalidOperationException("Attempt to access node state before initialization");
        set => _state = value;
    }

    public List<GraphNode> Children => _children ??= [];

    public List<GraphNode> AttributeNodes => _attributeNodes ??= [];

    public GraphNode? Parent { get; private set; }

    public bool HasChildren
        => _children?.Count > 0;

    public ICXNode SourceCXNode => State.Source;

    private ComponentState? _state;
    private Result<string>? _render;

    private List<GraphNode>? _children;
    private List<GraphNode>? _attributeNodes;

    public GraphNode(
        ComponentNode inner,
        ComponentState? state,
        List<GraphNode>? children,
        List<GraphNode>? attributeNodes,
        GraphNode? parent = null,
        Result<string>? render = null
    )
    {
        Inner = inner;
        _state = state;
        _children = children;
        _attributeNodes = attributeNodes;
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
        List<GraphNode>? children = null;
        List<GraphNode>? attrNodes = null;

        if (_children?.Count > 0)
        {
            children = new(_children.Count);

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                children[i] = child.UpdateState(context, diagnostics);
                recreate |= !ReferenceEquals(child, children[i]);
            }
        }

        if (_attributeNodes?.Count > 0)
        {
            attrNodes = new(_attributeNodes.Count);

            for (var i = 0; i < AttributeNodes.Count; i++)
            {
                var child = AttributeNodes[i];
                attrNodes[i] = child.UpdateState(context, diagnostics);
                recreate |= !ReferenceEquals(child, attrNodes[i]);
            }
        }

        var newState = _state is null ? _state : Inner.UpdateState(_state, context, diagnostics);
        recreate |= (!newState?.Equals(_state) ?? _state is not null);

        if (recreate)
        {
            return new(
                Inner,
                newState,
                children,
                attrNodes,
                Parent
            );
        }

        return this;
    }

    public Result<string> Render(IComponentContext context, ComponentRenderingOptions options = default)
    {
        // state is late-bound
        Debug.Assert(_state is not null, "State should not be null by render time");

        return _render ??= Inner.Render(State, context, options);
    }

    public void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        // state is late-bound
        Debug.Assert(_state is not null, "State should not be null by validation time");

        if (_state is not null)
            Inner.Validate(State, context, diagnostics);

        if (_attributeNodes?.Count > 0)
            foreach (var attributeNode in AttributeNodes)
                attributeNode.Validate(context, diagnostics);

        if (_children?.Count > 0)
            foreach (var child in Children)
                child.Validate(context, diagnostics);
    }

    public string ToPathString()
    {
        var nodes = new List<GraphNode>();

        var current = this;

        while (current is not null)
        {
            nodes.Add(current);
            current = current.Parent;
        }

        nodes.Reverse();

        return string.Join(" -> ", nodes.Select(x => x.Inner.Name));
    }

    public bool Equals(GraphNode other)
        => (_state?.Equals(other._state) ?? other._state is null) &&
           Inner.Equals(other.Inner) &&
           (_children, other._children) switch
           {
               (not null, not null) => _children.SequenceEqual(other._children),
               (null, null) => true,
               _ => false
           } &&
           (_attributeNodes, other._attributeNodes) switch
           {
               (not null, not null) => _attributeNodes.SequenceEqual(other._attributeNodes),
               (null, null) => true,
               _ => false
           };

    public override bool Equals(object? obj)
        => obj is GraphNode other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Inner,
            _state,
            _children?.Aggregate(0, Hash.Combine),
            _attributeNodes?.Aggregate(0, Hash.Combine)
        );
}