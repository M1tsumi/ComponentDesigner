using Discord.CX.Nodes;

namespace UnitTests.RendererTests;

public sealed class UnfurledMediaItemTests : BaseRendererTest
{
    [Fact]
    public void BasicUnfurledMediaItem()
    {
        AssertRenders(
            """
            'https://example.com'
            """,
            Renderers.UnfurledMediaItem,
            """
            new global::Discord.UnfurledMediaItemProperties("https://example.com")
            """
        );
    }
}