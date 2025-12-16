using System.Collections.Generic;
using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public readonly record struct ComponentInitializationGraphNode(
    ComponentNode Node,
    ICXNode CXNode,
    IReadOnlyList<CXNode> Children
);