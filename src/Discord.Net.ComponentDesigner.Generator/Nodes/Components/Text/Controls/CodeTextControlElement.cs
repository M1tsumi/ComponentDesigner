using System;
using System.Collections.Generic;
using Discord.CX.Parser;
using Discord.Net.ComponentDesignerGenerator.Utils;

namespace Discord.CX.Nodes.Components.Controls;

public sealed class CodeTextControlElement(
    CXElement element,
    IReadOnlyList<TextControlElement> children
) : TextControlElement(element)
{
    public override string FriendlyName => "Codeblock";
    public override IReadOnlyList<TextControlElement> Children => children;

    public override IReadOnlyList<Type>? AllowedChildren { get; } = [];
    
    protected override Result<RenderedTextControlElement> Render(
        IComponentContext context,
        TextControlRenderingOptions options
    ) => GetFormattingOptions(element, context, options)
        .Combine(
            Join(RenderChildren(context, options)),
            Build
        );

    private RenderedTextControlElement Build(
        FormatOptions options,
        RenderedTextControlElement children
    )
    {
        var isInline = options.IsInline ?? !children.HasNewlines;
        var childrenValue =
        (
            $"{children.LeadingTrivia.TrimLeadingSyntaxIndentation()}" +
            $"{children.Value}" +
            $"{children.TrailingTrivia.TrimTrailingSyntaxIndentation()}"
        ).NormalizeIndentation();

        var value = isInline
            ? $"`{childrenValue}`"
            : $"""
               ```{options.Language ?? string.Empty}
               {childrenValue}
               ```
               """;

        return new(
            element.LeadingTrivia,
            element.TrailingTrivia,
            children.ValueHasNewLines || !isInline,
            value
        );
    }

    private readonly record struct FormatOptions(
        bool? IsInline,
        string? Language
    );

    private static Result<FormatOptions> GetFormattingOptions(
        CXElement element,
        IComponentContext context,
        TextControlRenderingOptions options
    )
    {
        bool? isInline = null;
        string? language = null;

        // TODO: warn about duplicate attributes and unknown attributes
        foreach (var attribute in element.Attributes)
        {
            switch (attribute.Identifier)
            {
                case "inline":
                {
                    if (attribute.Value is null)
                    {
                        isInline = true;
                        continue;
                    }

                    if (!attribute.Value.TryGetConstantValue(context, out var constantValue))
                    {
                        return new DiagnosticInfo(
                            Diagnostics.ExpectedAConstantValue,
                            attribute
                        );
                    }

                    var value = constantValue.ToLowerInvariant();

                    if (value is "true" or "false")
                    {
                        isInline = value is "true";
                        continue;
                    }

                    return new DiagnosticInfo(
                        Diagnostics.InvalidPropertyValueSyntax("true or false"),
                        attribute.Value
                    );
                }
                case "lang" or "language":
                    if (attribute.Value is null)
                    {
                        return new DiagnosticInfo(
                            Diagnostics.MissingRequiredProperty("code", "language"),
                            attribute
                        );
                    }

                    var textValue = ToTextBasedValue(attribute.Value, context, options);

                    if (!textValue.HasResult) return textValue.Diagnostics;

                    language = textValue.Value;
                    break;
            }
        }

        return new FormatOptions(isInline, language);
    }
}