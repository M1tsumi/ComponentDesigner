using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public sealed record GraphInitializationContext(
    GraphGeneratorState State,
    CXDoc Document,
    IReadOnlyList<ICXNode> ReusedNodes,
    CXGraph? OldGraph,
    Dictionary<ICXNode, GraphNode> Map,
    IList<DiagnosticInfo> Diagnostics,
    IncrementalParseResult? IncrementalParseResult
) : IComponentContext
{
    public GeneratorOptions Options => State.GeneratorOptions;

    public KnownTypes KnownTypes => Compilation.GetKnownTypes();

    public Compilation Compilation => State.Compilation;

    public ComponentTypingContext RootTypingContext => ComponentTypingContext.Default;

    public EquatableArray<DesignerInterpolationInfo> Interpolations => State.CX.InterpolationInfos;
    public bool IsIncremental => OldGraph is not null && IncrementalParseResult is not null;

    public bool TryReuse(ICXNode node, GraphNode? parent, out GraphNode graphGraphNode)
    {
        if (
            IsIncremental &&
            ReusedNodes.Contains(node) &&
            OldGraph!.NodeMap.TryGetValue(node, out graphGraphNode)
        )
        {
            graphGraphNode = AddToMap(node, graphGraphNode.Reuse(parent));
            return true;
        }

        graphGraphNode = null!;
        return false;
    }

    public GraphNode AddToMap(ICXNode? node, GraphNode graphGraphNode)
    {
        if (node is null) return graphGraphNode;

        if (graphGraphNode.State is null)
            throw new InvalidOperationException("Cannot add a node with null state to the graph");

        return Map[node] = graphGraphNode;
    }
    
    public string GetDesignerValue(int index, string? type = null)
        => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";
    
    public DesignerInterpolationInfo GetInterpolationInfo(CXToken token)
        => GetInterpolationInfo(Document.GetInterpolationIndex(token));
    public DesignerInterpolationInfo GetInterpolationInfo(int index) => State.CX.InterpolationInfos[index];

    public string GetVariableName(string? hint = null)
        => throw new NotSupportedException("Variable declaration in graph initializations aren't supported");
}