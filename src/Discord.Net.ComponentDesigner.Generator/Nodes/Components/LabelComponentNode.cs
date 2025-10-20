using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components.SelectMenus;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class LabelComponentNode : ComponentNode
{
    public override string Name => "label";

    public ComponentProperty Component { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }

    public override bool HasChildren => true;

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public LabelComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Value = new(
                "value",
                renderer: Renderers.String
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            )
        ];
    }

    public override ComponentState? Create(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return null;

        var state = base.Create(context);

        if (state is null) return null;

        if (element.Children.FirstOrDefault() is CXValue value)
            state.SubstitutePropertyValue(Value, value);

        return state;
    }

    public override void Validate(ComponentState state, ComponentContext context)
    {
        base.Validate(state, context);

        if (context.KnownTypes.LabelBuilderType is null)
        {
            context.AddDiagnostic(
                Diagnostics.MissingTypeInAssembly,
                state.Source,
                nameof(context.KnownTypes.LabelBuilderType)
            );
        }

        if (!state.HasChildren)
        {
            context.AddDiagnostic(
                Diagnostics.MissingLabelComponent,
                state.Source
            );

            return;
        }

        if (state.Children.Count > 1)
        {
            var lower = state.Children[1].State.Source.Span.Start;
            var upper = state.Children.Last().State.Source.Span.End;

            context.AddDiagnostic(
                Diagnostics.TooManyChildrenInLabel,
                TextSpan.FromBounds(lower, upper)
            );
        }

        var labelInnerComponent = state.Children[0];

        if (!IsValidLabelChild(labelInnerComponent.Inner))
        {
            context.AddDiagnostic(
                Diagnostics.InvalidLabelChild,
                labelInnerComponent.State.Source
            );
        }
    }

    private static bool IsValidLabelChild(ComponentNode node)
        => node is IDynamicComponentNode
            or SelectMenuComponentNode
            or TextInputComponentNode
            or FileUploadComponentNode;

    public override string Render(ComponentState state, ComponentContext context)
    {
        var props = string.Join(
            ",\n",
            [
                state.RenderProperties(this, context),
                state.Children.FirstOrDefault()?.Render(context)
                    .PrefixIfSome("component: ")
            ]
        );

        return
            $"new {context.KnownTypes.LabelBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{
                props
                    .Prefix(4)
                    .WithNewlinePadding(4)
                    .WrapIfSome("\n")
                    .Map(x => $"({x})")
            }";
    }
}