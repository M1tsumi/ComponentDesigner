namespace Sandbox.Examples.Spyfall;

public sealed record Location(
    Guid Id,
    string Name,
    string? Icon,
    List<Role> Roles,
    short Chance
);