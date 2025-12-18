using Microsoft.CodeAnalysis.Text;
using System;

namespace Discord.CX.Parser;

partial class CXNode
{
    public readonly struct NodeSlot : IEquatable<NodeSlot>
    {
        public ICXNode Value { get; }
        public TextSpan FullSpan => Value.FullSpan;

        public readonly int Id;

        public NodeSlot(int id, ICXNode node)
        {
            Id = id;
            Value = node;
        }

        public static bool operator ==(NodeSlot slot, ICXNode node)
            => slot.Value.Equals(node);

        public static bool operator !=(NodeSlot slot, ICXNode node)
            => !slot.Value.Equals(node);

        public bool Equals(NodeSlot other)
            => Equals(Value, other.Value);

        public override bool Equals(object? obj)
            => obj is NodeSlot other && Equals(other);

        public override int GetHashCode()
            => Value.GetHashCode();
    }
}