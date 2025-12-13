namespace Discord.CX.Nodes;

public delegate Result<string> PropertyRenderer(
    IComponentContext context,
    IComponentPropertyValue value
);