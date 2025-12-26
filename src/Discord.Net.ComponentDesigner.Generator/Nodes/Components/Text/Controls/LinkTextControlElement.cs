using System;
using System.Collections.Generic;
using System.Text;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class LinkTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Link";
    public override IReadOnlyList<TextControlElement> Children => children;

    public override IReadOnlyList<Type>? AllowedChildren { get; } =
    [
        typeof(BoldTextControlElement),
        typeof(CodeTextControlElement),
        typeof(ItalicTextControlElement),
        typeof(ListItemTextControlElement),
        typeof(StrikethroughTextControlElement),
        typeof(SubtextTextControlElement),
        typeof(UnderlineTextControlElement),
        typeof(SpoilerTextControlElement),
        typeof(LineBreakTextControlElement),
        typeof(TimeTagTextControlElement),
    ];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => ExtractLink(element, context, options)
        .Combine(
            Join(RenderChildren(context, options)),
            Build
        );

    private RenderedTextControlElement Build(string link, RenderedTextControlElement inner)
    {
        var value = $"{inner.LeadingTrivia.ToIndentationOnly()}{inner.Value}".NormalizeIndentation();

        return new RenderedTextControlElement(
            element.LeadingTrivia,
            element.TrailingTrivia,
            inner.ValueHasNewLines,
            $"[{value}]({link})"
        );
    }

    private static Result<string> ExtractLink(
        CXElement element,
        IComponentContext context,
        TextControlRenderingOptions options
    )
    {
        CXAttribute? linkProperty = null;

        foreach (var attribute in element.Attributes)
        {
            if (attribute.Identifier is "href" or "url")
            {
                if (linkProperty is not null)
                {
                    return new DiagnosticInfo(
                        Diagnostics.DuplicateProperty(linkProperty.Identifier, attribute.Identifier),
                        attribute
                    );
                }

                linkProperty = attribute;
            }
        }

        if (linkProperty?.Value is null)
        {
            return new DiagnosticInfo(
                Diagnostics.MissingRequiredProperty(element.Identifier, "href"),
                element
            );
        }

        switch (linkProperty.Value)
        {
            case CXValue.Scalar scalar:
                return scalar.Value;
            case CXValue.Interpolation interpolation:
                return
                    $"{options.StartInterpolation}{context.GetDesignerValue(interpolation)}{options.EndInterpolation}";
            case CXValue.Multipart multipart:
                var sb = new StringBuilder();

                foreach (var part in multipart.Tokens)
                {
                    switch (part.Kind)
                    {
                        case CXTokenKind.Text:
                            sb.Append(part.Value);
                            break;
                        case CXTokenKind.Interpolation when part.InterpolationIndex is { } index:
                            sb.Append(options.StartInterpolation)
                                .Append(context.GetDesignerValue(index))
                                .Append(options.EndInterpolation);
                            break;
                        default:
                            return new DiagnosticInfo(
                                Diagnostics.InvalidPropertyValueSyntax("text or interpolation"),
                                part
                            );
                    }
                }

                return sb.ToString();
            default:
                return new DiagnosticInfo(
                    Diagnostics.InvalidPropertyValueSyntax("text or interpolation"),
                    linkProperty.Value
                );
        }
    }
}