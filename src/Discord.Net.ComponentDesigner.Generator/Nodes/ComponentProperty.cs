using Discord.CX.Parser;
using System.Collections.Generic;

namespace Discord.CX.Nodes;

public sealed class ComponentProperty
{
    public static ComponentProperty Id => new(
        "id",
        isOptional: true,
        renderer: Renderers.Integer,
        dotnetPropertyName: "Id"
    );

    public string Name { get; }
    public IReadOnlyList<string> Aliases { get; }

    public bool IsOptional { get; }
    public bool RequiresValue { get; }
    public bool Synthetic { get; }
    public string DotnetPropertyName { get; }
    public string DotnetParameterName { get; }
    public PropertyRenderer Renderer { get; }

    public IReadOnlyList<PropertyValidator> Validators { get; }

    public ComponentProperty(
        string name,
        bool isOptional = false,
        bool requiresValue = true,
        IEnumerable<string>? aliases = null,
        IEnumerable<PropertyValidator>? validators = null,
        PropertyRenderer? renderer = null,
        string? dotnetParameterName = null,
        string? dotnetPropertyName = null,
        bool synthetic = false
    )
    {
        Name = name;
        Aliases = [..aliases ?? []];
        IsOptional = isOptional;
        RequiresValue = requiresValue;
        Synthetic = synthetic;
        DotnetPropertyName = dotnetPropertyName ?? name;
        DotnetParameterName = dotnetParameterName ?? name;
        Renderer = renderer ?? Renderers.CreateDefault(this);
        Validators = [..validators ?? []];
    }
}
