using Discord.CX;
using Xunit.Abstractions;

namespace UnitTests.GeneratorTests;

public sealed class ExternalIncrementalTests(ITestOutputHelper output) : BaseGeneratorTest(output)
{
    [Fact]
    public void GraphDoesntReRender()
    {
        var a = RunCX(
            "<text>Foo</text>"
        );

        var b = RunCX(
            "<text>Foo</text>",
            additionalMethods:
            "public void Foo(){}"
        );

        var first = GetStepValue<ComponentDesignerTarget>(a, TrackingNames.INITIAL_TARGET);
        var second = GetStepValue<ComponentDesignerTarget>(b, TrackingNames.INITIAL_TARGET);
        
        LogRunVisual(b);
    }
}