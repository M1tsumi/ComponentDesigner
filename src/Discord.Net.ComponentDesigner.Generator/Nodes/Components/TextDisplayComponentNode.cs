using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class TextDisplayComponentNode : ComponentNode
{
    public override string Name => "text-display";

    public override IReadOnlyList<string> Aliases { get; } = ["text"];

    public ComponentProperty Content { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    protected override bool AllowChildrenInCX => true;

    public TextDisplayComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Content = new(
                "content",
                renderer: Renderers.String
            )
        ];
    }

    public override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var state = base.Create(context, diagnostics)!;

        if (context.CXNode is CXElement { Children.Count: 1 } element && element.Children[0] is CXValue value)
            state.SubstitutePropertyValue(Content, value);

        return state;
    }

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.TextDisplayBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                 x.PrefixIfSome(4)
                     .WithNewlinePadding(4)
                     .WrapIfSome(Environment.NewLine)
             })
             """
        );
}