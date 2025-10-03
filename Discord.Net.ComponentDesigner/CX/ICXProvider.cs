namespace Discord;

public interface ICXProvider<in TState>
{
    static abstract IMessageComponentBuilder Render(TState state);
}