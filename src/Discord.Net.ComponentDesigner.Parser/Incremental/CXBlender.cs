using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Discord.CX.Parser;

/// <summary>
///     A blender used to either reuse or parse AST nodes when incrementally parsing.
/// </summary>
public sealed class CXBlender
{
    /// <summary>
    ///     A data type relating source positions to indexes with the AST tree, as well as source change information.
    /// </summary>
    /// <param name="NewPosition">The new position of the AST node.</param>
    /// <param name="ChangeDelta">The character delta of the current change.</param>
    /// <param name="Index">The index of the AST node this cursor points to.</param>
    /// <param name="Changes">The remaining changes ahead of this cursor.</param>
    public readonly record struct Cursor(
        int NewPosition,
        int ChangeDelta,
        int Index,
        ImmutableStack<TextChangeRange> Changes
    )
    {
        /// <summary>
        ///     Gets whether this cursor represents an invalid state.
        /// </summary>
        public bool IsInvalid => Index is -1;

        /// <summary>
        ///     A read-only representation of an invalid cursor.
        /// </summary>
        public static readonly Cursor Invalid = new(
            -1,
            -1,
            -1,
            ImmutableStack<TextChangeRange>.Empty
        );

        /// <summary>
        ///     Invalidates the current cursor.
        /// </summary>
        public Cursor Invalidate() => this with {Index = -1};

        /// <summary>
        ///     Creates a new cursor given a node that has changed.
        /// </summary>
        /// <param name="node">The node that has changed.</param>
        public Cursor WithChangedNode(ICXNode node)
            => new(
                NewPosition: NewPosition + node.FullSpan.Length,
                ChangeDelta: ChangeDelta - node.FullSpan.Length,
                Index: Index + node.GraphWidth,
                Changes: Changes
            );

        /// <summary>
        ///     Creates a <see cref="BlendedNode"/> given a changed AST node.
        /// </summary>
        /// <param name="node">The node that was changed.</param>
        /// <returns>The newly created blended node.</returns>
        public BlendedNode BlendChangedNode(ICXNode node)
            => new(
                node,
                WithChangedNode(node)
            );
    }

    /// <summary>
    ///     The starting cursor of this blender.
    /// </summary>
    public readonly Cursor StartingCursor;

    /// <summary>
    ///     Gets the <see cref="ICXNode"/> the given cursor represents.
    /// </summary>
    /// <param name="cursor">The cursor representing the AST node to get.</param>
    private ICXNode? this[in Cursor cursor] =>
        cursor.Index >= 0 && cursor.Index < _graph.Count ? _graph[cursor.Index] : null;

    /// <summary>
    ///     Gets the cancellation token used to cancel the blending of nodes.
    /// </summary>
    public CancellationToken CancellationToken => _lexer.CancellationToken;

    /// <summary>
    ///     The lexer used by the blender. 
    /// </summary>
    private readonly CXLexer _lexer;

    /// <summary>
    ///     The flat AST this blender uses to blend from.
    /// </summary>
    private readonly IReadOnlyList<ICXNode> _graph;

    /// <summary>
    ///     Constructs a new <see cref="CXBlender"/>.
    /// </summary>
    /// <param name="lexer">The lexer the blender will lex from.</param>
    /// <param name="document">The old <see cref="CXDocument"/>.</param>
    /// <param name="changeRange">The changes to blend from.</param>
    public CXBlender(
        CXLexer lexer,
        CXDocument document,
        TextChangeRange changeRange
    )
    {
        _lexer = lexer;

        _graph = document.GetFlatGraph();

        StartingCursor = new(
            document.FullSpan.Start,
            0,
            0,
            ImmutableStack<TextChangeRange>
                .Empty
                .Push(changeRange)
        );
    }

    /// <summary>
    ///     Moves the given cursor to the first terminal token.
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void MoveToFirstToken(ref Cursor cursor)
    {
        if (cursor.Index >= _graph.Count) return;

        var index = cursor.Index;

        while (index < _graph.Count && _graph[index] is not CXToken)
        {
            index++;
            CancellationToken.ThrowIfCancellationRequested();
        }

        cursor = cursor with {Index = index};
    }

    /// <summary>
    ///     Moves the given cursor to the next sibling AST node.
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void MoveToNextSibling(ref Cursor cursor)
    {
        while (this[cursor]?.Parent is not null)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var tempCursor = cursor;

            FindNextNonZeroWidthOrIsEOFSibling(ref cursor);

            if (cursor.IsInvalid)
            {
                MoveToParent(ref tempCursor);
                cursor = tempCursor;
            }
            else return;
        }

        cursor = cursor.Invalidate();
    }

    /// <summary>
    ///     Moves the given cursor to the parent AST node.
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void MoveToParent(ref Cursor cursor)
    {
        var current = this[cursor];

        if (current?.Parent is null) return;

        var index = current.Parent.GetIndexOfSlot(current);

        if (index is -1) return;

        var delta = 1;

        for (var i = index - 1; i >= 0; i--)
        {
            delta += current.Parent.Slots[i].GraphWidth + 1;
        }

        cursor = cursor with {Index = cursor.Index - delta};
    }

    /// <summary>
    ///     Moves the given cursor to the next non-zero width AST node.  
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void FindNextNonZeroWidthOrIsEOFSibling(ref Cursor cursor)
    {
        var current = this[cursor];

        if (current?.Parent is { } parent)
        {
            var index = parent.GetIndexOfSlot(current);

            for (
                int slotIndex = index + 1,
                cursorIndex = cursor.Index + current.GraphWidth + 1;
                slotIndex < parent.Slots.Count;
                cursorIndex += parent.Slots[slotIndex++].GraphWidth + 1
            )
            {
                CancellationToken.ThrowIfCancellationRequested();

                var sibling = parent.Slots[slotIndex];

                if (IsNonZeroWidthOrIsEOF(sibling))
                {
                    cursor = cursor with {Index = cursorIndex};
                    return;
                }
            }
        }

        cursor = cursor.Invalidate();
    }

    /// <summary>
    ///     Moves the given cursor to the first child AST node.
    /// </summary>
    /// <param name="cursor">The cursor to move.</param>
    private void MoveToFirstChild(ref Cursor cursor)
    {
        var current = this[cursor];

        if (current is null || current.Slots.Count is 0)
        {
            cursor = cursor.Invalidate();
            return;
        }

        for (
            int childIndex = 0, childGraphIndex = cursor.Index + 1;
            childIndex < current.Slots.Count;
            childGraphIndex += current.Slots[childIndex++].GraphWidth + 1
        )
        {
            CancellationToken.ThrowIfCancellationRequested();

            var child = current.Slots[childIndex];
            if (IsNonZeroWidthOrIsEOF(child))
            {
                cursor = cursor with {Index = childGraphIndex};
                return;
            }
        }

        cursor = cursor.Invalidate();
    }

    /// <summary>
    ///     Check if the provided AST node is an EOF token or has a width of zero.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>
    ///     <see langword="true"/> if the node is zero-width OR the node is an EOF token; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    private static bool IsNonZeroWidthOrIsEOF(ICXNode node)
        => !node.FullSpan.IsEmpty || node is CXToken {Kind: CXTokenKind.EOF};

    /// <summary>
    ///     Checks if the given cursor points to the end of the AST tree.
    /// </summary>
    /// <param name="cursor"></param>
    /// <returns></returns>
    private bool IsCompletedCursor(in Cursor cursor)
        => this[cursor] is null or CXToken {Kind: CXTokenKind.EOF or CXTokenKind.Invalid};

    /// <summary>
    ///     Blends the next terminal token.
    /// </summary>
    /// <param name="cursor">The cursor to blend from.</param>
    /// <returns>The blended token.</returns>
    public BlendedNode NextToken(Cursor cursor) => Next(asToken: true, cursor);
    
    /// <summary>
    ///     Blends the next AST node.
    /// </summary>
    /// <param name="cursor">The cursor to blend from.</param>
    /// <returns>The blended node.</returns>
    public BlendedNode NextNode(Cursor cursor) => Next(asToken: false, cursor);

    /// <summary>
    ///     Blends the next AST node.
    /// </summary>
    /// <param name="asToken">Whether to blend as a token.</param>
    /// <param name="cursor">The cursor to blend from.</param>
    /// <returns>The blended AST node.</returns>
    public BlendedNode Next(bool asToken, Cursor cursor)
    {
        while (true)
        {
            CancellationToken.ThrowIfCancellationRequested();

            // does the cursor represent the completed state?
            if (IsCompletedCursor(cursor))
            {
                // read a fresh token from the lexer, in case that there are new tokens, we have nothing to blend.
                // if we are at the EOF, the lexer will return an EOF token.
                return ReadNewToken(cursor);
            }

            // we've read some new nodes past the current cursor, skip past the old nodes
            if (cursor.ChangeDelta < 0)
            {
                SkipOldToken(ref cursor);
            }
            // we've got some changes to the text, read out the new tokens
            else if (cursor.ChangeDelta > 0)
            {
                return ReadNewToken(cursor);
            }
            else
            {
                // try to reuse a node
                if (TryTakeOldNodeOrToken(asToken, cursor, out var node)) return node;

                // update the cursor and move to the next node
                if (this[cursor] is CXNode)
                    MoveToFirstChild(ref cursor);
                else
                    SkipOldToken(ref cursor);
            }
        }
    }

    /// <summary>
    ///     Skips a single token, assuming its outdated.
    /// </summary>
    /// <param name="cursor">The cursor pointing to the outdated token.</param>
    private void SkipOldToken(ref Cursor cursor)
    {
        MoveToFirstToken(ref cursor);

        var current = this[cursor];

        if (current is null) return;

        cursor = cursor with {ChangeDelta = cursor.ChangeDelta + current.FullSpan.Length};

        MoveToNextSibling(ref cursor);

        SkipPastChanges(ref cursor);
    }

    /// <summary>
    ///     Skips past changes in the cursor.
    /// </summary>
    /// <param name="cursor">The cursor to skip changes.</param>
    private void SkipPastChanges(ref Cursor cursor)
    {
        if (this[cursor] is not { } current) return;

        while (
            !cursor.Changes.IsEmpty &&
            current.FullSpan.Start >= cursor.Changes.Peek().Span.End
        )
        {
            var change = cursor.Changes.Peek();
            cursor = cursor with
            {
                ChangeDelta = cursor.ChangeDelta + (change.NewLength - change.Span.Length),
                Changes = cursor.Changes.Pop()
            };
        }
    }

    /// <summary>
    ///     Tries to reuse an old node or token.
    /// </summary>
    /// <param name="asToken">Whether to only return tokens.</param>
    /// <param name="cursor">The cursor to reuse.</param>
    /// <param name="blendedNode">The blended node that was reused.</param>
    /// <returns>
    ///     <see langword="true"/> if an AST node could be reused; otherwise <see langword="false"/>.
    /// </returns>
    private bool TryTakeOldNodeOrToken(
        bool asToken,
        Cursor cursor,
        out BlendedNode blendedNode)
    {
        if (asToken) MoveToFirstToken(ref cursor);

        var current = this[cursor];

        if (!CanReuse(current, cursor) || current is null)
        {
            blendedNode = default;
            return false;
        }

        MoveToNextSibling(ref cursor);
        
        blendedNode = new(
            current,
            cursor with {NewPosition = cursor.NewPosition + current.FullSpan.Length,}
        );
        return true;
    }

    /// <summary>
    ///     Determines whether a given AST node can safely be reused.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="cursor">The cursor pointing to the node.</param>
    /// <returns>
    ///     <see langword="true"/> if the given node can be reused; otherwise <see langword="false"/>.
    /// </returns>
    private static bool CanReuse(ICXNode? node, Cursor cursor)
    {
        if (node is null) return false;

        if (node.FullSpan.IsEmpty) return false;

        if (IntersectsChange(node, cursor)) return false;

        if (node.HasErrors) return false;

        return true;
    }

    /// <summary>
    ///     Determines if the provided node and cursor intersects a change.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="cursor">The cursor containing the source and change information.</param>
    /// <returns>
    ///     <see langword="true"/> if the given node and cursor intersect a change; otherwise <see langword="false"/>.
    /// </returns>
    private static bool IntersectsChange(ICXNode node, Cursor cursor)
    {
        if (cursor.Changes.IsEmpty) return false;

        // for collections, we assume anything after *could* be another element to
        // the collection. A simple way to force that is to up the nodes span by 1
        // before checking the changes
        var span = node is ICXCollection
            ? new TextSpan(node.FullSpan.Start, node.FullSpan.Length + 1)
            : node.FullSpan;

        return span.IntersectsWith(cursor.Changes.Peek().Span);
    }

    /// <summary>
    ///     Lexes a new token and constructs a <see cref="BlendedNode"/>.
    /// </summary>
    /// <param name="cursor">The cursor pointing to the source location to lex.</param>
    /// <returns>
    ///     The lexed token.
    /// </returns>
    private BlendedNode ReadNewToken(Cursor cursor)
    {
        var token = LexNewToken(cursor);

        cursor = cursor with
        {
            NewPosition = cursor.NewPosition + token.FullSpan.Length,
            ChangeDelta = cursor.ChangeDelta - token.FullSpan.Length,
        };

        SkipPastChanges(ref cursor);

        return new(
            token,
            cursor
        );
    }

    /// <summary>
    ///     Lexes a new token.
    /// </summary>
    /// <param name="cursor">The cursor pointing to the source location to lex.</param>
    /// <returns>The lexed token.</returns>
    private CXToken LexNewToken(Cursor cursor)
    {
        _lexer.Seek(cursor.NewPosition);
        return _lexer.Next();
    }
}
