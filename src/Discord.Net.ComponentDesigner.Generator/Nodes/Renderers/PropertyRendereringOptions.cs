namespace Discord.CX.Nodes;

public readonly record struct PropertyRenderingOptions(
    ComponentTypingContext? TypingContext = null
)
{
    public static readonly PropertyRenderingOptions Default = new();

    public ComponentRenderingOptions ToComponentOptions()
        => new(TypingContext: TypingContext);
}