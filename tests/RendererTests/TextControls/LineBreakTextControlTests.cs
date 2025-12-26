using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class LineBreakTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void LineBreak()
    {
        Renders(
            "this is<br/>on a newline",
            """
            this is
            on a newline
            """
        );
    }
}