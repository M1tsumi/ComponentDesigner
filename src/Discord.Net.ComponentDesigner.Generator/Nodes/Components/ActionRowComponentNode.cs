using System;
using Discord.CX.Parser;
using Discord.CX.Nodes.Components.SelectMenus;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class ActionRowComponentNode : ComponentNode
{
    public override string Name => "action-row";

    public override IReadOnlyList<string> Aliases { get; } = ["row"];

    public override bool HasChildren => true;

    public ComponentProperty Id { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public ActionRowComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id
        ];
    }

    public override void Validate(ComponentState state, ComponentContext context)
    {
        if (!state.HasChildren)
        {
            context.AddDiagnostic(
                Diagnostics.EmptyActionRow,
                state.Source
            );

            base.Validate(state, context);
            return;
        }

        switch (state.Children[0].Inner)
        {
            case ButtonComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    if (rest.Inner is not ButtonComponentNode)
                    {
                        context.AddDiagnostic(
                            Diagnostics.ActionRowInvalidChild,
                            rest.State.Source
                        );
                    }
                }

                foreach (var extra in state.Children.Skip(5))
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        extra.State.Source
                    );
                }
                
                break;
            case SelectMenuComponentNode:
                foreach (var rest in state.Children.Skip(1))
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;

            case InterleavedComponentNode: break;

            default:
                foreach (
                    var rest
                    in state.Children.Where(x => !IsValidChild(x.Inner))
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.ActionRowInvalidChild,
                        rest.State.Source
                    );
                }

                break;
        }

        base.Validate(state, context);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is ButtonComponentNode
            or SelectMenuComponentNode
            or IDynamicComponentNode;

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
              new {{context.KnownTypes.ActionRowBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(){{
                  init
                      .ToString()
                      .WithNewlinePadding(4)
                      .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                      .PostfixIfSome($"{Environment.NewLine}}}")
              }}
              """;
    }
    //         => $$"""
//              new {{context.KnownTypes.ActionRowBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(){{
//                 $"{
//                     state
//                         .RenderProperties(this, context, asInitializers: true)
//                         .PostfixIfSome(Environment.NewLine)
//                 }{
//                     state
//                         .RenderChildren(context)
//                         .Map(x =>
//                             $"""
//                              Components =
//                              [
//                                  {x.WithNewlinePadding(4)}
//                              ]
//                              """
//                         )
//                 }"
//                     .TrimEnd()
//                     .WithNewlinePadding(4)
//                     .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
//                     .PostfixIfSome($"{Environment.NewLine}}}")
//             }}
//              """;
}