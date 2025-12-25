using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class GeneratorFeaturesTest(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void AutoRowReportsDisabledDiagnostic()
    {
        Graph(
            """
            <container>
                <button customId="abc" label="abc"/>
            </container>
            """,
            options: new(
                EnableAutoRows: false
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: true);

            Diagnostic(Diagnostics.AutoRowsDisabled);
        }
    }
    
    [Fact]
    public void AutoTextReportsDisabledDiagnostic()
    {
        Graph(
            """
            <container>
                Hello World
            </container>
            """,
            options: new(
                EnableAutoTextDisplay: false
            )
        );
        {
            Node<ContainerComponentNode>();

            Diagnostic(Diagnostics.AutoTextDisplayDisabled);
        }
    }
}