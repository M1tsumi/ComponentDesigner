using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class StructuralTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void BasicPoll()
    {
        var yesVotes = 1;
        var noVotes = 0;
        
        Graph(
            $"""
             <container color="gold">
                 <h1>Poll</h1>
                 
                 <separator />
                 
                 <h2>Description</h2>
                 Click a button to vote.
                 
                 <separator />
                 
                 <h2>Votes</h2>
                 Yes: {yesVotes}
                 No: {noVotes}
                 
                 <sub>Poll ends in 5 minutes</sub>
                 
                 <separator />
                 
                 <button customId="yes">Yes</button>
                 <button customId="no">No</button>
                 <button customId="remove">Remove My Vote</button>
             </container>
             """,
            options: new GeneratorOptions(
                EnableAutoRows: true,
                EnableAutoTextDisplay: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoTextDisplayComponentNode>();
                Node<SeparatorComponentNode>();
                Node<AutoTextDisplayComponentNode>();
                Node<SeparatorComponentNode>();
                Node<AutoTextDisplayComponentNode>();
                Node<SeparatorComponentNode>();
                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """"
                new global::Discord.ContainerBuilder()
                {
                    AccentColor = global::Discord.Color.Gold,
                    Components =
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "# Poll"
                        ),
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.TextDisplayBuilder(
                            content: 
                            """
                            ## Description
                            Click a button to vote.
                            """
                        ),
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.TextDisplayBuilder(
                            content: 
                            """
                            ## Votes
                            Yes: 1
                            No: 0
                            
                            -# Poll ends in 5 minutes
                            """
                        ),
                        new global::Discord.SeparatorBuilder(),
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "Yes",
                                    customId: "yes"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "No",
                                    customId: "no"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "Remove My Vote",
                                    customId: "remove"
                                )
                            ]
                        }
                    ]
                }
                """"
            );
        }
    }
}