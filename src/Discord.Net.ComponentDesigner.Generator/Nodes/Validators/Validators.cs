using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Discord.CX.Nodes;

public static class Validators
{
    public static void Range(
        IComponentContext context,
        ComponentState state,
        ComponentProperty lower,
        ComponentProperty upper,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var lowerValue = state.GetProperty(lower);
        var upperValue = state.GetProperty(upper);

        if (!lowerValue.HasValue || !upperValue.HasValue) return;

        if (
            !TryGetIntValue(context, lowerValue.Value, out var lowerInt) ||
            !TryGetIntValue(context, upperValue.Value, out var upperInt)
        ) return;

        if (lowerInt > upperInt)
        {
            diagnostics.Add(
                Diagnostics.InvalidRange(lower.Name, upper.Name),
                (ICXNode?)lowerValue.Attribute ?? lowerValue.Value!
            );
        }
    }

    public static PropertyValidator IntRange(
        int? lower = null,
        int? upper = null
    ) => Range(false, lower, upper);

    public static PropertyValidator StringRange(
        int? lower = null,
        int? upper = null
    ) => Range(true, lower, upper);

    public static PropertyValidator Range(
        bool asString,
        int? lower = null,
        int? upper = null
    )
    {
        Debug.Assert(lower.HasValue || upper.HasValue);

        var bounds = (lower, upper) switch
        {
            (not null, null) => $"at least {lower}",
            (null, not null) => $"at most {upper}",
            (not null, not null) => $"between {lower} and {upper}",
            _ => string.Empty
        };

        return (context, propertyValue, diagnostics) =>
        {
            switch (propertyValue.Value)
            {
                case null or CXValue.Invalid: return;
                case CXValue.Interpolation interpolation:
                    var constant = context.GetInterpolationInfo(interpolation).Constant;

                    if (!constant.HasValue) break;

                    if (constant.Value is string constantValue && asString)
                        Check(constantValue.Length);
                    else if (constant.Value is int integer && !asString)
                        Check(integer);
                    break;

                case CXValue.Multipart { HasInterpolations: false, Tokens: var tokens } when !asString:
                    if (int.TryParse(tokens.ToString(), out var val))
                        Check(val);
                    break;

                case CXValue.Multipart literal when asString:
                {
                    int? length = null;

                    foreach (var token in literal.Tokens)
                    {
                        switch (token.Kind)
                        {
                            case CXTokenKind.Text:
                                length ??= 0;
                                length += token.Span.Length;
                                break;
                            case CXTokenKind.Interpolation
                                when literal.Document.TryGetInterpolationIndex(token, out var index):
                                var info = context.GetInterpolationInfo(index);
                                if (info.Constant.Value is string str)
                                {
                                    length ??= 0;
                                    length += str.Length;
                                }

                                break;
                        }
                    }

                    if (length.HasValue) Check(length.Value);

                    break;
                }
                case CXValue.Scalar scalar:
                {
                    int length;

                    if (asString) length = scalar.Value.Length;
                    else if (!int.TryParse(scalar.Value, out length))
                        return;

                    Check(length);

                    return;
                }
            }

            void Check(int length)
            {
                if (
                    length > upper || length < lower
                )
                {
                    diagnostics.Add(
                        Diagnostics.OutOfRange(
                            propertyValue.Property.Name,
                            bounds + (asString ? " characters in length" : string.Empty)
                        ),
                        propertyValue.Value
                    );
                }
            }
        };
    }

    private static bool TryGetIntValue(IComponentContext context, CXValue? value, out int result)
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                var constant = context.GetInterpolationInfo(interpolation).Constant;

                if (!constant.HasValue) break;

                switch (constant.Value)
                {
                    case string constantValue when int.TryParse(constantValue, out result):
                        return true;
                    case int integer:
                        result = integer;
                        return true;
                }

                break;
            case CXValue.Multipart { HasInterpolations: false, Tokens: var tokens }:
                return int.TryParse(tokens.ToString(), out result);

            case CXValue.Scalar scalar:
                return int.TryParse(scalar.Value, out result);
        }

        result = 0;
        return false;
    }
}