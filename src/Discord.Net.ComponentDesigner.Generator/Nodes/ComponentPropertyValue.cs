using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Discord.CX.Nodes;

public sealed record ComponentPropertyValue(
    ComponentProperty Property,
    CXAttribute? Attribute
)
{
    private CXValue? _value;

    public CXValue? Value
    {
        get => _value ??= Attribute?.Value;
        init => _value = value;
    }

    public bool IsSpecified => Attribute is not null || HasValue;

    public bool HasValue => Value is not null;

    public bool TryGetLiteralValue(out string value)
    {
        switch (Value)
        {
            case CXValue.Scalar scalar:
                value = scalar.Value;
                return true;
            case CXValue.StringLiteral {HasInterpolations: false} literal:
                value = literal.Tokens.ToString();
                return true;

            default:
                value = string.Empty;
                return false;
        }
    }

    public void ReportPropertyConfigurationDiagnostics(
        ComponentContext context,
        ComponentState state,
        bool? optional = null,
        bool? requiresValue = null
    )
    {
        var isOptional = optional ?? Property.IsOptional;

        if (!isOptional && !IsSpecified)
        {
            context.AddDiagnostic(
                Diagnostics.MissingRequiredProperty,
                state.Source,
                state.OwningNode?.Inner.Name,
                Property.Name
            );
        }

        if (requiresValue.HasValue)
        {
            if (Value is null or CXValue.Invalid && requiresValue.Value)
            {
                context.AddDiagnostic(
                    Diagnostics.MissingRequiredProperty,
                    Attribute ?? state.Source,
                    state.OwningNode?.Inner.Name,
                    Property.Name
                );
            }
        }
    }
}
