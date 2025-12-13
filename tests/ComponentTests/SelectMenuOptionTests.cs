using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components.SelectMenus;

namespace UnitTests.ComponentTests;

public sealed class SelectMenuOptionTests : BaseComponentTest
{
    [Fact]
    public void EmptyOption()
    {
        Graph(
            """
            <select-menu-option />
            """
        );
        {
            Node<SelectMenuOptionComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty("select-menu-option", "label")
            );
            
            Diagnostic(
                Diagnostics.MissingRequiredProperty("select-menu-option", "value")
            );
            
            EOF();
        }
    }

    [Fact]
    public void MinimalOption()
    {
        Graph(
            """
            <select-menu-option
                label="label"
                value="value"
            />
            """
        );
        {
            Node<SelectMenuOptionComponentNode>();
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuOptionBuilder(
                    label: "label",
                    value: "value"
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void FullOption()
    {
        Graph(
            """
            <select-menu-option
                label="label"
                value="value"
                description="description"
                emoji="😀"
                default
            />
            """
        );
        {
            Node<SelectMenuOptionComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuOptionBuilder(
                    label: "label",
                    value: "value",
                    description: "description",
                    emoji: global::Discord.Emoji.Parse("😀"),
                    isDefault: true
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void LabelAndValueAndDescriptionIsTooLong()
    {
        Graph(
            $"""
             <select-menu-option
                 label="{new string('x', 200)}"
                 value='{new string('y', 200)}'
                 description='{new string('z', 200)}'
             />
             """
        );
        {
            Node<SelectMenuOptionComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("label", "at most 100 characters in length")
            );
            
            Diagnostic(
                Diagnostics.OutOfRange("value", "at most 100 characters in length")
            );
            
            Diagnostic(
                Diagnostics.OutOfRange("description", "at most 100 characters in length")
            );
            
            EOF();
        }
    }

    [Fact]
    public void LabelAsChild()
    {
        Graph(
            """
            <option value="1">
                Label Text
            </option>
            """
        );
        {
            Node<SelectMenuOptionComponentNode>();
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuOptionBuilder(
                    label: "Label Text",
                    value: "1"
                )
                """
            );
            
            EOF();
        }
    }
}