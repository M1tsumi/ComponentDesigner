namespace Discord.CX.Parser;

/// <summary>
///     Represents a blended AST node.
/// </summary>
/// <param name="Value">The underlying AST node.</param>
/// <param name="Cursor">The cursor of this blended node.</param>
public readonly record struct BlendedNode(
    ICXNode Value,
    CXBlender.Cursor Cursor
);
