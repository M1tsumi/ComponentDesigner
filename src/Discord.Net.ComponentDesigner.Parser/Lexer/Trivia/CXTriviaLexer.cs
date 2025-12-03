using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using static Discord.CX.Parser.CXLexer;

namespace Discord.CX.Parser;

public enum CXTriviaTokenKind
{
    Whitespace,
    Newline,
    CommentStart,
    CommentEnd,
    Comment
}

public sealed class LexedCXTrivia(IReadOnlyList<CXTrivia> trivia) : IReadOnlyList<CXTrivia>
{
    public static readonly LexedCXTrivia Empty = new([]);

    public IEnumerator<CXTrivia> GetEnumerator() => trivia.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)trivia).GetEnumerator();

    public int Count => trivia.Count;

    public CXTrivia this[int index] => trivia[index];
}

public abstract record CXTrivia(TextSpan Span)
{
    public int Length => Span.Length;

    public bool IsWhitespaceTrivia
        => this is Token { Kind: CXTriviaTokenKind.Newline or CXTriviaTokenKind.Whitespace };

    public abstract override string ToString();

    public sealed record Token(
        CXTriviaTokenKind Kind,
        string Value,
        TextSpan Span
    ) : CXTrivia(Span)
    {
        public override string ToString() => Value;
    }

    public sealed record XmlComment(
        Token Start,
        Token Value,
        Token? End
    ) : CXTrivia(TextSpan.FromBounds(Start.Span.Start, End?.Span.End ?? Value.Span.End))
    {
        public override string ToString()
            => $"{Start}{Value}{End}";
    }
}

public class CXTriviaLexer
{
    public interface ITriviaBucket
    {
        ICXNode Node { get; }

        TextSpan LeadingTriviaSpan { get; }

        TextSpan TrailingTriviaSpan { get; }

        LexedCXTrivia LexedLeadingTrivia { get; }
        LexedCXTrivia LexedTrailingTrivia { get; }
    }

    private sealed class TokenTriviaBucket(CXToken token) : ITriviaBucket
    {
        public TextSpan LeadingTriviaSpan => GetLeadingTriviaSpan(token);
        public TextSpan TrailingTriviaSpan => GetTrailingTriviaSpan(token);

        public LexedCXTrivia LexedLeadingTrivia
            => _leading ??= new([..Lex(LeadingTriviaSpan, token.LeadingTrivia, false)]);

        public LexedCXTrivia LexedTrailingTrivia
            => _trailing ??= new([..Lex(TrailingTriviaSpan, token.TrailingTrivia, true)]);

        private LexedCXTrivia? _leading;
        private LexedCXTrivia? _trailing;

        ICXNode ITriviaBucket.Node => token;
    }

    private sealed class NodeTriviaBucket(CXNode node) : ITriviaBucket
    {
        public TextSpan LeadingTriviaSpan => GetLeadingTriviaSpan(node);
        public TextSpan TrailingTriviaSpan => GetTrailingTriviaSpan(node);

        public LexedCXTrivia LexedLeadingTrivia
            => node.FirstTerminal is null ? LexedCXTrivia.Empty : GetLeadingTrivia(node.FirstTerminal);

        public LexedCXTrivia LexedTrailingTrivia
            => node.LastTerminal is null ? LexedCXTrivia.Empty : GetTrailingTrivia(node.LastTerminal);

        ICXNode ITriviaBucket.Node => node;
    }

    private static readonly ConditionalWeakTable<ICXNode, ITriviaBucket> _buckets = new();

    public static LexedCXTrivia GetLeadingTrivia(ICXNode node)
        => GetOrCreateBucket(node).LexedLeadingTrivia;

    public static LexedCXTrivia GetTrailingTrivia(ICXNode node)
        => GetOrCreateBucket(node).LexedTrailingTrivia;

    private static ITriviaBucket GetOrCreateBucket(ICXNode node)
    {
        if (!_buckets.TryGetValue(node, out var bucket))
            _buckets.Add(node, bucket = CreateBucket(node));

        return bucket;
    }

    private static ITriviaBucket CreateBucket(ICXNode node)
        => node switch
        {
            CXToken token => new TokenTriviaBucket(token),
            CXNode nd => new NodeTriviaBucket(nd),
            _ => throw new InvalidOperationException($"{node.GetType()} is not a valid trivia node")
        };

    private static TextSpan GetLeadingTriviaSpan(ICXNode node)
        => new(node.FullSpan.Start, node.LeadingTrivia?.Length ?? 0);

    private static TextSpan GetTrailingTriviaSpan(ICXNode node)
        => new(node.FullSpan.End - (node.TrailingTrivia?.Length ?? 0), node.TrailingTrivia?.Length ?? 0);

    private static IEnumerable<CXTrivia> Lex(TextSpan span, string trivia, bool isTrailing)
    {
        var pos = 0;

        while (pos < trivia.Length)
        {
            if (!LexSingle(ref pos, out var token))
            {
                if (token is not null)
                {
                    yield return token;
                }

                yield break;
            }

            if (token is not null)
            {
                yield return token;
            }
        }

        bool LexSingle(ref int pos, out CXTrivia? result)
        {
            var current = trivia[pos];
            var next = pos + 1 == trivia.Length ? NULL_CHAR : trivia[pos + 1];

            if (current is CARRIAGE_RETURN_CHAR && next is NEWLINE_CHAR)
            {
                result = new CXTrivia.Token(
                    CXTriviaTokenKind.Newline,
                    trivia.Substring(pos, 2),
                    new(span.Start + pos, 2)
                );

                pos += 2;
                return true;
            }

            if (current is NEWLINE_CHAR)
            {
                result = new CXTrivia.Token(
                    CXTriviaTokenKind.Newline,
                    trivia.Substring(pos, 1),
                    new(span.Start + pos, 1)
                );

                pos++;

                return !isTrailing;
            }

            if (IsWhitespace(current))
            {
                var sz = pos + 1;

                for (;
                     sz < trivia.Length &&
                     IsWhitespace(trivia[sz]) &&
                     trivia[sz] is not NEWLINE_CHAR and not CARRIAGE_RETURN_CHAR;
                     sz++
                    ) ;

                var length = sz - pos;

                result = new CXTrivia.Token(
                    CXTriviaTokenKind.Whitespace,
                    trivia.Substring(pos, length),
                    new(span.Start + pos, length)
                );

                pos += length;

                return true;
            }

            if (
                current is LESS_THAN_CHAR &&
                (trivia.Length - pos) >= COMMENT_START.Length &&
                IsCommentStart(trivia.Substring(pos, COMMENT_START.Length))
            )
            {
                var start = new CXTrivia.Token(
                    CXTriviaTokenKind.CommentStart,
                    trivia.Substring(pos, COMMENT_START.Length),
                    new(span.Start + pos, COMMENT_START.Length)
                );

                pos += COMMENT_START.Length;
                var valueStartPos = pos;


                for (;
                     pos <= (trivia.Length - COMMENT_END.Length) &&
                     !IsCommentEnd(trivia.Substring(pos, COMMENT_END.Length));
                     pos++
                    ) ;

                var value = new CXTrivia.Token(
                    CXTriviaTokenKind.Comment,
                    trivia.Substring(valueStartPos, pos - valueStartPos),
                    new(span.Start + valueStartPos, pos - valueStartPos)
                );

                // is there a comment end?
                var endText = trivia.Substring(pos, COMMENT_END.Length);
                CXTrivia.Token? end = null;

                if (
                    pos <= (trivia.Length - COMMENT_END.Length) &&
                    IsCommentEnd(endText)
                )
                {
                    end = new CXTrivia.Token(
                        CXTriviaTokenKind.CommentEnd,
                        endText,
                        new(span.Start + pos, COMMENT_END.Length)
                    );

                    pos += COMMENT_END.Length;
                }

                result = new CXTrivia.XmlComment(
                    start,
                    value,
                    end
                );

                return end is not null;
            }

            result = null!;
            return false;
        }
    }
}