using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DiagnosticInfo = Discord.CX.DiagnosticInfo;

namespace UnitTests;

public abstract class BaseTestWithDiagnostics
{
    private readonly Queue<DiagnosticInfo> _diagnostics = [];
    private readonly HashSet<DiagnosticInfo> _expectedDiagnostics = [];

    protected void ClearDiagnostics()
    {
        _diagnostics.Clear();
        _expectedDiagnostics.Clear();
    }

    protected void AssertEmptyDiagnostics()
    {
        Assert.Empty(_diagnostics);
    }

    protected void PushDiagnostics(IEnumerable<DiagnosticInfo> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            if (!_expectedDiagnostics.Contains(diagnostic))
                _diagnostics.Enqueue(diagnostic);
    }

    protected DiagnosticInfo Diagnostic(
        DiagnosticDescriptor descriptor,
        ICXNode node
    ) => Diagnostic(descriptor, node.Span);
    
    protected DiagnosticInfo Diagnostic(
        DiagnosticDescriptor descriptor,
        TextSpan? span = null
    ) => Diagnostic(
        descriptor.Id,
        descriptor.Title.ToString(),
        descriptor.MessageFormat.ToString(),
        descriptor.DefaultSeverity,
        span
    );

    protected DiagnosticInfo Diagnostic(
        string id,
        string? title = null,
        string? message = null,
        DiagnosticSeverity? severity = null,
        TextSpan? span = null
    )
    {
        Assert.NotEmpty(_diagnostics);

        var diagnostic = _diagnostics.Dequeue();

        Assert.Equal(id, diagnostic.Descriptor.Id);

        if (title is not null) Assert.Equal(title, diagnostic.Descriptor.Title);
        if (message is not null) Assert.Equal(message, diagnostic.Descriptor.MessageFormat);
        if (severity is not null) Assert.Equal(severity, diagnostic.Descriptor.DefaultSeverity);
        if (span is not null) Assert.Equal(span, diagnostic.Span);

        _expectedDiagnostics.Add(diagnostic);

        return diagnostic;
    }

    protected virtual void EOF()
    {
        Assert.Empty(_diagnostics);
        _expectedDiagnostics.Clear();
    }
}