using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class SubtextTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <sub>
                Hello
            </sub>
            world
            """,
            """
            -# Hello
            world
            """
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <sub>
                This text spans
                across multiple
                lines
            </sub>
            and this doesn't
            """,
            """
            -# This text spans across multiple lines
            and this doesn't
            """
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<sub>Hello</sub> world",
            """
            -# Hello
            world
            """
        );
    }
}