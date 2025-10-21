using Discord;
using Discord.CX.Parser;
using Sandbox.Examples.Spyfall;
using static Discord.ComponentDesigner;

var pack = new Pack(
    Guid.NewGuid(),
    "Example",
    "Description",
    null,
    [
        new Location(
            Guid.NewGuid(),
            "Location1",
            null,
            [
                new Role(Guid.NewGuid(), "Role1", 100, null),
                new Role(Guid.NewGuid(), "Role2", 100, null),
            ],
            100
        )
    ],
    new(Guid.NewGuid(), "author", 123),
    DateTimeOffset.Now
);

var x = PackExample.CreatePackInfo(pack, 0);

Console.WriteLine(x);