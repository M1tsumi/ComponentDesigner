using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class ItalicTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <i>
                Hello
            </i>
            world
            """,
            """
            _Hello_
            world
            """
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <i>
                This text spans
                across multiple
                lines
            </i>
            and this doesn't
            """,
            """
            _This text spans
            across multiple
            lines_
            and this doesn't
            """
        );
    }

    [Fact]
    public void Basic()
    {
        Renders(
            "<i>Hello</i> world",
            "_Hello_ world"
        );
    }
}