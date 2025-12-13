using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discord.CX.Parser.DebugUtils;

partial class ASTFormatter
{
    public static string ToDOTFormat(this ICXNode root)
    {
        var flat = root.Walk().ToArray();
        var tokens = flat.OfType<CXToken>().ToArray();

        var parts = new List<string>()
        {
            "rankdir=BT",
            "graph [splines=ortho, overlap=false, splines=true]"
        };

        parts.Add(
            $$"""
              node [shape = box];
              {
                rank=same
                {{string.Join(
                    $"{Environment.NewLine}  ",
                    tokens.Select(x =>
                        $"{Array.IndexOf(flat, x)} [label=<<b>{x.Kind}</b><br/>{HtmlClean(x.Value)}>]"
                    )
                )}}
              }
              """
        );

        for (var i = 0; i < flat.Length; i++)
        {
            var node = flat[i];

            if (node is ICXCollection { Count: 0 }) continue;

            if (node is CXNode)
            {
                // add the node
                parts.Add($"{i} [label=\"{node.GetType().Name}\"]");
            }

            switch (node)
            {
                case CXDoc cxDoc:
                {
                    for (var j = 0; j < cxDoc.RootNodes.Count; j++)
                    {
                        var rootNode = cxDoc.RootNodes[j];
                        var index = Array.IndexOf(flat, rootNode);

                        parts.Add($"{index} -- {i} [label=\"RootNodes[{j}]\"]");
                    }

                    break;
                }
                case CXElement cxElement:
                {
                    AddEdge(i, cxElement.ElementStartOpenToken);
                    AddEdge(i, cxElement.ElementStartNameToken);
                    AddEdge(i, cxElement.Attributes);
                    AddEdge(i, cxElement.ElementStartCloseToken);

                    AddEdge(i, cxElement.Children);
                    AddEdge(i, cxElement.ElementEndOpenToken);
                    AddEdge(i, cxElement.ElementEndNameToken);
                    AddEdge(i, cxElement.ElementEndCloseToken);

                    break;
                }
                case CXAttribute cxAttribute:
                    AddEdge(i, cxAttribute.Identifier);
                    AddEdge(i, cxAttribute.EqualsToken);
                    AddEdge(i, cxAttribute.Value);
                    break;
                case CXValue.Element element:
                    AddEdge(i, element.OpenParenthesis);
                    AddEdge(i, element.Value);
                    AddEdge(i, element.CloseParenthesis);
                    break;
                case CXValue.Interpolation interpolation:
                    AddEdge(i, interpolation.Token);
                    break;
                case CXValue.StringLiteral stringLiteral:
                    AddEdge(i, stringLiteral.StartToken);
                    AddEdge(i, stringLiteral.Tokens);
                    AddEdge(i, stringLiteral.EndToken);
                    break;
                case CXValue.Multipart multipart:
                    AddEdge(i, multipart.Tokens);
                    break;
                case CXValue.Scalar scalar:
                    AddEdge(i, scalar.Token);
                    break;
                case ICXCollection cxCollection:
                    var list = cxCollection.ToList();
                    for (var j = 0; j < list.Count; j++)
                    {
                        var item = list[j];
                        AddEdge(i, item, $"[{j}]");
                    }

                    break;
            }
        }

        return
            $$"""
              graph syntax {
                {{string.Join(Environment.NewLine, parts).Replace(Environment.NewLine, $"{Environment.NewLine}  ")}}
              }
              """;

        void AddEdge(int index, ICXNode? node, string? name = null)
        {
            if (node is null or ICXCollection { Count: 0 }) return;

            var targetIndex = Array.IndexOf(flat, node);

            if (targetIndex is -1) return;

            var part = $"{targetIndex} -- {index}";

            if (name is not null) part += $" [label=\"{name}\"]";

            parts.Add(part);
        }

        static string HtmlClean(string str)
            => str.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}