using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;

namespace UnitTests.ComponentTests;

public abstract class BaseComponentTest
{
    protected CXGraph CurrentGraph => _graph!.Value;
    
    private CXGraph? _graph;
    private ComponentContext? _context;
    private IEnumerator<CXGraph.Node>? _nodeEnumerator;
    private readonly Queue<Diagnostic> _diagnostics = [];
    private readonly HashSet<Diagnostic> _expectedDiagnostics = [];

    public void Graph(
        string cx,
        string? pretext = null,
        bool allowParsingErrors = false
    )
    {
        if(_graph is not null) EOF();

        _graph = null;
        _context = null;
        _nodeEnumerator = null;
        _diagnostics.Clear();
        _expectedDiagnostics.Clear();
        
        var source =
            $""""
             using Discord;
             {pretext}
             ComponentDesigner.cx(
                 $"""
                  {cx.WithNewlinePadding(5)}
                  """
             );
             """";

        var target = Targets.FromSource(source);

        var manager = CXGraphManager.Create(
            new SourceGenerator(),
            "<global>:0",
            target,
            CancellationToken.None
        );

        Assert.Equal(allowParsingErrors, manager.Document.HasErrors);

        _graph = manager.Graph;
        _nodeEnumerator = _graph.Value.RootNodes.SelectMany(EnumerateNodes).GetEnumerator();
        _context = new(_graph.Value);
    }

    private IEnumerable<CXGraph.Node> EnumerateNodes(CXGraph.Node node)
    {
        yield return node;

        foreach (var attrNode in node.AttributeNodes)
        foreach (var child in EnumerateNodes(attrNode))
            yield return child;

        foreach (var childNode in node.Children)
        foreach (var child in EnumerateNodes(childNode))
            yield return child;
    }

    protected void Validate(bool? hasErrors = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        _graph.Value.Validate(_context);

        if (hasErrors.HasValue)
            Assert.Equal(hasErrors.Value, _graph.Value.HasErrors);

        Assert.Empty(_diagnostics);

        PushDiagnostics();
    }

    protected void Renders(string? expected = null)
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        Assert.Empty(_diagnostics);

        var result = _graph.Value.Render(_context);

        PushDiagnostics();

        if (expected is not null) Assert.Equal(expected, result);
    }

    private void PushDiagnostics()
    {
        Assert.NotNull(_graph);
        Assert.NotNull(_context);

        IEnumerable<Diagnostic> diag = [.._context.GlobalDiagnostics, .._graph.Value.Diagnostics];
        foreach (var diagnostic in diag)
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


    private CXGraph.Node NextGraphNode()
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.True(_nodeEnumerator.MoveNext());
        return _nodeEnumerator.Current;
    }

    protected void EOF()
    {
        Assert.NotNull(_nodeEnumerator);
        Assert.False(_nodeEnumerator.MoveNext());
        Assert.Empty(_diagnostics);
    }

    protected T Node<T>() where T : ComponentNode
        => Node<T>(out _);

    protected T Node<T>(out CXGraph.Node graphNode) where T : ComponentNode
    {
        graphNode = NextGraphNode();

        Assert.IsType<T>(graphNode.Inner);

        return (T)graphNode.Inner;
    }
}