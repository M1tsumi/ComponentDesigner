using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

using RenderResult = Result<string>;
using static Result<string>;
using static DiagnosticInfo;

public static class Renderers
{
    public static RenderResult DefaultRenderer(
        IComponentContext context,
        IComponentPropertyValue value,
        PropertyRenderingOptions options
    ) => "default";

    public static PropertyRenderer CreateRenderer(
        Compilation compilation,
        ITypeSymbol type
    )
    {
        if (type.SpecialType == SpecialType.System_String)
            return Renderers.String;

        if (type.SpecialType is SpecialType.System_Int32)
            return Integer;

        if (compilation.GetKnownTypes().ColorType!.Equals(type, SymbolEqualityComparer.Default))
            return Color;

        // TODO: more ways to extract renderers
        return (context, propValue, options) =>
        {
            switch (propValue.Value)
            {
                case CXValue.Interpolation interpolation:
                    var builder = new Builder();

                    var info = context.GetInterpolationInfo(interpolation);

                    if (
                        !context.Compilation.HasImplicitConversion(
                            info.Symbol,
                            type
                        )
                    )
                    {
                        builder.AddDiagnostic(
                            Diagnostics.TypeMismatch(type.ToDisplayString(), info.Symbol!.ToDisplayString()),
                            interpolation
                        );
                    }

                    return builder.WithValue(
                        context.GetDesignerValue(
                            interpolation,
                            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        )
                    );
                default:
                    return FromDiagnostic(
                        Diagnostics.TypeMismatch("<interpolation>", propValue.Value?.GetType().Name ?? "unknown"),
                        propValue.Span
                    );
            }
        };
    }

    public static bool IsLoneInterpolatedLiteral(
        IComponentContext context,
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

    public static Result<string> ComponentAsProperty(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    )
    {
        switch (propertyValue.Value)
        {
            case CXValue.Element when propertyValue.Node is not null:
                return propertyValue.Node.Render(context, options.ToComponentOptions());
            case CXValue.Interpolation interpolation:
            {
                // ensure its a component builder
                var info = context.GetInterpolationInfo(interpolation);

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        info.Symbol,
                        context.Compilation,
                        out var interleavedKind
                    )
                )
                {
                    return FromDiagnostic(
                        Diagnostics.TypeMismatch(
                            $"{context.KnownTypes.IMessageComponentBuilderType} | {context.KnownTypes.MessageComponentType}",
                            info.Symbol!.ToString()
                        ),
                        interpolation
                    );
                }

                var source = context.GetDesignerValue(
                    info,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );

                // ensure we can convert it to a builder
                var target = options.TypingContext?.ConformingType ?? ComponentBuilderKind.IMessageComponentBuilder;

                var result = new Builder();
                if (
                    !ComponentBuilderKindUtils.TryConvert(
                        source,
                        interleavedKind,
                        target,
                        out var converted,
                        spreadCollections: options.TypingContext?.CanSplat is true
                    )
                )
                {
                    source = interleavedKind switch
                    {
                        ComponentBuilderKind.CXMessageComponent => $"{source}.Builders.First()",
                        ComponentBuilderKind.MessageComponent => $"{source}.Components.First().ToBuilder()",
                        ComponentBuilderKind.CollectionOfCXComponents => $"{source}.First().Builders.First()",
                        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.First()",
                        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.First().ToBuilder()",
                        ComponentBuilderKind.CollectionOfMessageComponents =>
                            $"{source}.First().Components.First().ToBuilder",
                        _ => string.Empty
                    };

                    if (source != string.Empty)
                    {
                        result.AddDiagnostic(
                            Diagnostics.CardinalityForcedToRuntime(info.Symbol.ToDisplayString()),
                            interpolation
                        );
                    }
                    else
                    {
                        result.AddDiagnostic(
                            Diagnostics.InvalidChildComponentCardinality(propertyValue.PropertyName),
                            interpolation
                        );
                    }
                }

                return result.WithValue(source);
            }
            default:
            {
                var result = new Builder();

                if (propertyValue.Value is not null)
                {
                    result.AddDiagnostic(
                        Diagnostics.InvalidPropertyValueSyntax("interpolation"),
                        propertyValue.Value
                    );
                }

                return result;
            }
        }
    }

    public static RenderResult UnfurledMediaItem(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    ) => String(context, propertyValue, options)
        .Map(x =>
            $"new {
                context.KnownTypes
                    .UnfurledMediaItemPropertiesType
                    !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            }({x})"
        );

    public static RenderResult Integer(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    )
    {
        switch (propertyValue.Value)
        {
            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);

            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Multipart literal:
                if (!literal.HasInterpolations)
                    return FromText(literal, literal.Tokens.ToString().Trim());

                if (IsLoneInterpolatedLiteral(context, literal, out var info))
                    return FromInterpolation(literal, info);

                return FromValue(
                    $"int.Parse({RenderStringLiteral(literal)})",
                    Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
                    literal
                );
            default: return "default";
        }

        RenderResult FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (info.Constant.Value is int || int.TryParse(info.Constant.Value?.ToString(), out _))
                return info.Constant.Value!.ToString();

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Int32)
                )
            )
            {
                return context.GetDesignerValue(info, "int");
            }

            return FromValue(
                $"int.Parse({context.GetDesignerValue(info)})",
                Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
                owner
            );
        }

        RenderResult FromText(ICXNode owner, string text)
        {
            if (int.TryParse(text, out _)) return text;

            return FromValue(
                $"int.Parse({ToCSharpString(text)})",
                Diagnostics.FallbackToRuntimeValueParsing("int.Parse"),
                owner
            );
        }
    }

    public static RenderResult Boolean(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    )
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

                return FromValue(
                    $"bool.Parse({context.GetDesignerValue(info)})",
                    Diagnostics.FallbackToRuntimeValueParsing("bool.Parse"),
                    stringLiteral
                );

            case null when !propertyValue.RequiresValue:
                return "true";

            default: return "default";
        }

        RenderResult FromInterpolation(ICXNode node, DesignerInterpolationInfo info)
        {
            if (info.Constant.Value is bool b) return b ? "true" : "false";

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_Boolean)
                )
            )
            {
                return context.GetDesignerValue(info, "bool");
            }

            if (info.Constant.Value?.ToString().Trim().ToLowerInvariant() is { } str and ("true" or "false"))
                return str;

            return FromValue(
                $"bool.Parse({context.GetDesignerValue(info)})",
                Diagnostics.FallbackToRuntimeValueParsing("bool.Parse"),
                node
            );
        }

        RenderResult FromText(ICXNode owner, string value)
        {
            if (value is not "true" and not "false")
            {
                return Create(
                    Diagnostics.TypeMismatch("bool", "string"),
                    owner
                );
            }

            return value;
        }
    }

    private static readonly Dictionary<string, string> _colorPresets = [];

    private static bool TryGetColorPreset(
        IComponentContext context,
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

    public static RenderResult Color(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    )
    {
        var colorSymbol = context.KnownTypes.ColorType;
        var qualifiedColor = colorSymbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        switch (propertyValue.Value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value);

            case CXValue.Multipart stringLiteral:

                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                return FromValue(
                    UseLibraryParser(RenderStringLiteral(stringLiteral)),
                    Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"),
                    stringLiteral
                );
            default: return "default";
        }

        RenderResult FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            if (
                info.Symbol is not null &&
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    colorSymbol
                )
            )
            {
                return context.GetDesignerValue(
                    info,
                    qualifiedColor
                );
            }

            uint hexColor;

            if (info.Constant.Value is string str)
            {
                if (TryGetColorPreset(context, str, out var preset))
                    return $"{qualifiedColor}.{preset}";

                if (TryParseHexColor(str, out hexColor))
                    return $"new {qualifiedColor}({hexColor})";
            }
            else if (info.Constant.HasValue && uint.TryParse(info.Constant.Value?.ToString(), out hexColor))
            {
                return $"new {qualifiedColor}({hexColor})";
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.Compilation.GetSpecialType(SpecialType.System_UInt32)
                )
            )
            {
                return $"new {qualifiedColor}({context.GetDesignerValue(info, "uint")})";
            }

            return FromValue(
                UseLibraryParser(context.GetDesignerValue(info)),
                Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"),
                owner
            );
        }

        RenderResult FromText(ICXNode owner, string rawValue)
        {
            if (TryGetColorPreset(context, rawValue, out var presetName))
            {
                return $"{qualifiedColor}.{presetName}";
            }

            // maybe hex?
            var hex = rawValue;

            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            if (
                uint.TryParse(
                    hex,
                    NumberStyles.HexNumber,
                    null,
                    out var hexCode
                )
            ) return $"new {qualifiedColor}({hexCode})";

            return FromValue(
                UseLibraryParser(ToCSharpString(rawValue)),
                Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"),
                owner
            );
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

    public static RenderResult Snowflake(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    ) => Snowflake(context, propertyValue.Value, options);

    public static RenderResult Snowflake(IComponentContext context, CXValue? value, PropertyRenderingOptions options)
    {
        switch (value)
        {
            case CXValue.Interpolation interpolation:
                return FromInterpolation(interpolation, context.GetInterpolationInfo(interpolation));

            case CXValue.Scalar scalar:
                return FromText(scalar, scalar.Value.Trim());

            case CXValue.Multipart stringLiteral:
                if (!stringLiteral.HasInterpolations)
                    return FromText(stringLiteral, stringLiteral.Tokens.ToString().Trim());

                if (IsLoneInterpolatedLiteral(context, stringLiteral, out var info))
                    return FromInterpolation(stringLiteral, info);

                return FromValue(
                    UseParseMethod(RenderStringLiteral(stringLiteral)),
                    Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"),
                    stringLiteral
                );

            default: return "default";
        }

        RenderResult FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
        {
            var targetType = context.Compilation.GetSpecialType(SpecialType.System_UInt64);

            if (info.Constant is { HasValue: true, Value: ulong ul })
                return ul.ToString();

            if (
                info.Symbol is not null &&
                context.Compilation.HasImplicitConversion(info.Symbol, targetType)
            )
            {
                return context.GetDesignerValue(info, "ulong");
            }

            return FromValue(
                UseParseMethod(context.GetDesignerValue(info)),
                Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"),
                owner
            );
        }

        RenderResult FromText(ICXNode owner, string text)
        {
            if (ulong.TryParse(text, out _)) return text;

            return FromValue(
                UseParseMethod(ToCSharpString(text)),
                Diagnostics.FallbackToRuntimeValueParsing("ulong.Parse"),
                owner
            );
        }

        static string UseParseMethod(string input)
            => $"ulong.Parse({input})";
    }

    public static RenderResult String(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    ) => String(context, propertyValue.Value, options);
    
    public static RenderResult String(
        IComponentContext context,
        CXValue? value,
        PropertyRenderingOptions options
    )
    {
        switch (value)
        {
            default: return "string.Empty";

            case CXValue.Interpolation interpolation:
                if (context.GetInterpolationInfo(interpolation).Constant.Value is string constant)
                    return ToCSharpString(constant);

                return context.GetDesignerValue(interpolation);
            case CXValue.Multipart literal: return RenderStringLiteral(literal);
            case CXValue.Scalar scalar:
                return ToCSharpString(scalar.FullValue);
        }
    }

    public static string RenderStringLiteral(CXValue.Multipart literal)
    {
        if (literal.Tokens.Count is 0) return "string.Empty";

        var sb = new StringBuilder();

        var literalParts = literal.Tokens
            .Where(x => x.Kind is CXTokenKind.Text)
            .Select(x => x.Value)
            .ToArray();

        if (literalParts.Length > 0)
        {
            literalParts[0] = literalParts[0].TrimStart();

            literalParts[literalParts.Length - 1] = literalParts[literalParts.Length - 1].TrimEnd();
        }

        var quoteCount = literalParts.Length is 0
            ? 1
            : literalParts.Select(x => x.Count(x => x is '"')).Max() + 1;

        var hasInterpolations = literal.Tokens.Any(x => x.Kind is CXTokenKind.Interpolation);

        var dollars = hasInterpolations
            ? new string(
                '$',
                literalParts.Length is 0
                    ? 1
                    : Math.Max(1, literalParts.Select(GetInterpolationDollarRequirement).Max())
            )
            : string.Empty;

        var startInterpolation = dollars.Length > 0
            ? new string('{', dollars.Length)
            : string.Empty;

        var endInterpolation = dollars.Length > 0
            ? new string('}', dollars.Length)
            : string.Empty;

        var isMultiline = false;

        for (var i = 0; i < literal.Tokens.Count; i++)
        {
            var token = literal.Tokens[i];

            // first and last token allow one newline before/after as syntax trivia
            var leadingTrivia = token.LeadingTrivia;
            var trailingTrivia = token.TrailingTrivia;

            for (var j = 0; j < leadingTrivia.Count; j++)
            {
                var trivia = leadingTrivia[j];
                if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                if (i != 0) continue;

                // remove all trivia leading up to this newline
                leadingTrivia = leadingTrivia.RemoveRange(0, j + 1);
                break;
            }

            for (var j = trailingTrivia.Count - 1; j >= 0; j--)
            {
                var trivia = trailingTrivia[j];
                if (trivia is not CXTrivia.Token { Kind: CXTriviaTokenKind.Newline }) continue;

                if (i != literal.Tokens.Count - 1) continue;

                // remove all trivia after the newline
                trailingTrivia = trailingTrivia.RemoveRange(j, trailingTrivia.Count - j);
                break;
            }

            isMultiline |=
            (
                trailingTrivia.ContainsNewlines ||
                leadingTrivia.ContainsNewlines ||
                token.Value.Contains("\n")
            );

            switch (token.Kind)
            {
                case CXTokenKind.Text:
                    sb
                        .Append(leadingTrivia)
                        .Append(EscapeBackslashes(token.Value))
                        .Append(trailingTrivia);
                    break;
                case CXTokenKind.Interpolation:
                    var index = literal.Document!.InterpolationTokens.IndexOf(token);

                    // TODO: handle better
                    if (index is -1) throw new InvalidOperationException();

                    sb
                        .Append(leadingTrivia)
                        .Append(startInterpolation)
                        .Append($"designer.GetValueAsString({index})")
                        .Append(endInterpolation)
                        .Append(trailingTrivia);
                    break;

                default: continue;
            }
        }

        // normalize the value indentation
        var value = sb.ToString().NormalizeIndentation().Trim(['\r', '\n']);

        // pad the value to the amount of dollar signs we have to properly align the value text to the 
        // multi-line string literal
        if (hasInterpolations && isMultiline)
            value = value.Indent(dollars.Length);

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

        // ending quotes are on a different line 
        if (isMultiline) sb.AppendLine();

        // if it has interpolations, offset the ending quotes by the amount of dollar signs
        if (hasInterpolations && isMultiline) sb.Append("".PadLeft(dollars.Length));
        sb.Append(quotes);

        return sb.ToString();
    }

    public static int GetInterpolationDollarRequirement(string part)
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

        if (isMultiline) sb.AppendLine();

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

    public static int GetSequentialQuoteCount(string text)
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

        return (context, propertyValue, options) =>
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

            RenderResult FromInterpolation(ICXNode owner, DesignerInterpolationInfo info)
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

            RenderResult FromText(string text, ICXNode owner)
            {
                if (variants.TryGetValue(text, out var name))
                    return $"{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{name}";

                return FromValue(
                    $"global::System.Enum.Parse<{symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({ToCSharpString(text)})",
                    Diagnostics.InvalidEnumVariant(text, symbol.ToDisplayString()),
                    owner
                );
            }
        };
    }

    public static RenderResult Emoji(
        IComponentContext context,
        IComponentPropertyValue propertyValue,
        PropertyRenderingOptions options
    )
    {
        var isDiscordEmote = false;
        var isEmoji = false;

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
                    context,
                    context.GetDesignerValue(interpolation)
                );

            case CXValue.Scalar scalar:
            {
                var builder = new Builder();
                LightlyValidateEmote(ref builder, scalar.Value, scalar, out isDiscordEmote, out isEmoji);
                return builder.WithValue(
                    UseLibraryParser(context, ToCSharpString(scalar.Value), isEmoji, isDiscordEmote)
                );
            }

            case CXValue.Multipart stringLiteral:
            {
                var builder = new Builder();
                if (!stringLiteral.HasInterpolations)
                    LightlyValidateEmote(
                        ref builder,
                        stringLiteral.Tokens.ToString(),
                        stringLiteral.Tokens,
                        out isDiscordEmote,
                        out isEmoji
                    );

                return builder.WithValue(
                    UseLibraryParser(context, RenderStringLiteral(stringLiteral), isEmoji, isDiscordEmote)
                );
            }

            default: return "null";
        }

        void LightlyValidateEmote(
            ref Builder builder,
            string emote,
            ICXNode node,
            out bool isDiscordEmote,
            out bool isEmoji
        )
        {
            isDiscordEmote = IsDiscordEmote.IsMatch(emote);
            isEmoji = IsEmoji.IsMatch(emote);

            if (!isDiscordEmote && !isEmoji)
            {
                builder.AddDiagnostic(
                    Diagnostics.PossibleInvalidEmote(emote),
                    node
                );
            }
        }

        static string UseLibraryParser(
            IComponentContext context,
            string source,
            bool? isEmoji = null,
            bool? isDiscordEmote = null
        )
        {
            if (isEmoji is true)
                return $"global::Discord.Emoji.Parse({source})";

            if (isDiscordEmote is true)
                return $"global::Discord.Emote.Parse({source})";

            var varName = context.GetVariableName("emoji");

            return
                $"""
                 global::Discord.Emoji.TryParse({source}, out var {varName})
                     ? (global::Discord.IEmote){varName}
                     : global::Discord.Emote.Parse({source})
                 """;
        }
    }

    private static readonly Regex IsEmoji = new Regex(@"^(?>(?>[\uD800-\uDBFF][\uDC00-\uDFFF]\p{M}*){1,5}|\p{So})$",
        RegexOptions.Compiled);

    private static readonly Regex IsDiscordEmote = new Regex(@"^<(?>a|):.+:\d+>$", RegexOptions.Compiled);
}