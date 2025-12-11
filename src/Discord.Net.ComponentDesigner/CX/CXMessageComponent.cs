using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Discord;

public sealed class CXMessageComponent : INestedComponent, IComponentContainer
{
    public static readonly CXMessageComponent Empty = new(Array.Empty<IMessageComponent>());
    
    public bool IsSingle => Components.Count is 1;
    public bool IsEmpty => Components.Count is 0;

    public IReadOnlyCollection<IMessageComponent> Components
        => _components ??= [.._builders?.Select(x => x.Build()) ?? throw new ArgumentNullException(nameof(_components))];
    
    public IReadOnlyCollection<IMessageComponentBuilder> Builders
        => _builders ??= [.._components?.Select(x => x.ToBuilder()) ??  throw new ArgumentNullException(nameof(_builders))];
    
    private IReadOnlyCollection<IMessageComponentBuilder>? _builders;
    private IReadOnlyCollection<IMessageComponent>? _components;

    private MessageComponent? _built;
    
    public CXMessageComponent(params IEnumerable<IMessageComponentBuilder> components)
    {
        _builders = [..components];
    }

    public CXMessageComponent(params IEnumerable<IMessageComponent> components)
    {
        _components = [..components];
    }

    public CXMessageComponent(MessageComponent component) : this(component.Components)
    {
    }
    
    public MessageComponent ToDiscordComponents()
        => _built ??= new ComponentBuilderV2(Components).Build();

    public static implicit operator MessageComponent(CXMessageComponent self)
        => self.ToDiscordComponents();

    public static implicit operator CXMessageComponent(MessageComponent comp)
        => new(comp);
    
    public static implicit operator Optional<MessageComponent>(CXMessageComponent self)
        => self.ToDiscordComponents();
    
    public static CXMessageComponent operator +(CXMessageComponent left, CXMessageComponent right)
        => new([..left.Builders, ..right.Builders]);
    
    ImmutableArray<ComponentType> IComponentContainer.SupportedComponentTypes => [
        ComponentType.ActionRow,
        ComponentType.Section,
        ComponentType.MediaGallery,
        ComponentType.Separator,
        ComponentType.Container,
        ComponentType.File,
        ComponentType.TextDisplay
    ];

    int IComponentContainer.MaxChildCount => ComponentBuilderV2.MaxChildCount;

    List<IMessageComponentBuilder> IComponentContainer.Components => [..Builders];

    IComponentContainer IComponentContainer.AddComponent(IMessageComponentBuilder component)
        => new CXMessageComponent([..Builders, component]);

    IComponentContainer IComponentContainer.AddComponents(params IMessageComponentBuilder[] components)
        => new CXMessageComponent([..Builders, ..components]);

    IComponentContainer IComponentContainer.WithComponents(IEnumerable<IMessageComponentBuilder> components)
        => new CXMessageComponent(components);
}