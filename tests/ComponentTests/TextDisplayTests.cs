using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class TextDisplayTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void WithTextControls()
    {
        Graph(
            """
            <text>
                with <b>bold</b> controls, and others like <u>underline</u>
            </text>
            """
        );
        {
            Node<TextDisplayComponentNode>();
            
            Validate(hasErrors: false);

            Renders(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: "with **bold** controls, and others like __underline__"
                )
                """"
            );
            
            EOF();
        }
    }
    
    [Fact]
    public void MultipartInterpolatedText()
    {
        Graph(
            """
            <text>
                {a}
                {b}
            </text>
            """,
            pretext:
            // prevent constants with random numbers
            """
            string a = Random.Shared.Next().ToString();
            string b = Random.Shared.Next().ToString();
            """
        );
        {
            Node<TextDisplayComponentNode>();
            
            Validate(hasErrors: false);

            Renders(
                """"
                new global::Discord.TextDisplayBuilder(
                    content: 
                    $"""
                     {designer.GetValueAsString(0)}
                     {designer.GetValueAsString(1)}
                     """
                )
                """"
            );
            
            EOF();
        }
    }

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
                Diagnostics.MissingRequiredProperty("text-display", "content")
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

            Assert.True(content is { IsSpecified: false, HasValue: false });
            Assert.NotNull(((TextDisplayState)textNode.State).Content);

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