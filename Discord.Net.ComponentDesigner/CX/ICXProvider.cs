namespace Discord;

public interface ICXProvider<in TProps> where TProps : IEquatable<TProps>
{
    static abstract IMessageComponentBuilder Render(TProps state);
}