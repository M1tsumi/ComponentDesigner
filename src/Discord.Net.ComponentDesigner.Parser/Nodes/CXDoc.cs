using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Discord.CX.Util;

namespace Discord.CX.Parser;

public sealed class CXDoc : CXNode
{
    public override CXParser Parser { get; }

    public CXSourceText Source => Parser.Reader.Source;

    public IReadOnlyList<CXToken> Tokens { get; }

    public IReadOnlyList<CXNode> RootNodes { get; private set; }

    public readonly CXToken[] InterpolationTokens;

    internal StringInternTable StringTable => Parser.Reader.StringTable;

    public CXDoc(
        CXParser parser,
        IReadOnlyList<CXNode> rootNodes
    )
    {
        Parser = parser;
        Tokens = parser.Tokens;
        Slot(RootNodes = rootNodes);
        InterpolationTokens = parser.Lexer.InterpolationMap;
    }

    public int GetInterpolationIndex(CXToken token)
    {
        if (token.Kind is not CXTokenKind.Interpolation) return -1;
        return Array.IndexOf(InterpolationTokens, token);
    }

    public bool IsInterpolation(CXToken token) => TryGetInterpolationIndex(token, out _);
    public bool TryGetInterpolationIndex(CXToken token, out int index)
    {
        index = GetInterpolationIndex(token);
        return index != -1;
    }

    public CXDoc IncrementalParse(
        CXSourceReader reader,
        IReadOnlyList<TextChange> changes,
        out IncrementalParseResult result,
        CancellationToken token = default
    )
    {
        var affectedRange = TextChangeRange.Collapse(changes.Select(x => (TextChangeRange)x));

        var parser = new CXParser(reader, this, affectedRange, token);

        var context = new IncrementalParseContext(changes, affectedRange);

        var children = new List<CXElement>();

        while (parser.CurrentToken.Kind is not CXTokenKind.EOF and not CXTokenKind.Invalid)
        {
            var element = parser.ParseElement();

            children.Add(element);

            if (element.Width is 0) break;
        }

        var reusedNodes = new List<ICXNode>();
        var flatGraph = GetFlatGraph();

        foreach (var reusedNode in Parser.BlendedNodes)
        {
            reusedNodes.Add(reusedNode);

            if(reusedNode is not CXNode concreteNode) continue;

            // add descendants to reused collection
            reusedNodes.AddRange(concreteNode.Descendants);
        }

        result = new(
            reusedNodes,
            [..GetFlatGraph().Except(Parser.BlendedNodes)],
            changes,
            affectedRange
        );

        return new CXDoc(parser, children);
    }

    public List<ICXNode> GetFlatGraph()
    {
        var result = new List<ICXNode>();

        var stack = new Stack<(ICXNode Node, int SlotIndex)>([(this, 0)]);

        while (stack.Count > 0)
        {
            var (node, index) = stack.Pop();

            if (node is CXToken token)
            {
                result.Add(token);
                continue;
            }

            if (node is CXNode concreteNode)
            {
                if(index is 0) result.Add(node);

                if (concreteNode.Slots.Count > index)
                {
                    // enqueue self
                    stack.Push(
                        (concreteNode, index + 1)
                    );

                    // enqueue child
                    stack.Push(
                        (concreteNode.Slots[index].Value, 0)
                    );

                    continue;
                }

                // we do nothing
            }
        }

        return result;
    }
}
