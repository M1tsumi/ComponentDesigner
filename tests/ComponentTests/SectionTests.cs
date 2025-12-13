using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class SectionTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptySection()
    {
        Graph(
            """
            <section />
            """
        );
        {
            Node<SectionComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.EmptySection.Id
            );
            
            EOF();
        }
    }

    [Fact]
    public void SectionWithInlineAccessory()
    {
        Graph(
            """
            <section accessory=(<thumbnail url="abc" />)>
                <text>Hello</text>
            </section>
            """
        );
        {
            Node<SectionComponentNode>();
            {
                Node<ThumbnailComponentNode>();
                Node<TextDisplayComponentNode>();
            }
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties("abc")
                    ),
                    components:
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "Hello"
                        )
                    ]
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void SectionWithChildAccessory()
    {
        Graph(
            """
            <section>
                <accessory>
                    <thumbnail url="abc" />
                </accessory>
                <text>Hello</text>
            </section>
            """
        );
        {
            Node<SectionComponentNode>();
            {
                Node<AccessoryComponentNode>();
                {
                    Node<ThumbnailComponentNode>();
                }
                Node<TextDisplayComponentNode>();
            }
            
            Validate(hasErrors: false);
            
            Renders(
                """
                new global::Discord.SectionBuilder(
                    accessory: new global::Discord.ThumbnailBuilder(
                        media: new global::Discord.UnfurledMediaItemProperties("abc")
                    ),
                    components:
                    [
                        new global::Discord.TextDisplayBuilder(
                            content: "Hello"
                        )
                    ]
                )
                """
            );
            
            EOF();
        }
    }
}