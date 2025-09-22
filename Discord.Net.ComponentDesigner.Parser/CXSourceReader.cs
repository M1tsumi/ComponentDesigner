using Microsoft.CodeAnalysis.Text;
using System;

namespace Discord.CX.Parser;

public sealed class CXSourceReader
{
    public char this[int index]
        => SourceSpan.Contains(index)
            ? Source[NormalizePosition(index)]
            : CXLexer.NULL_CHAR;
    
    public string this[TextSpan span]
        => Source[NormalizePosition(span)];

    public bool IsEOF => Position >= SourceSpan.End;

    public char Current => this[Position];

    public char Next => this[Position + 1];

    public char Previous => this[Position - 1];

    public bool IsInInterpolation => IsAtInterpolation(Position);

    public int Position { get; set; }
    public CXSourceText Source { get; }

    public TextSpan SourceSpan { get; }
    
    public TextSpan[] Interpolations { get; }
    
    public int WrappingQuoteCount { get; }
    
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
        Interpolations = interpolations;
        WrappingQuoteCount = wrappingQuoteCount;
    }
    
    public bool IsAtInterpolation(int index)
    {
        for (var i = 0; i < Interpolations.Length; i++)
            if (Interpolations[i].Contains(index))
                return true;

        return false;
    }

    private int NormalizePosition(int position)
        => position - SourceSpan.Start;

    private TextSpan NormalizePosition(TextSpan span)
        => new(NormalizePosition(span.Start), span.Length);

    public void Advance(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            Position++;
        }
    }

    public string Peek(int count = 1)
    {
        var upper = Math.Min(SourceSpan.End, Position + count);

        return Source[TextSpan.FromBounds(
            NormalizePosition(Position),
            NormalizePosition(upper))
        ];
    }
}
