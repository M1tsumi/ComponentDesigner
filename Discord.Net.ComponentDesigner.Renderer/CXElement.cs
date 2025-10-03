namespace Discord;

public abstract class CXElement
{
    protected IMessageComponentBuilder Children { get; }
    public abstract IMessageComponentBuilder Render();
}

internal sealed class CXNode(IMessageComponentBuilder builder) : CXElement
{
    public override IMessageComponentBuilder Render() => builder;
} 