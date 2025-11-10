namespace Discord.CX.Nodes;

public delegate string PropertyRenderer(
    IComponentContext context,
    IComponentPropertyValue value
);