using System;
using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class BoldTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Bold";

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
    ) => Join(RenderChildren(context, options)).Map(Build);

    private RenderedTextControlElement Build(RenderedTextControlElement inner)
    {
        var value = $"{inner.LeadingTrivia.ToIndentationOnly()}{inner.Value}".NormalizeIndentation();
        
        return new RenderedTextControlElement(
            element.LeadingTrivia,
            element.TrailingTrivia,
            inner.ValueHasNewLines,
            $"**{value}**"
        );
    }
}