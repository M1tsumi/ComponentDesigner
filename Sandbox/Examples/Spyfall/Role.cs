namespace Sandbox.Examples.Spyfall;

public sealed record Role(
    Guid Id,
    string Name,
    short Chance,
    string? Icon
);