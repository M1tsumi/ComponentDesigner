using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX.Parser;

public sealed record CXToken(
    CXTokenKind Kind,
    TextSpan FullSpan,
    int LeadingTriviaLength,
    int TrailingTriviaLength,
    CXTokenFlags Flags,
    string FullValue,
    params IReadOnlyList<CXDiagnostic> Diagnostics
) : ICXNode
{
    public string Value => FullValue.Substring(
        LeadingTriviaLength,
        FullValue.Length - LeadingTriviaLength - TrailingTriviaLength
    );

    public TextSpan Span => new(
        FullSpan.Start + LeadingTriviaLength,
        FullValue.Length - LeadingTriviaLength - TrailingTriviaLength
    );

    public CXNode? Parent { get; set; }

    public bool HasErrors
        => _hasErrors ??= (
            Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error) ||
            IsInvalid ||
            IsMissing
        );

    public bool IsMissing => (Flags & CXTokenFlags.Missing) != 0;

    public bool IsZeroWidth => Span.IsEmpty;

    public bool IsInvalid => Kind is CXTokenKind.Invalid;

    public int Width => FullSpan.Length;

    public string LeadingTrivia
        => LeadingTriviaLength is 0 
            ? string.Empty 
            : FullValue.Substring(0, LeadingTriviaLength);

    public string TrailingTrivia
        => TrailingTriviaLength is 0
            ? string.Empty
            : FullValue.Substring(FullValue.Length - TrailingTriviaLength);

    private bool? _hasErrors;

    public static CXToken CreateSynthetic(
        CXTokenKind kind,
        TextSpan? span = null,
        CXTokenFlags? flags = null,
        string? value = null,
        IEnumerable<CXDiagnostic>? diagnostics = null
    )
    {
        return new CXToken(
            kind,
            span ?? default,
            0,
            0,
            CXTokenFlags.Synthetic | (flags ?? CXTokenFlags.None),
            value ?? string.Empty,
            [..diagnostics ?? []]
        );
    }

    public static CXToken CreateMissing(
        CXTokenKind kind,
        TextSpan span,
        params IEnumerable<CXDiagnostic> diagnostics
    ) => CreateMissing(kind, span, string.Empty, diagnostics);

    public static CXToken CreateMissing(
        CXTokenKind kind,
        TextSpan span,
        string value,
        params IEnumerable<CXDiagnostic> diagnostics
    ) => new(
        kind,
        span,
        0,
        0,
        Flags: CXTokenFlags.Missing,
        FullValue: value,
        Diagnostics: [..diagnostics]
    );

    public void ResetCachedState()
    {
        _hasErrors = null;
    }

    public CXToken WithNewPosition(int position)
    {
        if (FullSpan.Start == position) return this;

        return this with { FullSpan = new(position, FullSpan.Length) };
    }

    public override string ToString() => ToString(false, false);
    public string ToFullString() => ToString(true, true);

    public string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia)
        => (includeLeadingTrivia, includeTrailingTrivia) switch
        {
            (false, false) => Value,
            (true, true) => FullValue,
            (false, true) => FullValue.Substring(LeadingTriviaLength),
            (true, false) => FullValue.Substring(0, FullValue.Length - TrailingTriviaLength)
        };

    public bool Equals(CXToken? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return
            Kind == other.Kind &&
            Span.Equals(other.Span) &&
            LeadingTriviaLength == other.LeadingTriviaLength &&
            TrailingTriviaLength == other.TrailingTriviaLength &&
            Flags == other.Flags &&
            Diagnostics.SequenceEqual(other.Diagnostics);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Diagnostics.Aggregate(0, (a, b) => (a * 397) ^ b.GetHashCode());
            hashCode = (hashCode * 397) ^ (int)Kind;
            hashCode = (hashCode * 397) ^ Span.GetHashCode();
            hashCode = (hashCode * 397) ^ LeadingTriviaLength;
            hashCode = (hashCode * 397) ^ TrailingTriviaLength;
            hashCode = (hashCode * 397) ^ (int)Flags;
            return hashCode;
        }
    }

    int ICXNode.GraphWidth => 0;
    IReadOnlyList<CXNode.ParseSlot> ICXNode.Slots => [];
}