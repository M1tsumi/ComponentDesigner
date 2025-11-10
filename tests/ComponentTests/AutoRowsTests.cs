using Discord.CX.Nodes.Components;

namespace UnitTests.ComponentTests;

public sealed class AutoRowsTests : BaseComponentTest
{
    // [Fact]
    // public void BasicAutoRow()
    // {
    //     Graph(
    //         """
    //         <container>
    //             <button 
    //                 customId="abc"
    //                 label="abc"
    //             />
    //         </container>
    //         """,
    //         options: new(
    //             EnableAutoRows: true
    //         )
    //     );
    //     {
    //         Node<ContainerComponentNode>();
    //         {
    //             Node<ActionRowComponentNode>();
    //             {
    //                 Node<ButtonComponentNode>();
    //             }
    //         }
    //         
    //         Validate(hasErrors: false);
    //
    //         Renders(
    //             """
    //             new global::Discord.ContainerBuilder()
    //             {
    //                 Components =
    //                 [
    //                     new global::Discord.ActionRowBuilder()
    //                     {
    //                         Components =
    //                         [
    //                             new global::Discord.ButtonBuilder(
    //                                 label: "abc",
    //                                 customId: "abc"
    //                             )
    //                         ]
    //                     }
    //                 ]
    //             }
    //             """
    //         );
    //         
    //         EOF();
    //     }
    // }
}