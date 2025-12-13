using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class LabelTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    // waiting for label builder in dnet before testing.
    
    [Fact]
    public void EmptyLabel()
    {
        Graph(
            """
            <label />
            """
        );
        {
            Node<LabelComponentNode>();
            
            Validate(hasErrors: true);
            
            Diagnostic(
                Diagnostics.MissingRequiredProperty("label", "component")
            );

            Diagnostic(
                Diagnostics.MissingRequiredProperty("label", "value")
            );
            
            Diagnostic(
                Diagnostics.MissingTypeInAssembly("LabelBuilderType")
            );

            EOF();
        }
    }
}