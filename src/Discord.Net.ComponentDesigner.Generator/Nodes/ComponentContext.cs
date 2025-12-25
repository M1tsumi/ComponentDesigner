using System;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes.Components;

namespace Discord.CX.Nodes;

public class ComponentContext : IComponentContext
{
    public KnownTypes KnownTypes => Compilation.GetKnownTypes();
    public virtual Compilation Compilation => _graph.Compilation;
    public virtual CXDesignerGeneratorState CX => _graph.CX;

    public GeneratorOptions Options => _graph.Options;
    
    public ComponentTypingContext RootTypingContext { get; }

    private readonly CXGraph _graph;

    private readonly Dictionary<string, int> _varsCount = [];
    
    public ComponentContext(CXGraph graph, ComponentTypingContext? typingContext = null)
    {
        _graph = graph;
        
        RootTypingContext = typingContext ?? ComponentTypingContext.Default;
    }

    public string GetVariableName(string? hint = null)
    {
        hint ??= "local_";

        if (!_varsCount.TryGetValue(hint, out var count))
            _varsCount[hint] = 1;
        else
            _varsCount[hint] = count + 1;

        return $"{hint}{count}";
    }

    public string GetDesignerValue(int index, string? type = null)
        => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";
    
    public DesignerInterpolationInfo GetInterpolationInfo(CXToken token)
        => GetInterpolationInfo(_graph.Document.GetInterpolationIndex(token));
    public DesignerInterpolationInfo GetInterpolationInfo(int index) => _graph.InterpolationInfos[index];
}
