using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Text;
using Discord.CX.Util;

namespace Discord.CX.Parser;

using RelativeIndex = int;
using NormalizedIndex = int;
using RelativeTextSpan = TextSpan;
using NormalizedTextSpan = TextSpan;

/// <summary>
///     Represents a class used to read from a <see cref="CXSourceText"/> and understand additional language features like
///     interpolations and source location offsetting. 
/// </summary>
public sealed class CXSourceReader
{
    /// <summary>
    ///     Gets a single <see cref="char"/> at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">
    ///     The relative index of the <see cref="char"/> to get.
    /// </param>
    public char this[RelativeIndex index]
        => SourceSpan.Contains(index)
            ? Source[Normalize(index)]
            : CXLexer.NULL_CHAR;

    /// <summary>
    ///     Gets a string at the specified <paramref name="span"/>.
    /// </summary>
    /// <param name="span">
    ///     A relative <see cref="TextSpan"/> representing the region of the source to get.
    /// </param>
    public string this[RelativeTextSpan span]
        => Source[Normalize(span)];

    /// <summary>
    ///     Gets a string at the specified <paramref name="index"/> and <paramref name="length"/>
    /// </summary>
    /// <param name="index">
    ///     The relative index on which the return string should start.
    /// </param>
    /// <param name="length">
    ///     The number of characters to read from the source.
    /// </param>
    public string this[RelativeIndex index, int length]
        => this[new TextSpan(index, length)];

    /// <summary>
    ///     Gets whether the reader is at the end of the source.
    /// </summary>
    public bool IsEOF => Position >= SourceSpan.End;

    /// <summary>
    ///     Gets the current character the reader is at.
    /// </summary>
    /// <remarks>
    ///     If the reader is at the end of the source, the <see cref="CXLexer.NULL_CHAR"/> is returned instead.
    /// </remarks>
    public char Current => this[Position];

    /// <summary>
    ///     Gets the character ahead of the <see cref="Current"/> character.
    /// </summary>
    /// <remarks>
    ///     If the next character is at the end of the source, the <see cref="CXLexer.NULL_CHAR"/> is returned instead.
    /// </remarks>
    public char Next => this[Position + 1];

    /// <summary>
    ///     Gets the character behind the <see cref="Current"/> character.
    /// </summary>
    /// <remarks>
    ///     If the previous character is at the end of the source, or before the start of the source, the
    ///     <see cref="CXLexer.NULL_CHAR"/> is returned instead.
    /// </remarks>
    public char Previous => this[Position - 1];

    /// <summary>
    ///     Gets whether the current <see cref="Position"/> is within an interpolation.
    /// </summary>
    public bool IsInInterpolation => IsAtInterpolation(Position);

    /// <summary>
    ///     Gets the current position of the reader.
    /// </summary>
    public NormalizedIndex Position { get; set; }
    
    /// <summary>
    ///     Gets the underlying <see cref="CXSourceText"/> that this reader is reading from.
    /// </summary>
    public CXSourceText Source { get; }

    /// <summary>
    ///     Gets the span that encapsulates the <see cref="Source"/> that this reader is reading from.
    /// </summary>
    /// <remarks>
    ///     All positions are normalized to be within this span.
    /// </remarks>
    public TextSpan SourceSpan { get; }

    /// <summary>
    ///     Gets a read-only array of <see cref="TextSpan"/>s representing the interpolations found within the
    ///     <see cref="Source"/>.
    /// </summary>
    public ImmutableArray<TextSpan> Interpolations { get; }

    /// <summary>
    ///     Gets the number of quotes that wrap the <see cref="Source"/>.
    /// </summary>
    /// <remarks>
    ///     This is explicitly defined by the C# code that contains the CX
    /// </remarks>
    public int WrappingQuoteCount { get; }

    /// <summary>
    ///     Gets the <see cref="StringInternTable"/> used by this reader for interning strings.
    /// </summary>
    public StringInternTable StringTable { get; }

    /// <summary>
    ///     Constructs a new <see cref="CXSourceReader"/>.
    /// </summary>
    /// <param name="source">The source the reader should read from.</param>
    /// <param name="sourceSpan">
    ///     The span used for normalizing positions to the source.
    /// </param>
    /// <param name="interpolations">
    ///     The interpolations within the source.
    /// </param>
    /// <param name="wrappingQuoteCount">
    ///     The number of quotes wrapping the source.
    /// </param>
    public CXSourceReader(
        CXSourceText source,
        TextSpan sourceSpan,
        TextSpan[] interpolations,
        int wrappingQuoteCount
    )
    {
        Source = source;
        Position = sourceSpan.Start;
        SourceSpan = sourceSpan;
        Interpolations = [..interpolations];
        WrappingQuoteCount = wrappingQuoteCount;

        StringTable = new();
    }

    /// <summary>
    ///     Determines whether the provided index is 
    /// </summary>
    /// <param name="index">The normalized index to check against.</param>
    /// <returns>
    ///     <see langword="true"/> if the provided <paramref name="index"/> falls within an interpolation; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool IsAtInterpolation(NormalizedIndex index)
    {
        // nit: a binary search could be faster, but generally there are not that many
        // interpolations to warrant it
        for (var i = 0; i < Interpolations.Length; i++)
            if (Interpolations[i].Contains(index))
                return true;

        return false;
    }

    /// <summary>
    ///     Normalizes an index to the <see cref="Source"/>.
    /// </summary>
    /// <param name="index">
    ///     The index to normalize.
    /// </param>
    /// <returns>
    ///     The normalized form of the provided <paramref name="index"/> to the <see cref="Source"/>
    /// </returns>
    public NormalizedIndex Normalize(RelativeIndex index)
        => index - SourceSpan.Start;

    /// <summary>
    ///     Normalizes a <see cref="TextSpan"/> to the <see cref="Source"/>.
    /// </summary>
    /// <param name="span">
    ///     The <see cref="TextSpan"/> to normalize.
    /// </param>
    /// <returns>
    ///     The normalized form of the provided <paramref name="span"/> to the <see cref="Source"/>.
    /// </returns>
    public NormalizedTextSpan Normalize(RelativeTextSpan span)
        => new(Normalize(span.Start), span.Length);

    /// <summary>
    ///     Advances the reader by <paramref name="count"/> characters
    /// </summary>
    /// <param name="count">
    ///     The number of characters to advance by.
    /// </param>
    public void Advance(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            Position++;
        }
    }

    /// <summary>
    ///     Peeks into the source without updating the readers position.
    /// </summary>
    /// <param name="length">
    ///     The number of characters to peek by.
    /// </param>
    /// <returns>
    ///     A string from the current <see cref="Position"/> with the provided <paramref name="length"/> 
    /// </returns>
    /// <remarks>
    ///     If the <paramref name="length"/> exceeds the remaining characters in the <see cref="Source"/>, the length
    ///     is clamped to the end of the source.
    /// </remarks>
    public string Peek(int length = 1)
    {
        var upper = Math.Min(SourceSpan.End, Position + length);

        return Source[TextSpan.FromBounds(
            Normalize(Position),
            Normalize(upper))
        ];
    }

    /// <summary>
    ///     Reads a string from the <see cref="Source"/>, interns it, and advances the reader.
    /// </summary>
    /// <param name="length">
    ///     The number of characters to read.
    /// </param>
    /// <returns>
    ///     An interned string representing the text at the current <see cref="Position"/> with the provided
    ///     <paramref name="length"/>.
    /// </returns>
    public string ReadInternedText(int length) => ReadInternedText(length, out _);

    /// <summary>
    ///     Reads a string from the <see cref="Source"/>, interns it, and advances the reader.
    /// </summary>
    /// <param name="length">
    ///     The number of characters to read.
    /// </param>
    /// <param name="span">The span representing the location of the string that was read.</param>
    /// <returns>
    ///     An interned string representing the text at the current <see cref="Position"/> with the provided
    ///     <paramref name="length"/>.
    /// </returns>
    public string ReadInternedText(int length, out TextSpan span)
    {
        var text = this[Position, length];
        span = new TextSpan(Position, text.Length);
        Advance(length);
        return Intern(text);
    }

    /// <summary>
    ///     Reads a string from the <see cref="Source"/> and interns it; does not advance the readers position.
    /// </summary>
    /// <param name="start">The starting position to read from</param>
    /// <param name="length">
    ///     The number of characters to read.
    /// </param>
    /// <returns>
    ///     An interned string representing the text at the current <see cref="Position"/> with the provided
    ///     <paramref name="length"/>.
    /// </returns>
    public string GetInternedText(NormalizedIndex start, int length)
        => Intern(this[start, length]);

    /// <summary>
    ///     Adds the provided <see cref="StringBuilder"/> to this readers <see cref="StringTable"/>.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> to add.</param>
    /// <returns>
    ///     The interned string that represents the provided <see cref="StringBuilder"/>.
    /// </returns>
    public string Intern(StringBuilder builder)
        => StringTable.Add(builder);

    /// <summary>
    ///     Adds the provided <see cref="ReadOnlySpan{char}"/> to this readers <see cref="StringTable"/>.
    /// </summary>
    /// <param name="chars">The <see cref="ReadOnlySpan{char}"/> to add.</param>
    /// <returns>
    ///     The interned string that represents the provided <see cref="ReadOnlySpan{char}"/>.
    /// </returns>
    public string Intern(ReadOnlySpan<char> chars)
        => StringTable.Add(chars);
}