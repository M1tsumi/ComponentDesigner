using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Utils;

internal static class IncrementalGeneratorDebugUtils
{
    public static string ToDOTTree<T>(this IncrementalValueProvider<T> provider)
    {
        var node = GetFieldValue(provider, provider.GetType(), "Node");

        if (node is null) return "graph {}";

        var parts = new List<string>();

        var visited = new HashSet<object>();

        AddNodeToTree(node, parts, visited);
        
        return
            $$"""
              digraph {
                node [
                  style=filled
                  shape=plaintext
                ]
                
                {{string.Join(Environment.NewLine, parts).WithNewlinePadding(2)}}
              }
              """;
    }

    private static void AddNodeToTree(
        object? node,
        List<string> graph,
        HashSet<object> visited
    )
    {
        if (node is null) return;

        if (!visited.Add(node)) return;
        
        var type = node.GetType();
        var hash = node.GetHashCode();
        switch (type.Name)
        {
            case "BatchNode`1":
            {
                var input = GetNodeFieldValue(node, type);
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Batch Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     {input!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(input, graph, visited);
                break;
            }
            case "CombineNode`2":
            {
                var left = GetNodeFieldValue(node, type, "_input1");
                var right = GetNodeFieldValue(node, type, "_input2");
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Combine Node",
                           ("Left Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Right Type", PrettyTypeName(type.GenericTypeArguments[1])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     {left!.GetHashCode()} -> {hash}
                     {right!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(left, graph, visited);
                AddNodeToTree(right, graph, visited);
                break;
            }
            case "InputNode`1":
            {
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Input Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     """
                );
                break;
            }
            case "SyntaxInputNode`1":
            {
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Syntax Input Node",
                           ("Type", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     """
                );
                break;
            }
            case "TransformNode`2":
            {
                var input = GetNodeFieldValue(node, type);
                graph.Add(
                    $"""
                     {node.GetHashCode()} [
                       label={Table(
                           "Transform Node",
                           ("From", PrettyTypeName(type.GenericTypeArguments[0])),
                           ("To", PrettyTypeName(type.GenericTypeArguments[1])),
                           ("Name", GetFieldValue(node, type, "_name")?.ToString())
                       )}
                     ]
                     {input!.GetHashCode()} -> {hash}
                     """
                );
                AddNodeToTree(input, graph, visited);
                break;
            }
        }
    }

    private static string PrettyTypeName(Type t)
    {
        if (t.IsArray)
        {
            return PrettyTypeName(t.GetElementType()!) + "[]";
        }

        if (t.IsGenericType)
        {
            return string.Format(
                "{0}<{1}>",
                t.Name.Substring(0, t.Name.LastIndexOf("`", StringComparison.InvariantCulture)),
                string.Join(", ", t.GetGenericArguments().Select(PrettyTypeName)));
        }

        return t.Name;
    }

    private static string Table(string title, params IEnumerable<(string Name, string? Value)?> parts)
    {
        var partsArr = parts
            .Where(x => x.HasValue && !string.IsNullOrWhiteSpace(x.Value.Value))
            .Select(x => x!.Value)
            .ToArray();

        if (partsArr.Length is 0) return $"<<b>{title}</b>>";

        return
            $"""
             <<table border="0" cellborder="1" cellspacing="0" cellpadding="4">
             <tr><td colspan="2"><b>{title}</b></td></tr>
             {string.Join(Environment.NewLine, partsArr.Select(x => 
                 $"<tr><td>{Clean(x.Name)}</td><td>{Clean(x.Value!)}</td></tr>"))}
             </table>>
             """;

        static string Clean(string v)
            => v.Replace("<", "&lt;").Replace(">", "&gt;");
    }


    private static object? GetNodeFieldValue(object node, Type type, string name = "_sourceNode")
        => GetFieldValue(node, type, name);

    private static object? GetFieldValue(object node, Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        return field?.GetValue(node);
    }
}