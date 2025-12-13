using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class ThumbnailTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyThumbnail()
    {
        Graph(
            """
            <thumbnail />
            """
        );
        {
            Node<ThumbnailComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty("thumbnail", "media")
            );

            EOF();
        }
    }

    [Fact]
    public void BasicThumbnail()
    {
        Graph(
            """
            <thumbnail url="abc" />
            """
        );
        {
            var thumbnail = Node<ThumbnailComponentNode>(out var thumbnailNode);

            var url = thumbnailNode.State.GetProperty(thumbnail.Media);

            Assert.True(url is { IsSpecified: true, HasValue: true });

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ThumbnailBuilder(
                    media: new global::Discord.UnfurledMediaItemProperties("abc")
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void FullThumbnail()
    {
        Graph(
            """
            <thumbnail 
                id='123'
                media="abc"
                description="desc"
                spoiler
            />
            """
        );
        {
            var thumbnail = Node<ThumbnailComponentNode>(out var thumbnailNode);

            var id = thumbnailNode.State.GetProperty(thumbnail.Id);
            var url = thumbnailNode.State.GetProperty(thumbnail.Media);
            var description = thumbnailNode.State.GetProperty(thumbnail.Description);
            var spoiler = thumbnailNode.State.GetProperty(thumbnail.Spoiler);

            Assert.True(id is { IsSpecified: true, HasValue: true });
            Assert.True(url is { IsSpecified: true, HasValue: true });
            Assert.True(description is { IsSpecified: true, HasValue: true });
            Assert.True(spoiler is { IsSpecified: true, HasValue: false });
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ThumbnailBuilder(
                    id: 123,
                    media: new global::Discord.UnfurledMediaItemProperties("abc"),
                    description: "desc",
                    isSpoiler: true
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void DescriptionTooLong()
    {
        Graph(
            $"""
             <thumbnail
                 url="abc"
                 description="{new string('a', 2000)}"
             />
             """
        );
        {
            Node<ThumbnailComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("description", "at most 1024 characters in length")
            );
            
            EOF();
        }
    }
}