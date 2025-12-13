using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    TextSpan Span
)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, TextSpan span) => new(descriptor, span);
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ICXNode node) => new(descriptor, node.Span);
}