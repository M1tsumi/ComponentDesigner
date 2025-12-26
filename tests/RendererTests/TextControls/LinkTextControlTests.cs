using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class LinkTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void Basic()
    {
        Renders(
            "<a href=\"example.com\">Click me</a>",
            "[Click me](example.com)"
        );
    }

    [Fact]
    public void SyntaxIndented()
    {
        Renders(
            """
            <a href="https://example.com">
                click me
            </a>
            """,
            "[click me](https://example.com)"
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <a href="https://example.com">
                click me
                with multiple
                lines
            </a>
            """,
            """
            [click me
            with multiple
            lines](https://example.com)
            """
        );
    }
}