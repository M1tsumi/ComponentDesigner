using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class QuoteTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void Basic()
    {
        Renders("<q>quote</q>", "> quote");
    }

    [Fact]
    public void Multiline()
    {
        Renders(
            """
            <q>This quote
            is multi line</q>
            """,
            """
            > This quote
            > is multi line
            """
        );
    }

    [Fact]
    public void SyntaxIndented()
    {
        Renders(
            """
            <q>
                quote
            </q>
            """,
            """
            > quote
            """
        );
    }
}