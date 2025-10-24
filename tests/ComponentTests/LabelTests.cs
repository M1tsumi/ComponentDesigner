using Discord.CX;
using Discord.CX.Nodes.Components;

namespace UnitTests.ComponentTests;

public sealed class LabelTests : BaseComponentTest
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
                Diagnostics.MissingRequiredProperty.Id,
                message: "'label' requires the property 'component' to be specified"
            );

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'label' requires the property 'value' to be specified"
            );
            
            Diagnostic(
                Diagnostics.MissingTypeInAssembly.Id
            );

            EOF();
        }
    }
}