using System;
using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class QuoteTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Quote";

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
        typeof(SpoilerTextControlElement)
    ];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => Join(RenderChildren(context, options)).Map(Build);
    
    private RenderedTextControlElement Build(RenderedTextControlElement inner)
    {
        var value = inner.Value;

        if (inner.ValueHasNewLines)
            value = value.Replace("\n", "\n> ");
        
        value = $"{inner.LeadingTrivia.ToIndentationOnly()}{value}".NormalizeIndentation();
        
        return new RenderedTextControlElement(
            element.LeadingTrivia,
            element.TrailingTrivia,
            inner.ValueHasNewLines,
            $"> {value}"
        );
    }
}