using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Parser;

public readonly record struct Cursor(
    int NodeIndex,
    int ChangeDelta,
    int NewPosition,
    ImmutableStack<TextChangeRange> Changes
)
{
    public Cursor Finish()
        => this with { NodeIndex = -1 };

    // public static readonly Cursor Invalid = new(
    //     -1,
    //     -1,
    //     -1,
    //     ImmutableStack<TextChangeRange>.Empty
    // );
}