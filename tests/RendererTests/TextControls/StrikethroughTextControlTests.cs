using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class StrikethroughTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <del>
                Hello
            </del>
            world
            """,
            """
            ~~Hello~~
            world
            """
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <del>
                This text spans
                across multiple
                lines
            </del>
            and this doesn't
            """,
            """
            ~~This text spans
            across multiple
            lines~~
            and this doesn't
            """
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<del>Hello</del> world",
            "~~Hello~~ world"
        );
    }
}