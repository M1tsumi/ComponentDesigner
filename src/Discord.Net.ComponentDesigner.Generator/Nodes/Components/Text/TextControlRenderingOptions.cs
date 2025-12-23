using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes.Components;

public readonly record struct TextControlRenderingOptions(
    string StartInterpolation,
    string EndInterpolation,
    bool AsCSharpString
)
{
    public static readonly TextControlRenderingOptions Default = new(string.Empty, string.Empty, false);
}