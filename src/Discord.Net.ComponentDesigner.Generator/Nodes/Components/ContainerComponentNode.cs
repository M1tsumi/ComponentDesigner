using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ContainerComponentNode : ComponentNode
{
    public override string Name => "container";

    public override bool HasChildren => true;

    public ComponentProperty Id { get; }
    public ComponentProperty AccentColor { get; }
    public ComponentProperty Spoiler { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ContainerComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            AccentColor = new(
                "accentColor",
                isOptional: true,
                aliases: ["color", "accent"],
                renderer: Renderers.Color,
                dotnetPropertyName: "AccentColor"
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                requiresValue: false,
                renderer: Renderers.Boolean,
                dotnetPropertyName: "IsSpoiler"
            )
        ];
    }

    public override void Validate(ComponentState state, ComponentContext context)
    {
        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Inner))
            {
                context.AddDiagnostic(
                    Diagnostics.InvalidContainerChild,
                    child.State.Source,
                    child.Inner.Name
                );
            }
        }

        base.Validate(state, context);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or ActionRowComponentNode
            or TextDisplayComponentNode
            or SectionComponentNode
            or MediaGalleryComponentNode
            or SeparatorComponentNode
            or FileComponentNode;

    public override string Render(ComponentState state, ComponentContext context)
    {
        var props = state.RenderProperties(this, context, asInitializers: true);
        var children = state.RenderChildren(context);

        var init = new StringBuilder(props);

        if (!string.IsNullOrWhiteSpace(children))
        {
            if (!string.IsNullOrWhiteSpace(props)) init.Append(',').AppendLine();
            
            init.Append(
                $"""
                 Components =
                 [
                     {children.WithNewlinePadding(4)}
                 ]
                 """
            );
        }

        return
            $$"""
              new {{context.KnownTypes.ContainerBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(){{
                  init
                      .ToString()
                      .WithNewlinePadding(4)
                      .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                      .PostfixIfSome($"{Environment.NewLine}}}")
              }}
              """;
    }
//         => $$"""
//              new {{context.KnownTypes.ContainerBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(){{
//                  $"{
//                      state
//                          .RenderProperties(this, context, asInitializers: true)
//                          .PostfixIfSome(Environment.NewLine)
//                  }{
//                      state.RenderChildren(context)
//                          .Map(x =>
//                              $"""
//                               Components =
//                               [
//                                   {x.WithNewlinePadding(4)}
//                               ]
//                               """
//                          )
//                  }"
//                      .TrimEnd()
//                      .WithNewlinePadding(4)
//                      .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
//                      .PostfixIfSome($"{Environment.NewLine}}}")
//              }}
//              """;
}