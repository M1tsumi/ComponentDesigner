namespace Sandbox.Examples.Spyfall;

public sealed record User(
    Guid Id,
    string? DisplayName,
    ulong DiscordId
);