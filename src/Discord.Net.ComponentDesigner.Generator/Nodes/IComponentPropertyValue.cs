using Discord.CX.Parser;

namespace Discord.CX.Nodes;

public interface IComponentPropertyValue
{
    CXValue? Value { get; }
    
    CXGraph.Node? Node { get; }
    
    bool IsSpecified { get; }
    
    bool HasValue { get; }
    
    bool IsAttributeValue { get; }
    
    bool RequiresValue { get; }
    
    bool IsOptional { get; }
    
    string PropertyName { get; }
}