using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public enum HeadingTextControlElementVariant
{
    H1,
    H2,
    H3
}

public sealed class HeadingTextControlElement(
    CXElement element,
    HeadingTextControlElementVariant variant,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Heading";
    public override IReadOnlyList<TextControlElement> Children => children;

    public override IReadOnlyList<Type>? AllowedChildren { get; } =
    [
        typeof(BoldTextControlElement),
        typeof(CodeTextControlElement),
        typeof(ItalicTextControlElement),
        typeof(LinkTextControlElement),
        typeof(ListItemTextControlElement),
        typeof(StrikethroughTextControlElement),
        typeof(SubtextTextControlElement),
        typeof(UnderlineTextControlElement),
        typeof(QuoteTextControlElement),
        typeof(SpoilerTextControlElement),
        typeof(LineBreakTextControlElement),
        typeof(TimeTagTextControlElement),
    ];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => VariantText
        .Combine(RenderChildren(context, options))
        .Map(tuple => Build(tuple.Left, tuple.Right));

    private RenderedTextControlElement Build(
        string prefix,
        EquatableArray<RenderedTextControlElement> children
    ) => new(
        element.LeadingTrivia,
        EnsureLineBreaks(element.TrailingTrivia),
        false,
        $"{prefix} {RenderChildrenWithoutNewlines(children)}"
    );

    private Result<string> VariantText
        => variant switch
        {
            HeadingTextControlElementVariant.H1 => "#",
            HeadingTextControlElementVariant.H2 => "##",
            HeadingTextControlElementVariant.H3 => "###",
            _ => new DiagnosticInfo(
                Diagnostics.UnknownComponent(element.Identifier),
                element
            )
        };
}