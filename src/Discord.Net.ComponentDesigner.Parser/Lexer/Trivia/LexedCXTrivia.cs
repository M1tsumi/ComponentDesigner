using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Util;

namespace Discord.CX.Parser;

[Flags]
public enum LexedTriviaFlags
{
    None = 0,
    HasNewlines = 1 << 0,
    HasComments = 1 << 1,
    HasWhitespace = 1 << 2
}

public sealed class LexedCXTrivia(
    IReadOnlyList<CXTrivia> trivia,
    string value,
    LexedTriviaFlags flags = LexedTriviaFlags.None
) : IReadOnlyList<CXTrivia>, IEquatable<LexedCXTrivia>
{
    public bool ContainsWhitespace => flags.HasFlag(LexedTriviaFlags.HasWhitespace);
    public bool ContainsNewlines => flags.HasFlag(LexedTriviaFlags.HasNewlines);
    public bool ContainsComments => flags.HasFlag(LexedTriviaFlags.HasComments);

    public int Length { get; } = trivia.Count is 0 ? 0 : trivia.Sum(x => x.Length);

    public static readonly LexedCXTrivia Empty = new([], string.Empty);

    public IEnumerator<CXTrivia> GetEnumerator() => trivia.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)trivia).GetEnumerator();

    public int Count => trivia.Count;

    public CXTrivia this[int index] => trivia[index];

    public bool Equals(LexedCXTrivia? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return
            Length == other.Length &&
            value == other.ToString() &&
            this.SequenceEqual(other);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is LexedCXTrivia other && Equals(other);

    public override int GetHashCode()
        => trivia.Aggregate(
            Hash.Combine(Length, value),
            Hash.Combine
        );

    public override string ToString() => value;
}