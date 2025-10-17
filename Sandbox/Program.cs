using Discord;
using Discord.CX.Parser;
using static Discord.ComponentDesigner;

var x = cx(
    """
    <row>
        <button customId="SomeId"/>
    </row>
    """
);

Console.WriteLine(x);