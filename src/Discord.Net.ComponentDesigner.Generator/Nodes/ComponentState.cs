using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discord.CX.Nodes;

public record ComponentState(
    GraphNode OwningGraphNode,
    ICXNode Source
)
{
    public bool HasChildren => OwningGraphNode?.Children.Count > 0;

    public IReadOnlyList<GraphNode> Children
        => OwningGraphNode?.Children ?? [];

    public bool IsElement => Source is CXElement;

    public bool IsRootNode => OwningGraphNode?.Parent is null;

    private readonly Dictionary<ComponentProperty, ComponentPropertyValue> _properties = [];

    public ComponentPropertyValue GetProperty(ComponentProperty property)
    {
        //if (!IsElement) return null;

        if (_properties.TryGetValue(property, out var value)) return value;

        var attribute = (Source as CXElement)?
            .Attributes
            .FirstOrDefault(x =>
                property.Name == x.Identifier.Value || property.Aliases.Contains(x.Identifier.Value)
            );

        GraphNode? node = null;

        if (attribute?.Value is CXValue.Element element)
        {
            node = OwningGraphNode?.AttributeNodes
                .FirstOrDefault(x => ReferenceEquals(x.State.Source, element.Value));
        }

        return _properties[property] = new(property, attribute, node);
    }

    public void ReportPropertyNotAllowed(
        ComponentProperty property,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var propertyValue = GetProperty(property);
        if (propertyValue.IsSpecified)
        {
            diagnostics.Add(
                Diagnostics.PropertyNotAllowed(
                    OwningGraphNode?.Inner.Name ?? "Unknown",
                    propertyValue.Attribute!.Identifier.Value
                ),
                propertyValue.Attribute
            );
        }
    }

    public void RequireOneOf(
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics,
        params ReadOnlySpan<ComponentProperty> properties)
    {
        if (properties.Length is 0) return;

        if (properties.Length is 1)
        {
            GetProperty(properties[0]).ReportPropertyConfigurationDiagnostics(
                context,
                this,
                diagnostics,
                optional: false
            );
            return;
        }

        for (var i = 0; i < properties.Length; i++)
        {
            if (GetProperty(properties[i]).IsSpecified) return;
        }

        var sb = new StringBuilder();

        for (var i = 0; i < properties.Length; i++)
        {
            if (i == properties.Length - 1)
                sb.Append(" or ");

            if (i is not 0) sb.Append('\'');

            sb.Append(properties[i].Name);

            if (i < properties.Length - 1) sb.Append('\'');
        }

        diagnostics.Add(
            Diagnostics.MissingRequiredProperty(OwningGraphNode?.Inner.Name, sb.ToString()),
            Source
        );
    }

    public bool TryGetChildGraphNode(ICXNode node, out GraphNode childGraphNode)
    {
        foreach (var child in Children)
        {
            if (ReferenceEquals(child.State.Source, node))
            {
                childGraphNode = child;
                return true;
            }
        }

        childGraphNode = null!;
        return false;
    }

    public void SubstitutePropertyValue(ComponentProperty property, CXValue value)
    {
        if (!_properties.TryGetValue(property, out var existing))
            _properties[property] = new(property, null) { Value = value };
        else
            _properties[property] = _properties[property] with { Value = value };
    }

    public Result<string> RenderProperties(
        ComponentNode node,
        IComponentContext context,
        bool asInitializers = false,
        Predicate<ComponentProperty>? ignorePredicate = null)
    {
        // TODO: correct handling?
        if (Source is not CXElement element) return string.Empty;

        var values = new List<string>();
        var result = new Result<string>.Builder();

        var success = true;

        foreach (var property in node.Properties)
        {
            if (ignorePredicate?.Invoke(property) is true) continue;

            var propertyValue = GetProperty(property);

            if (propertyValue.CanOmitFromSource) continue;

            var propertyResult = property.Renderer(context, propertyValue);

            if (propertyResult.HasResult)
            {
                var prefix = asInitializers
                    ? $"{property.DotnetPropertyName} = "
                    : $"{property.DotnetParameterName}: ";

                values.Add($"{prefix}{propertyResult.Value}");
            }
            else success = false;

            result.AddDiagnostics(propertyResult.Diagnostics);
        }

        if (success)
            result.WithValue(string.Join($",{Environment.NewLine}", values));

        return result;
    }

    public Result<string> RenderInitializer(
        ComponentNode node,
        IComponentContext context,
        Predicate<ComponentProperty>? ignorePredicate = null
    ) => RenderProperties(node, context, asInitializers: true, ignorePredicate)
        .Map(x =>
            x.PrefixIfSome(4)
                .WithNewlinePadding(4)
                .PrefixIfSome($"{{{Environment.NewLine}")
                .PostfixIfSome($"{Environment.NewLine}}}")
        );

    public Result<string> RenderChildren(
        IComponentContext context,
        Func<GraphNode, bool>? predicate = null,
        ComponentRenderingOptions options = default
    )
    {
        if (OwningGraphNode is null || !HasChildren) return string.Empty;

        IEnumerable<GraphNode> children = OwningGraphNode.Children;

        if (predicate is not null) children = children.Where(predicate);

        return children
            .Select(x => x.Render(context, options))
            .FlattenAll()
            .Map(x => string.Join($",{Environment.NewLine}", x));
    }

    public virtual bool Equals(ComponentState? other)
    {
        return ReferenceEquals(this, other);
    }
}