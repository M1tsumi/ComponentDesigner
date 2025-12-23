using System;
using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class LineBreakTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Line Break";

    public override void Validate(IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        if (children.Count > 0)
        {
            diagnostics.Add(
                Diagnostics.ComponentDoesntAllowChildren(FriendlyName),
                element
            );
        }
    }

    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => new RenderedTextControlElement(
        element.LeadingTrivia,
        element.TrailingTrivia,
        true,
        Environment.NewLine
    );
}