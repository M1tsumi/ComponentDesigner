using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class UnderlineTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <mark>
                Hello
            </mark>
            world
            """,
            """
            __Hello__
            world
            """
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <mark>
                This text spans
                across multiple
                lines
            </mark>
            and this doesn't
            """,
            """
            __This text spans
            across multiple
            lines__
            and this doesn't
            """
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<mark>Hello</mark> world",
            "__Hello__ world"
        );
    }
}