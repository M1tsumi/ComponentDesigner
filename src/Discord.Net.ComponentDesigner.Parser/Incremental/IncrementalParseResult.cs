using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Discord.CX.Parser;

/// <summary>
///     Represents the result of incrementally parsing.
/// </summary>
/// <param name="ReusedNodes">A read-only list of AST nodes that were reused.</param>
/// <param name="NewNodes">A read-only list of AST nodes that were parsed.</param>
/// <param name="Changes">A read-only list of changes.</param>
/// <param name="AppliedRange">A range describing the changes.</param>
public readonly record struct IncrementalParseResult(
    IReadOnlyList<ICXNode> ReusedNodes,
    IReadOnlyList<ICXNode> NewNodes,
    IReadOnlyList<TextChange> Changes,
    TextChangeRange AppliedRange
)
{
    public static readonly IncrementalParseResult Empty = new([], [], [], default);
}
