using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser;

namespace Discord.CX;

public static class CXUtils
{
    public static bool TryGetConstantValue(
        this CXValue cx,
        IComponentContext context,
        [MaybeNullWhen(false)] out string value
    )
    {
        switch (cx)
        {
            case CXValue.Scalar scalar:
                value = scalar.Value;
                return true;
            case CXValue.Interpolation interpolation when TryGetInterpolationValue(interpolation.InterpolationIndex, out value):
                return true;

            case CXValue.Multipart multipart:
                if (!multipart.HasInterpolations)
                {
                    value = multipart.Tokens.ToString();
                    return true;
                }

                var sb = new StringBuilder();

                foreach (var token in multipart.Tokens)
                {
                    if (token.InterpolationIndex is { } index)
                    {
                        if (!TryGetInterpolationValue(index, out var interpValue))
                        {
                            value = null;
                            return false;
                        }

                        sb.Append(interpValue);
                    }
                    else sb.Append(token.Value);
                }

                value = sb.ToString();
                return true;
            default:
                value = string.Empty;
                return false;
        }

        bool TryGetInterpolationValue(int index, [MaybeNullWhen(false)] out string value)
        {
            var info = context.GetInterpolationInfo(index);

            if (info.Constant.HasValue)
            {
                value = info.Constant.Value?.ToString() ?? string.Empty;
                return true;
            }

            value = null;
            return false;
        }
    }
    
    public static bool IsLoneInterpolatedLiteral(
        this CXValue.Multipart literal,
        IComponentContext context,
        out DesignerInterpolationInfo info)
    {
        if (
            literal is { HasInterpolations: true, Tokens.Count: 1 } &&
            literal.Document!.TryGetInterpolationIndex(literal.Tokens[0], out var index)
        )
        {
            info = context.GetInterpolationInfo(index);
            return true;
        }

        info = null!;
        return false;
    }
}