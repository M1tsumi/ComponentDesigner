using Discord;
using Discord.CX.Parser;
using static Discord.ComponentDesigner;

var x = cx(
    """
    <button customId="SomeId" label="foo"/>
    """
);

Console.WriteLine(x);