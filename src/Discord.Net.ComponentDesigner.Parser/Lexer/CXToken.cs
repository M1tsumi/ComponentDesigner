using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX.Parser;

/// <summary>
///     An AST node representing a terminal token within the CX syntax.
/// </summary>
/// <param name="Kind">The kind of the token.</param>
/// <param name="LeadingTrivia">The leading trivia of the token.</param>
/// <param name="TrailingTrivia">The trailing trivia of the token.</param>
/// <param name="Flags">The flags of the token.</param>
/// <param name="Value">The raw value of the token.</param>
/// <param name="Diagnostics">The diagnostics for the token.</param>
public sealed record CXToken(
    CXTokenKind Kind,
    LexedCXTrivia LeadingTrivia,
    LexedCXTrivia TrailingTrivia,
    CXTokenFlags Flags,
    string Value,
    params IReadOnlyList<CXDiagnosticDescriptor> Diagnostics
) : ICXNode
{
    /// <summary>
    ///     Gets the interpolation index of this token.
    /// </summary>
    /// <remarks>
    ///     If this tokens <see cref="Kind"/> is not an <see cref="CXTokenKind.Interpolation"/>, <see langword="null"/>
    ///     is returned.
    /// </remarks>
    public int? InterpolationIndex
        => Document is null || Kind is not CXTokenKind.Interpolation ? null : Document.GetInterpolationIndex(this);

    /// <summary>
    ///     Gets the full character width of this token. 
    /// </summary>
    public int Width => LeadingTrivia.Length + Value.Length + TrailingTrivia.Length;

    public TextSpan Span => new(this.Offset + LeadingTrivia.Length, Value.Length);
    public TextSpan FullSpan => new(this.Offset, Width);

    /// <inheritdoc/>
    public CXNode? Parent { get; set; }

    /// <inheritdoc/>
    public CXDocument? Document => Parent?.Document;

    /// <inheritdoc/>
    public bool HasErrors
        => IsInvalid ||
           IsMissing ||
           Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

    /// <summary>
    ///     Gets whether this token is missing from the underlying <see cref="CXSourceText"/>.
    /// </summary>
    public bool IsMissing => (Flags & CXTokenFlags.Missing) != 0;

    /// <summary>
    ///     Gets whether this token has a zero character width.
    /// </summary>
    public bool IsZeroWidth => Span.IsEmpty;

    /// <summary>
    ///     Gets whether this token is an <see cref="CXTokenKind.Invalid"/> kind.
    /// </summary>
    public bool IsInvalid => Kind is CXTokenKind.Invalid;

    /// <summary>
    ///     Creates a new synthetic token.
    /// </summary>
    /// <param name="kind">The kind of the synthetic token.</param>
    /// <param name="span">The <see cref="TextSpan"/> of the synthetic token.</param>
    /// <param name="flags">The flags of the synthetic token.</param>
    /// <param name="value">The value of the synthetic token.</param>
    /// <param name="diagnostics">The diagnostics of the synthetic token.</param>
    /// <returns>The newly created synthetic token.</returns>
    public static CXToken CreateSynthetic(
        CXTokenKind kind,
        TextSpan? span = null,
        CXTokenFlags? flags = null,
        string? value = null,
        IEnumerable<CXDiagnosticDescriptor>? diagnostics = null
    )
    {
        return new CXToken(
            kind,
            LexedCXTrivia.Empty,
            LexedCXTrivia.Empty,
            CXTokenFlags.Synthetic | (flags ?? CXTokenFlags.None),
            value ?? string.Empty,
            [..diagnostics ?? []]
        );
    }

    /// <summary>
    ///     Creates a new <see cref="CXToken"/> with the <see cref="CXTokenFlags.Missing"/> flag set.
    /// </summary>
    /// <param name="kind">The kind of the token to create.</param>
    /// <param name="diagnostics">The diagnostics of the token.</param>
    /// <returns>The newly created token.</returns>
    public static CXToken CreateMissing(
        CXTokenKind kind,
        params IEnumerable<CXDiagnosticDescriptor> diagnostics
    ) => CreateMissing(kind, string.Empty, diagnostics: diagnostics);

    /// <summary>
    ///     Creates a new <see cref="CXToken"/> with the <see cref="CXTokenFlags.Missing"/> flag set.
    /// </summary>
    /// <param name="kind">The kind of the token to create.</param>
    /// <param name="value">The value of the token.</param>
    /// <param name="leadingTrivia">The leading trivia of the token to create.</param>
    /// <param name="trailingTrivia">The trailing trivia of the token to create.</param>
    /// <param name="diagnostics">The diagnostics of the token.</param>
    /// <returns>The newly created token.</returns>
    public static CXToken CreateMissing(
        CXTokenKind kind,
        string value,
        LexedCXTrivia? leadingTrivia = null,
        LexedCXTrivia? trailingTrivia = null,
        params IEnumerable<CXDiagnosticDescriptor> diagnostics
    ) => new(
        kind,
        leadingTrivia ?? LexedCXTrivia.Empty,
        trailingTrivia ?? LexedCXTrivia.Empty,
        Flags: CXTokenFlags.Missing,
        Value: value,
        Diagnostics: [..diagnostics]
    );

    /// <inheritdoc/>
    public void ResetCachedState()
    {
    }

    /// <inheritdoc/>
    public override string ToString() => ToString(false, false);

    /// <inheritdoc/>
    public string ToString(bool includeLeadingTrivia, bool includeTrailingTrivia)
        => (includeLeadingTrivia, includeTrailingTrivia) switch
        {
            (false, false) => Value,
            (true, true) => $"{LeadingTrivia}{Value}{TrailingTrivia}",
            (false, true) => $"{Value}{TrailingTrivia}",
            (true, false) => $"{LeadingTrivia}{Value}"
        };

    /// <inheritdoc/>
    public bool Equals(CXToken? other)
        => CXNodeEqualityComparer.Default.Equals(this, other);

    /// <inheritdoc/>
    public bool Equals(ICXNode? other)
        => other is CXToken token && Equals(token);

    /// <inheritdoc/>
    public override int GetHashCode()
        => CXNodeEqualityComparer.Default.GetHashCode(this);


    /// <inheritdoc/>
    int ICXNode.GraphWidth => 0;

    /// <inheritdoc/>
    IReadOnlyList<ICXNode> ICXNode.Slots => [];

    IReadOnlyList<CXDiagnosticDescriptor> ICXNode.DiagnosticDescriptors
    {
        get => Diagnostics;
        init => Diagnostics = value;
    }

    object ICloneable.Clone() => this with {};
}