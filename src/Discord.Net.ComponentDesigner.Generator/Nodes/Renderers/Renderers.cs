using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public static class Renderers
{
    public static PropertyRenderer CreateDefault(ComponentProperty property)
    {
        return (context, value) => { return string.Empty; };
    }

    public static PropertyRenderer CreateRenderer(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return Renderers.String;

        // TODO: more ways to extract renderers

        return (context, propValue) =>
        {
            switch (propValue.Value)
            {
                case CXValue.Interpolation interpolation:
                    var info = context.GetInterpolationInfo(interpolation);
                    if (
                        !context.Compilation.HasImplicitConversion(
                            info.Symbol,
                            type
                        )
                    )
                    {
                        context.AddDiagnostic(
                            Diagnostics.TypeMismatch,
                            interpolation,
                            info.Symbol,
                            type
                        );
                    }

                    return context.GetDesignerValue(
                        interpolation,
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                default: return "default";
            }
        };
    }

    public static bool IsLoneInterpolatedLiteral(
        ComponentContext context,
        CXValue.Multipart literal,
        out DesignerInterpolationInfo info)
    {
        if (
            literal is { HasInterpolations: true, Tokens.Count: 1 } &&
            literal.Document.TryGetInterpolationIndex(literal.Tokens[0], out var index)
        )
        {
            info = context.GetInterpolationInfo(index);
            return true;
        }

        info = null!;
        return false;
    }

    public static string ComponentAsProperty(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Element when propertyValue.Node is not null:
                return propertyValue.Node.Render(context);
            case CXValue.Interpolation interpolation:
                // ensure its a component builder
                var info = context.GetInterpolationInfo(interpolation);

                if (
                    !InterleavedComponentNode.IsValidInterleavedType(
                        info.Symbol,
                        context.Compilation,
                        out var interleavedKind
                    )
                )
                {
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        interpolation,
                        info.Symbol,
                        $"{context.KnownTypes.IMessageComponentBuilderType} | {context.KnownTypes.MessageComponentType}"
                    );

                    return string.Empty;
                }

                var source = context.GetDesignerValue(
                    info,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );


                // ensure we can convert it to a builder
                if (
                    !InterleavedComponentNode.TryConvert(
                        source,
                        interleavedKind,
                        InterleavedKind.IMessageComponentBuilder,
                        out var converted
                    )
                )
                {
                    source = interleavedKind switch
                    {
                        InterleavedKind.CXMessageComponent => $"{source}.Builders.First()",
                        InterleavedKind.MessageComponent => $"{source}.Components.First().ToBuilder()",
                        InterleavedKind.CollectionOfCXComponents => $"{source}.First().Builders.First()",
                        InterleavedKind.CollectionOfIMessageComponentBuilders => $"{source}.First()",
                        InterleavedKind.CollectionOfIMessageComponents => $"{source}.First().ToBuilder()",
                        InterleavedKind.CollectionOfMessageComponents =>
                            $"{source}.First().Components.First().ToBuilder",
                        _ => string.Empty
                    };

                    if (source != string.Empty)
                    {
                        context.AddDiagnostic(
                            Diagnostics.CardinalityForcedToRuntime,
                            interpolation,
                            info.Symbol.ToDisplayString()
                        );
                    }
                    else
                    {
                        context.AddDiagnostic(
                            Diagnostics.InvalidChildComponentCardinality,
                            interpolation,
                            propertyValue.Property.Name
                        );
                    }
                }

                return source;
            default:
                if (propertyValue.Value is not null)
                {
                    context.AddDiagnostic(
                        Diagnostics.InvalidPropertyValueSyntax,
                        propertyValue.Value,
                        "interpolation"
                    );
                }

                return string.Empty;
        }
    }

    public static string UnfurledMediaItem(ComponentContext context, ComponentPropertyValue propertyValue)
        =>
            $"new {context.KnownTypes.UnfurledMediaItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({String(context, propertyValue)})";

    public static string Integer(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Scalar scalar:
                return FromText(scalar.Value);

            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Multipart literal:
                if (!literal.HasInterpolations)
                    return FromText(literal.Tokens.ToString().Trim());

                if (IsLoneInterpolatedLiteral(context, literal, out var info))
                    return FromInterpolation(literal, info);

                return $"int.Parse({RenderStringLiteral(literal)})";
            default: return "default";
        }

        string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (info.Constant.Value is int || int.TryParse(info.Constant.Value?.ToString(), out _))
                return info.Constant.Value!.ToString();

            if (info.Constant.HasValue)
            {
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    owner,
                    info.Constant.Value!.GetType().Name,
                    "int"
                );
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Int32)
                )
            )
            {
                return context.GetDesignerValue(info, "int");
            }

            return $"int.Parse({context.GetDesignerValue(info)})";
        }

        string FromText(string text)
        {
            if (int.TryParse(text, out _)) return text;

            return $"int.Parse({ToCSharpString(text)})";
        }
    }

    public static string Boolean(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value.Trim().ToLowerInvariant());

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString().Trim().ToLowerInvariant());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                return $"bool.Parse({context.GetDesignerValue(info)})";
            
            case null when !propertyValue.Property.RequiresValue:
                return "true";
            
            default: return "default";
        }

        string FromInterpolation(ICXNode node, DesignerInterpolationInfo info)
        {
            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Boolean)
                )
            )
            {
                return context.GetDesignerValue(info, "bool");
            }

            if (info.Constant.Value is bool b) return b ? "true" : "false";

            if (info.Constant.Value?.ToString().Trim().ToLowerInvariant() is { } str and ("true" or "false"))
                return str;

            return $"bool.Parse({context.GetDesignerValue(info)})";
        }

        string FromText(ICXNode owner, string value)
        {
            if (value is not "true" and not "false")
            {
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    owner,
                    "string",
                    "bool"
                );
            }

            return value;
        }
    }

    private static readonly Dictionary<string, string> _colorPresets = [];

    private static bool TryGetColorPreset(
        ComponentContext context,
        string value,
        out string fieldName)
    {
        var colorSymbol = context.KnownTypes.ColorType;

        if (colorSymbol is null)
        {
            fieldName = null!;
            return false;
        }

        if (_colorPresets.Count is 0)
        {
            foreach (
                var field
                in colorSymbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(x =>
                        x.Type.Equals(colorSymbol, SymbolEqualityComparer.Default) &&
                        x.IsStatic
                    )
            )
            {
                _colorPresets[field.Name.ToLowerInvariant()] = field.Name;
            }
        }

        return _colorPresets.TryGetValue(value.ToLowerInvariant(), out fieldName);
    }

    public static string Color(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        var colorSymbol = context.KnownTypes.ColorType;
        var qualifiedColor = colorSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                var info = context.GetInterpolationInfo(interpolation);

                if (
                    info.Symbol is not null &&
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        colorSymbol
                    )
                )
                {
                    return context.GetDesignerValue(
                        interpolation,
                        qualifiedColor
                    );
                }

                if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        context.Compilation.GetSpecialType(SpecialType.System_UInt32)
                    )
                )
                {
                    return $"new {qualifiedColor}({context.GetDesignerValue(interpolation, "uint")})";
                }


                if (info.Constant.Value is string str)
                {
                    if (TryGetColorPreset(context, str, out var preset))
                        return $"{qualifiedColor}.{preset}";

                    if (TryParseHexColor(str, out var hexColor))
                        return $"new {qualifiedColor}({hexColor})";
                }
                else if (info.Constant.HasValue && uint.TryParse(info.Constant.Value?.ToString(), out var hexColor))
                {
                    return $"new {qualifiedColor}({hexColor})";
                }

                return $"{qualifiedColor}.Parse({context.GetDesignerValue(interpolation)})";
            case CXValue.Scalar scalar:
                return UseLibraryParser(scalar.Value);

            case CXValue.Multipart stringLiteral:
                if (
                    !stringLiteral.HasInterpolations &&
                    TryGetColorPreset(context, stringLiteral.Tokens.ToString(), out var presetName)
                ) return $"{qualifiedColor}.{presetName}";

                return UseLibraryParser(RenderStringLiteral(stringLiteral));
            default: return "default";
        }


        string UseLibraryParser(string source)
            => $"{qualifiedColor}.Parse({source})";

        static bool TryParseHexColor(string hexColor, out uint color)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
            {
                color = 0;
                return false;
            }

            if (hexColor[0] is '#')
                hexColor = hexColor.Substring(1);
            else if (hexColor.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexColor = hexColor.Substring(2);

            return uint.TryParse(hexColor, NumberStyles.HexNumber, null, out color);
        }
    }

    public static string Snowflake(ComponentContext context, ComponentPropertyValue propertyValue)
        => Snowflake(context, propertyValue.Value);

    public static string Snowflake(ComponentContext context, CXValue? value)
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                var targetType = context.Compilation.GetSpecialType(SpecialType.System_UInt64);

                var interpolationInfo = context.GetInterpolationInfo(interpolation);

                if (
                    interpolationInfo.Symbol is not null &&
                    context.Compilation.HasImplicitConversion(interpolationInfo.Symbol, targetType)
                )
                {
                    return $"designer.GetValue<ulong>({interpolation.InterpolationIndex})";
                }

                return UseParseMethod($"designer.GetValueAsString({interpolation.InterpolationIndex})");

            case CXValue.Scalar scalar:
                return FromText(scalar.Value.Trim());

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral.Tokens.ToString().Trim());

                return UseParseMethod(RenderStringLiteral(stringLiteral));

            default: return "default";
        }

        string FromText(string text)
        {
            if (ulong.TryParse(text, out _)) return text;

            return UseParseMethod(ToCSharpString(text));
        }

        static string UseParseMethod(string input)
            => $"ulong.Parse({input})";
    }

    public static string String(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            default: return "string.Empty";

            case CXValue.Interpolation interpolation:
                if (context.GetInterpolationInfo(interpolation).Constant.Value is string constant)
                    return ToCSharpString(constant);

                return context.GetDesignerValue(interpolation);
            case CXValue.Multipart literal: return RenderStringLiteral(literal);
            case CXValue.Scalar scalar:
                return ToCSharpString(scalar.Value.Trim());
        }
    }

    private static string RenderStringLiteral(CXValue.Multipart literal)
    {
        if (literal.Tokens.Count is 0) return "string.Empty";

        var sb = new StringBuilder();

        var parts = literal.Tokens
            .Where(x => x.Kind is CXTokenKind.Text)
            .Select(x => x.Value)
            .ToArray();

        if (parts.Length is 0) return string.Empty;

        parts[0] = parts[0].TrimStart();

        parts[parts.Length - 1] = parts[parts.Length - 1].TrimEnd();

        var quoteCount = parts.Select(x => x.Count(x => x is '"')).Max() + 1;

        var dollars = new string(
            '$',
            parts.Select(GetInterpolationDollarRequirement).Max() +
            (
                literal.Tokens.Any(x => x.Kind is CXTokenKind.Interpolation)
                    ? 1
                    : 0
            )
        );

        var startInterpolation = dollars.Length > 0
            ? new string('{', dollars.Length)
            : string.Empty;

        var endInterpolation = dollars.Length > 0
            ? new string('}', dollars.Length)
            : string.Empty;

        var isMultiline = literal.Tokens.Any(x => x.FullValue.Contains("\n"));

        foreach (var token in literal.Tokens)
        {
            switch (token.Kind)
            {
                case CXTokenKind.Text:
                    sb
                        .Append(token.LeadingTrivia)
                        .Append(EscapeBackslashes(token.Value))
                        .Append(token.TrailingTrivia);
                    break;
                case CXTokenKind.Interpolation:
                    var index = Array.IndexOf(literal.Document.InterpolationTokens, token);

                    // TODO: handle better
                    if (index is -1) throw new InvalidOperationException();

                    sb
                        .Append(token.LeadingTrivia)
                        .Append(startInterpolation)
                        .Append($"designer.GetValueAsString({index})")
                        .Append(endInterpolation)
                        .Append(token.TrailingTrivia);
                    break;

                default: continue;
            }
        }

        // normalize the value indentation
        var value = sb.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        sb.Clear();

        if (isMultiline)
        {
            sb.AppendLine();
            quoteCount = Math.Max(quoteCount, 3);
        }

        var quotes = new string('"', quoteCount);

        sb.Append(dollars).Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(value);

        if (isMultiline) sb.AppendLine();
        sb.Append(quotes);

        return sb.ToString();

        static int GetInterpolationDollarRequirement(string part)
        {
            var result = 0;

            var count = 0;
            char? last = null;

            foreach (var ch in part)
            {
                if (ch is '{' or '}')
                {
                    if (last is null)
                    {
                        last = ch;
                        count = 1;
                        continue;
                    }

                    if (last == ch)
                    {
                        count++;
                        continue;
                    }
                }

                if (count > 0)
                {
                    result = Math.Max(result, count);
                    last = null;
                    count = 0;
                }
            }

            return result;
        }
    }

    public static string ToCSharpString(string text)
    {
        var quoteCount = (GetSequentialQuoteCount(text) + 1) switch
        {
            2 => 3,
            var r => r
        };

        text = text.NormalizeIndentation().Trim(['\r', '\n']);

        var isMultiline = text.Contains('\n');

        if (isMultiline)
            quoteCount = Math.Max(3, quoteCount);

        var quotes = new string('"', quoteCount);

        var sb = new StringBuilder();

        sb.Append(quotes);

        if (isMultiline) sb.AppendLine();

        sb.Append(text);

        if (isMultiline)
            sb.AppendLine();

        sb.Append(quotes);

        return sb.ToString();
    }

    private static string EscapeBackslashes(string text)
        => text.Replace("\\", @"\\");

    private static int GetSequentialQuoteCount(string text)
    {
        var result = 0;
        var count = 0;

        foreach (var ch in text)
        {
            if (ch is '"')
            {
                count++;
                continue;
            }

            if (count > 0)
            {
                result = Math.Max(result, count);
                count = 0;
            }
        }

        return result;
    }

    public static PropertyRenderer RenderEnum(string fullyQualifiedName)
    {
        ITypeSymbol? symbol = null;
        Dictionary<string, string> variants = [];

        return (context, propertyValue) =>
        {
            if (symbol is null || variants.Count is 0)
            {
                symbol = context.Compilation.GetTypeByMetadataName(fullyQualifiedName);

                if (symbol is null) throw new InvalidOperationException($"Unknown type '{fullyQualifiedName}'");

                if (symbol.TypeKind is not TypeKind.Enum)
                    throw new InvalidOperationException($"'{symbol}' is not an enum type.");

                variants = symbol
                    .GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(x => x.Type.Equals(symbol, SymbolEqualityComparer.Default))
                    .ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Name);
            }

            switch (propertyValue.Value)
            {
                case CXValue.Scalar scalar:
                    return FromText(scalar.Value.Trim(), scalar);
                case CXValue.Interpolation interpolation:
                    return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));
                case CXValue.Multipart literal:
                    if (!literal.HasInterpolations)
                        return FromText(literal.Tokens.ToString().Trim().ToLowerInvariant(), literal);

                    if (IsLoneInterpolatedLiteral(context, literal, out var info))
                        return FromInterpolation(literal, info);

                    return
                        $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({RenderStringLiteral(literal)})";
                default: return "default";
            }

            string FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
            {
                if (
                    context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        symbol
                    )
                )
                {
                    return context.GetDesignerValue(
                        info,
                        symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                }

                if (info.Constant.Value?.ToString() is { } str)
                {
                    return FromText(str.Trim().ToLowerInvariant(), owner);
                }

                return
                    $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({context.GetDesignerValue(info)})";
            }

            string FromText(string text, ICXNode owner)
            {
                if (variants.TryGetValue(text, out var name))
                    return $"{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{name}";

                context.AddDiagnostic(
                    Diagnostics.InvalidEnumVariant,
                    owner,
                    text,
                    symbol.ToDisplayString()
                );

                return
                    $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({ToCSharpString(text)})";
            }
        };
    }

    public static string Emoji(ComponentContext context, ComponentPropertyValue propertyValue)
    {
        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                var interpolationInfo = context.GetInterpolationInfo(interpolation);

                if (
                    interpolationInfo.Symbol is not null &&
                    context.Compilation.HasImplicitConversion(
                        interpolationInfo.Symbol,
                        context.KnownTypes.IEmoteType
                    )
                )
                {
                    return context.GetDesignerValue(
                        interpolation,
                        $"{context.KnownTypes.IEmoteType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}"
                    );
                }

                return UseLibraryParser(
                    context.GetDesignerValue(interpolation)
                );

            case CXValue.Scalar scalar:
                LightlyValidateEmote(scalar.Value, scalar);
                return UseLibraryParser(ToCSharpString(scalar.Value));

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    LightlyValidateEmote(stringLiteral.Tokens.ToString(), stringLiteral.Tokens);

                return UseLibraryParser(RenderStringLiteral(stringLiteral));

            default: return "null";
        }

        void LightlyValidateEmote(string emote, ICXNode node)
        {
            if (!IsDiscordEmote.IsMatch(emote) && !IsEmoji.IsMatch(emote))
            {
                context.AddDiagnostic(
                    Diagnostics.PossibleInvalidEmote,
                    node,
                    emote
                );
            }
        }

        static string UseLibraryParser(string source)
            => $"""
                global::Discord.Emoji.TryParse({source}, out var emoji)
                    ? (global::Discord.IEmote)emoji
                    : global::Discord.Emote.Parse({source})
                """;
    }

    private static readonly Regex IsEmoji = new Regex(@"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled);

    private static readonly Regex IsDiscordEmote = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}