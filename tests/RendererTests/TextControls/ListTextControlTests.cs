using Xunit.Abstractions;

namespace UnitTests.RendererTests.TextControls;

public sealed class ListTextControlTests(ITestOutputHelper output) : BaseTextControlTest(output)
{
    [Fact]
    public void Unordered()
    {
        Renders(
            """
            <ul>
                <li>First item</li>
                <li>
                    Second item
                </li>
            </ul>
            """,
            """
            - First item
            - Second item
            """
        );
    }

    [Fact]
    public void Ordered()
    {
        Renders(
            """
            <ol>
                <li>First item</li>
                <li>
                    Second item
                </li>
            </ol>
            """,
            """
              1. First item
              2. Second item
            """
        );
    }

    [Fact]
    public void OrderedProperPrefixIndentation()
    {
        Renders(
            """
            <ol>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
            </ol>
            """,
            """
              1. item
              2. item
              3. item
              4. item
              5. item
              6. item
              7. item
              8. item
              9. item
             10. item
             11. item
            """
        );
    }
}