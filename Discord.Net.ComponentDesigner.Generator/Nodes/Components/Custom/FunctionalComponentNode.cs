using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components.Custom;

public class FunctionalComponentNode : ComponentNode
{
    private readonly IMethodSymbol _method;
    public override string Name => $"<functional {_method.ToDisplayString()}>";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public FunctionalComponentNode(IMethodSymbol method)
    {
        _method = method;

        var properties = new List<ComponentProperty>();

        foreach (var parameter in method.Parameters)
        {
            properties.Add(new(
                parameter.Name,
                parameter.HasExplicitDefaultValue,
                renderer: Renderers.CreateRenderer(parameter.Type)
            ));
        }

        Properties = properties;
    }

    private string MethodReference =>
        $"{_method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{_method.Name}";
    
    public override string Render(ComponentState state, ComponentContext context)
        =>
            $"{MethodReference}({state.RenderProperties(this, context)})";
}