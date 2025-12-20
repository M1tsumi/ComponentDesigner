using System;
using System.Linq;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Parser;

public readonly record struct CXDiagnosticDescriptor(
    DiagnosticSeverity Severity,
    CXErrorCode Code,
    string Message
)
{
     public static CXDiagnosticDescriptor MissingElementClosingTag(CXToken? identifier)
        => new(
            DiagnosticSeverity.Error,
            CXErrorCode.MissingElementClosingTag,
            identifier is not null
                ? $"Missing closing tag for '{identifier.Value}'"
                : "Missing fragment closing tag"
        );

     public static CXDiagnosticDescriptor InvalidRootElement(CXToken token)
         => new(
             DiagnosticSeverity.Error,
             CXErrorCode.InvalidRootElement,
             $"'{token.Kind}' is not a valid root element"
         );

     public static CXDiagnosticDescriptor InvalidElementChildToken(CXToken token)
         => new(
             DiagnosticSeverity.Error,
             CXErrorCode.InvalidElementChildToken,
             $"'{token.Kind}' is not a valid child of an element"
         );

     public static CXDiagnosticDescriptor InvalidStringLiteralToken(CXToken token)
         => new(
             DiagnosticSeverity.Error,
             CXErrorCode.InvalidStringLiteralToken,
             $"'{token.Kind}' is not valid within a string literal"
         );

    public static CXDiagnosticDescriptor InvalidAttributeValue(CXToken token)
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
                "Missing attribute value"
            );
        }

        return new CXDiagnosticDescriptor(
            DiagnosticSeverity.Error,
            CXErrorCode.InvalidAttributeValue,
            $"'{token.Kind}' is not a valid attribute value token"
        );
    }

    public static CXDiagnosticDescriptor UnexpectedToken(
        CXToken token,
        params CXTokenKind[] expected
    ) => new(
        DiagnosticSeverity.Error,
        CXErrorCode.UnexpectedToken,
        $"Unexpected token; expected {FormatExpected(expected)}, but got '{token.Kind}'"
    );

    private static string FormatExpected(CXTokenKind[] kinds)
    {
        if (kinds.Length is 0) throw new ArgumentOutOfRangeException(nameof(kinds));

        if (kinds.Length is 1) return $"'{kinds[0]}'";

        return
            $"one of {string.Join(", ", kinds.Take(kinds.Length - 1).Select(x => $"'{x}'"))} or '{kinds[kinds.Length - 1]}'";
    }
}

public readonly record struct CXDiagnostic(
    CXDiagnosticDescriptor Descriptor,
    TextSpan Span
)
{
    public DiagnosticSeverity Severity => Descriptor.Severity;
    public CXErrorCode Code => Descriptor.Code;
    public string Message => Descriptor.Message;
}