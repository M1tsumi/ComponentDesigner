using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Discord.CX.Nodes.Components.SelectMenus;

public sealed class StringSelectOptionComponentNode : ComponentNode
{
    public override string Name => "select-menu-option";

    public override IReadOnlyList<string> Aliases { get; } = ["option"];

    public ComponentProperty Label { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Emoji { get; }
    public ComponentProperty Default { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public StringSelectOptionComponentNode()
    {
        Properties =
        [
            Label = new(
                "label",
                renderer: Renderers.String
            ),
            Value = new(
                "value",
                renderer: Renderers.String
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            ),
            Emoji = new(
                "emoji",
                isOptional: false,
                renderer: Renderers.Emoji
            ),
            Default = new(
                "default",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isDefault"
            )
        ];
    }

    public override ComponentState? Create(ComponentStateInitializationContext context)
    {
        var state = base.Create(context);

        if (
            context.Node is CXElement {Children.Count: 1} element &&
            element.Children[0] is CXValue value
        )
        {
            state!.SubstitutePropertyValue(Value, value);
        }

        return state;
    }

    public override string Render(ComponentState state, ComponentContext context)
        => $"""
            new {context.KnownTypes.SelectMenuOptionBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                state.RenderProperties(this, context)
                    .WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome("\n")
            })
            """;
}
