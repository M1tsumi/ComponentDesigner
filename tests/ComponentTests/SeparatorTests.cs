using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components;

namespace UnitTests.ComponentTests;

public sealed class SeparatorTests : BaseComponentTest
{
    [Fact]
    public void SingleQuoteSpacing()
    {
        Graph(
            "<separator spacing='large'/>",
            quoteCount: 1,
            hasInterpolations: false
        );
        {
            Node<SeparatorComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SeparatorBuilder(
                    spacing: global::Discord.SeparatorSpacingSize.Large
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void EmptySeparator()
    {
        Graph(
            """
            <separator />
            """
        );
        {
            Node<SeparatorComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SeparatorBuilder()
                """
            );

            EOF();
        }
    }

    [Fact]
    public void SeparatorWithProperties()
    {
        Graph(
            """
            <separator
                id='123'
                divider='true'
                spacing='large'
            />
            """
        );
        {
            var separator = Node<SeparatorComponentNode>(out var separatorNode);

            var id = separatorNode.State.GetProperty(separator.Id);
            var divider = separatorNode.State.GetProperty(separator.Divider);
            var spacing = separatorNode.State.GetProperty(separator.Spacing);

            Assert.True(id is { IsSpecified: true, HasValue: true });
            Assert.True(divider is { IsSpecified: true, HasValue: true });
            Assert.True(spacing is { IsSpecified: true, HasValue: true });

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SeparatorBuilder(
                    id: 123,
                    isDivider: true,
                    spacing: global::Discord.SeparatorSpacingSize.Large
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void InvalidSpacing()
    {
        Graph(
            """
            <separator spacing="abc" />
            """
        );
        {
            var separator = Node<SeparatorComponentNode>(out var separatorNode);

            var spacing = separatorNode.State.GetProperty(separator.Spacing);

            Assert.NotNull(spacing.Value);

            Validate();

            Renders(
                """
                new global::Discord.SeparatorBuilder(
                    spacing: global::System.Enum.Parse<global::Discord.SeparatorSpacingSize>("abc")
                )
                """
            );

            Diagnostic(
                Diagnostics.InvalidEnumVariant("abc", "Discord.SeparatorSpacingSize"),
                spacing.Value
            );

            EOF();
        }
    }
}