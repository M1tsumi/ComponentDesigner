using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public enum ListTextControlElementKind
{
    Unordered,
    Ordered
}

public sealed class ListTextControlElement(
    CXElement element,
    ListTextControlElementKind kind,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "List";
    public override IReadOnlyList<TextControlElement> Children => children;

    public override IReadOnlyList<Type>? AllowedChildren { get; } =
    [
        typeof(BoldTextControlElement),
        typeof(CodeTextControlElement),
        typeof(ItalicTextControlElement),
        typeof(LinkTextControlElement),
        typeof(ListItemTextControlElement),
        typeof(ListTextControlElement),
        typeof(StrikethroughTextControlElement),
        typeof(SubtextTextControlElement),
        typeof(UnderlineTextControlElement),
        typeof(QuoteTextControlElement),
        typeof(SpoilerTextControlElement),
        typeof(LineBreakTextControlElement),
    ];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => Build(RenderChildrenItems(context, options));

    private Result<RenderedTextControlElement> Build(
        Result<EquatableArray<RenderedTextControlElement>> children
    ) => JoinWithTrimmedTrivia(children).Map(x => x with
    {
        LeadingTrivia = element.LeadingTrivia,
        TrailingTrivia = element.TrailingTrivia
    });

    private Result<EquatableArray<RenderedTextControlElement>> RenderChildrenItems(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => kind switch
    {
        ListTextControlElementKind.Ordered => RenderChildren(context, options).Map(BuildOrderedListChildren),
        ListTextControlElementKind.Unordered => RenderChildren(context, options).Map(BuildUnorderedListChildren),
        _ => new DiagnosticInfo(
            Diagnostics.UnknownComponent(element.Identifier),
            element
        )
    };

    private EquatableArray<RenderedTextControlElement> BuildUnorderedListChildren(
        EquatableArray<RenderedTextControlElement> renderedChildren
    )
    {
        var result = new RenderedTextControlElement[renderedChildren.Count];
        const string pad = "  ";
        const string liPrefix = "- ";

        for (var i = 0; i < renderedChildren.Count; i++)
        {
            var child = children[i];
            var renderedChild = renderedChildren[i];

            var prefix = child is ListItemTextControlElement
                ? liPrefix
                : pad;

            result[i] = renderedChild with
            {
                Value = $"{prefix}{renderedChild.Value}",
                LeadingTrivia = renderedChild.LeadingTrivia.NewlinesOnly()
            };
        }

        return [..result];
    }

    private EquatableArray<RenderedTextControlElement> BuildOrderedListChildren(
        EquatableArray<RenderedTextControlElement> renderedChildren
    )
    {
        var orderNumber = 1;
        var itemCount = children.Count(x => x is ListItemTextControlElement);

        var result = new RenderedTextControlElement[renderedChildren.Count];

        var padWidth = Math.Max(
            3,
            (int)Math.Floor(Math.Log10(itemCount)) + 1
        );

        var pad = new string(' ', padWidth + 3);

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var renderedChild = renderedChildren[i];

            var prefix = child is ListItemTextControlElement
                ? $"{$"{orderNumber++}".PadLeft(padWidth)}. "
                : pad;

            result[i] = renderedChild with
            {
                Value = $"{prefix}{renderedChild.Value}",
                LeadingTrivia = renderedChild.LeadingTrivia.NewlinesOnly()
            };
        }

        return [..result];
    }
}