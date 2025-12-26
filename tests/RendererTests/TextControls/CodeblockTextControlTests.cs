using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class CodeblockTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void Language()
    {
        Renders(
            """
            <c lang="csharp">
                int x = 1;
            </c>
            """,
            """
            ```csharp
            int x = 1;
            ```
            """
        );
    }

    [Fact]
    public void Inference()
    {
        Renders("<c>inline</c>", "`inline`");
        
        Renders(
            """
            <c>
                block
            </c>
            """,
            """
            ```
            block
            ```
            """
        );
    }

    [Fact]
    public void Inline()
    {
        Renders(
            "<c>inline</c>",
            "`inline`"
        );
    }

    [Fact]
    public void MultilineInline()
    {
        Renders(
            """
            <c inline>
                multiline
                inline
                codeblock
            </c>
            """,
            """
            `multiline
            inline
            codeblock`
            """
        );
    }

    [Fact]
    public void SyntaxIndentedInline()
    {
        Renders(
            """
            <c inline>
                indented codeblock
            </c>
            """,
            "`indented codeblock`"
        );
    }
}