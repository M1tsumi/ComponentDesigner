using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class BoldTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <b>
                Hello
            </b>
            world
            """,
            """
            **Hello**
            world
            """
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <b>
                This text spans
                across multiple
                lines
            </b>
            and this doesn't
            """,
            """
            **This text spans
            across multiple
            lines**
            and this doesn't
            """
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<b>Hello</b> world",
            "**Hello** world"
        );
    }
}