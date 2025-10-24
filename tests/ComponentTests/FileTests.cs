using Discord.CX;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;

namespace UnitTests.ComponentTests;

public sealed class FileTests : BaseComponentTest
{
    [Fact]
    public void EmptyFile()
    {
        Graph(
            """
            <file />
            """
        );
        {
            Node<FileComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'file' requires the property 'url' to be specified"
            );
            
            EOF();
        }
        
        Graph(
            """
            <file>
            
            </file>
            """
        );
        {
            Node<FileComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'file' requires the property 'url' to be specified"
            );
            
            EOF();
        }
    }

    [Fact]
    public void CompleteFile()
    {
        Graph(
            """
            <file
                id='1'
                url="attachment://file.png"
                spoiler='false'
            />
            """
        );
        {
            var file = Node<FileComponentNode>(out var fileNode);

            var id = fileNode.State.GetProperty(file.Id);
            var url = fileNode.State.GetProperty(file.Url);
            var spoiler = fileNode.State.GetProperty(file.Spoiler);
            
            Assert.True(id is { IsSpecified: true, HasValue: true });
            Assert.True(url is { IsSpecified: true, HasValue: true });
            Assert.True(spoiler is { IsSpecified: true, HasValue: true });
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.FileComponentBuilder(
                    id: 1,
                    media: new global::Discord.UnfurledMediaItemProperties("attachment://file.png"),
                    isSpoiler: false
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void FileWithChild()
    {
        Graph(
            """
            <file url="abc">
                <container />
            </file>
            """
        );
        {
            Node<FileComponentNode>(out var fileNode);

            Assert.IsType<CXElement>(fileNode.State.Source);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.ComponentDoesntAllowChildren.Id,
                message: "'file' doesn't allow children",
                location: CurrentGraph.GetLocation(((CXElement)fileNode.State.Source).Children)
            );
            
            EOF();
        }
    }
}