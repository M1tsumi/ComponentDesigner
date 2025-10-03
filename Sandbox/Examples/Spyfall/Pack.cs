namespace Sandbox.Examples.Spyfall;

public sealed record Pack(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    IReadOnlyList<Location> Locations,
    User? Author,
    DateTimeOffset CreatedAt
);