using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;

namespace Discord.CX.Nodes;

public sealed class ComponentContext
{
    private sealed record DiagnosticScope(
        List<Diagnostic> Bag,
        DiagnosticScope? Parent,
        ComponentContext Context
    ) : IDisposable
    {
        public void Dispose()
        {
            if (Parent is null) return;
            Context._scope = Parent;
        }
    }
    
    public KnownTypes KnownTypes => Compilation.GetKnownTypes();
    public Compilation Compilation => _graph.Manager.Compilation;

    public bool HasErrors => GlobalDiagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

    public List<Diagnostic> GlobalDiagnostics { get; init; } = [];

    private readonly CXGraph _graph;

    private DiagnosticScope _scope;

    public ComponentContext(CXGraph graph)
    {
        _graph = graph;
        _scope = new(GlobalDiagnostics, null, this);
    }

    public IDisposable CreateDiagnosticScope(List<Diagnostic> bag)
        => _scope = new(bag, _scope, this);
    
    public string GetDesignerValue(CXValue.Interpolation interpolation, string? type = null)
        => GetDesignerValue(interpolation.InterpolationIndex, type);

    public string GetDesignerValue(DesignerInterpolationInfo interpolation, string? type = null)
        => GetDesignerValue(interpolation.Id, type);

    public string GetDesignerValue(int index, string? type = null)
        => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";


    public Location GetLocation(ICXNode node)
        => _graph.GetLocation(node);
    public Location GetLocation(TextSpan span)
        => _graph.GetLocation(span);

    public void AddDiagnostic(DiagnosticDescriptor descriptor, ICXNode node, params object?[]? args)
        => AddDiagnostic(Diagnostic.Create(descriptor, GetLocation(node), args));

    public void AddDiagnostic(DiagnosticDescriptor descriptor, TextSpan span, params object?[]? args)
        => AddDiagnostic(Diagnostic.Create(descriptor, GetLocation(span), args));


    public DesignerInterpolationInfo GetInterpolationInfo(CXValue.Interpolation interpolation)
        => GetInterpolationInfo(interpolation.InterpolationIndex);

    public DesignerInterpolationInfo GetInterpolationInfo(int index) => _graph.Manager.InterpolationInfos[index];

    public void AddDiagnostic(Diagnostic diagnostics)
    {
        _scope.Bag.Add(diagnostics);
    }
}
