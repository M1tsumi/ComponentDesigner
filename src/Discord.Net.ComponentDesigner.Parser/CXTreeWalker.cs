using System.Collections;
using System.Collections.Generic;

namespace Discord.CX.Parser;

public static class CXTreeWalker
{
    public static IReadOnlyList<ICXNode> Walk(this ICXNode root)
    {
        var result = new List<ICXNode>();

        var stack = new Stack<(ICXNode Node, int SlotIndex)>([(root, 0)]);

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