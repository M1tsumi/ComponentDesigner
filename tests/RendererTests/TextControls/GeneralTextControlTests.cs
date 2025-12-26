using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class GeneralTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void EscapesPreserved()
    {
        Renders(
            "Some <br/> text&copy; and &pi;",
            $"Some {Environment.NewLine} text© and π"
        );
    }
}