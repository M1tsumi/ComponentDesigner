using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class HeaderTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void H1()
    {
        Renders(
            "<h1>This is a header</h1>",
            "# This is a header"
        );
    }

    [Fact]
    public void H2()
    {
        Renders(
            "<h2>This is a header</h2>",
            "## This is a header"
        );
    }

    [Fact]
    public void H3()
    {
        Renders(
            "<h3>This is a header</h3>",
            "### This is a header"
        );
    }

    [Fact]
    public void SyntaxIndentation()
    {
        Renders(
            """
            <h1>
                This is a header
            </h1>
            """,
            "# This is a header"
        );
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <h1>
                This is a header
                across multiple
                lines
            </h1>
            """,
            "# This is a header across multiple lines"
        );
    }
}