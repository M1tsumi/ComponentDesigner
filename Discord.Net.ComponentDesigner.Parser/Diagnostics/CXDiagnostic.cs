using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Parser;

public readonly record struct CXDiagnostic(
    DiagnosticSeverity Severity,
    CXErrorCode Code,
    string Message,
    TextSpan Span
)
{
    public static CXDiagnostic MissingElementClosingTag(CXToken? identifier, TextSpan span)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.MissingElementClosingTag,
            identifier is not null
                ? $"Missing closing tag for '{identifier.Value}'"
                : "Missing fragment closing tag",
            span
        );
    
    public static CXDiagnostic InvalidRootElement(CXToken token)
    {
        return new CXDiagnostic(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidRootElement,
            $"'{token.Kind}' is not a valid root element",
            token.FullSpan
        );
    }
    
    public static CXDiagnostic InvalidElementChildToken(CXToken token)
    {
        return new CXDiagnostic(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidElementChildToken,
            $"'{token.Kind}' is not a valid child of an element",
            token.FullSpan
        );
    }
    
    public static CXDiagnostic InvalidStringLiteralToken(CXToken token)
    {
        return new(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidStringLiteralToken,
            $"'{token.Kind}' is not valid within a string literal",
            token.FullSpan
        );
    }

    public static CXDiagnostic InvalidAttributeValue(CXToken token)
    {
        if (
            token.Kind is CXTokenKind.ForwardSlashGreaterThan
            or CXTokenKind.GreaterThan
            or CXTokenKind.EOF
        )
        {
            return new(
                DiagnosticSeverity.Error,
                CXErrorCode.MissingAttributeValue,
                "Missing attribute value",
                token.FullSpan
            );
        }

        return new CXDiagnostic(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidAttributeValue,
            $"'{token.Kind}' is not a valid attribute value token",
            token.FullSpan
        );
    }

    public static CXDiagnostic UnexpectedToken(
        CXToken token,
        params CXTokenKind[] expected
    ) => new(
        DiagnosticSeverity.Error,
        CXErrorCode.UnexpectedToken,
        $"Unexpected token; expected {FormatExpected(expected)}, but got '{token.Kind}'",
        token.FullSpan
    );

    private static string FormatExpected(CXTokenKind[] kinds)
    {
        if (kinds.Length is 0) throw new ArgumentOutOfRangeException(nameof(kinds));

        if (kinds.Length is 1) return $"'{kinds[0]}'";

        return
            $"one of {string.Join(", ", kinds.Take(kinds.Length - 1).Select(x => $"'{x}'"))} or '{kinds[kinds.Length - 1]}'";
    }
}