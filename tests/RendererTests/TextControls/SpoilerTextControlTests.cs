using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class SpoilerTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void Basic()
    {
        Renders("<spoiler>foo</spoiler>", "||foo||");
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <spoiler>This spoiler
            is multi line</spoiler>
            """,
            """
            ||This spoiler
            is multi line||
            """
        );
    }

    [Fact]
    public void SyntaxIndented()
    {
        Renders(
            """
            <spoiler>
                spoiler
            </spoiler>
            """,
            """
            ||spoiler||
            """
        );
    }
}