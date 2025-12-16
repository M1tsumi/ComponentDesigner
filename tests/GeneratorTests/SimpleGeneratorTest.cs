using Xunit.Abstractions;

namespace UnitTests.GeneratorTests;

public class SimpleGeneratorTest(ITestOutputHelper output) : BaseGeneratorTest(output)
{
    [Fact]
    public void Test()
    {
        RunCX(
            """
            <text>Hello</text>
            """,
            expected: """
            new global::Discord.TextDisplayBuilder(
                content: "Hello"
            )
            """
        );
    }
}