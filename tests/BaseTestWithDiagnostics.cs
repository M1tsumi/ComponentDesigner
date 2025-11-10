using Microsoft.CodeAnalysis;

namespace UnitTests;

public abstract class BaseTestWithDiagnostics
{
    private readonly Queue<Diagnostic> _diagnostics = [];
    private readonly HashSet<Diagnostic> _expectedDiagnostics = [];

    protected void ClearDiagnostics()
    {
        _diagnostics.Clear();
        _expectedDiagnostics.Clear();
    }

    protected void AssertEmptyDiagnostics()
    {
        Assert.Empty(_diagnostics);
    }
    
    protected void PushDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            if(!_expectedDiagnostics.Contains(diagnostic))
                _diagnostics.Enqueue(diagnostic);
    }
    
    protected Diagnostic Diagnostic(
        string id,
        string? title = null,
        string? message = null,
        DiagnosticSeverity? severity = null,
        Location? location = null
    )
    {
        Assert.NotEmpty(_diagnostics);
        
        var diagnostic = _diagnostics.Dequeue();

        Assert.Equal(id, diagnostic.Id);

        if (title is not null) Assert.Equal(title, diagnostic.Descriptor.Title);
        if (message is not null) Assert.Equal(message, diagnostic.GetMessage());
        if (severity is not null) Assert.Equal(severity, diagnostic.Severity);
        if (location is not null) Assert.Equal(location, diagnostic.Location);

        _expectedDiagnostics.Add(diagnostic);
        
        return diagnostic;
    }
    
    protected virtual void EOF()
    {
        Assert.Empty(_diagnostics);
        _expectedDiagnostics.Clear();
    }
}