using System;
using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class SubtextTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Sub text";
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
        typeof(UnderlineTextControlElement),
        typeof(HeadingTextControlElement),
        typeof(QuoteTextControlElement),
        typeof(SpoilerTextControlElement),
        typeof(LineBreakTextControlElement),
        typeof(TimeTagTextControlElement),
    ];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => RenderChildren(context, options)
        .Map(Build);

    private RenderedTextControlElement Build(EquatableArray<RenderedTextControlElement> children)
        => new(
            element.LeadingTrivia,
            EnsureLineBreaks(element.TrailingTrivia),
            false,
            $"-# {RenderChildrenWithoutNewlines(children)}"
        );
}