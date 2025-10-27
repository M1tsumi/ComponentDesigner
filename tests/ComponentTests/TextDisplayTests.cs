using Discord.CX;
using Discord.CX.Nodes.Components;

namespace UnitTests.ComponentTests;

public sealed class TextDisplayTests : BaseComponentTest
{
    [Fact]
    public void EmptyTextDisplay()
    {
        Graph(
            """
            <text />
            """
        );
        {
            Node<TextDisplayComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'text-display' requires the property 'content' to be specified"
            );
            
            EOF();
        }
    }

    [Fact]
    public void TextDisplayWithContentInAttribute()
    {
        Graph(
            """
            <text content="Hello World!" />
            """
        );
        {
            var text = Node<TextDisplayComponentNode>(out var textNode);

            var content = textNode.State.GetProperty(text.Content);

            Assert.True(content is { IsSpecified: true, HasValue: true });
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.TextDisplayBuilder(
                    content: "Hello World!"
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void TextDisplayWithContentInChildren()
    {
        Graph(
            """
            <text>
                Hello, World!
            </text>
            """
        );
        {
            var text = Node<TextDisplayComponentNode>(out var textNode);

            var content = textNode.State.GetProperty(text.Content);

            Assert.True(content is { IsSpecified: true, HasValue: true });
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.TextDisplayBuilder(
                    content: "Hello, World!"
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void MultilineTextContent()
    {
        Graph(
            """
            <text>
                This content contains multiple lines:
                  - The indentation is preserved based on the shortest line
                So we can do 
                  multi
                    line
                      indentation
                
                
                and multiple breaks
            </text>
            """
        );
        {
            Node<TextDisplayComponentNode>();
            
            Validate(hasErrors: false);

            Renders(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: 
                    """
                    This content contains multiple lines:
                      - The indentation is preserved based on the shortest line
                    So we can do 
                      multi
                        line
                          indentation
                    
                    
                    and multiple breaks
                    """
                )
                """"
            );
            
            EOF();
        }
    }
}