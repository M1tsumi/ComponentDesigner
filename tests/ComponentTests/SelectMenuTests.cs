using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components.SelectMenus;
using SelectMenuDefaultValue = Discord.SelectMenuDefaultValue;

namespace UnitTests.ComponentTests;

public sealed class SelectMenuTests: BaseComponentTest
{
    [Fact]
    public void EmptyMenu()
    {
        Graph(
            """
            <select-menu />
            """
        );
        {
            Node<SelectMenuComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'select-menu' requires the property 'customId' to be specified"
            );
            
            Diagnostic(
                Diagnostics.MissingSelectMenuType.Id
            );
            
            EOF();
        }
    }

    [Fact]
    public void EmptyStringSelectMenu()
    {
        Graph(
            """
            <select-menu type="string" />
            """
        );
        {
            Node<SelectMenuComponentNode>(out var selectNode);

            Assert.IsType<SelectMenuComponentNode.SelectState>(selectNode.State);
            Assert.Equal(SelectKind.String, ((SelectMenuComponentNode.SelectState)selectNode.State).Kind);
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.MissingRequiredProperty.Id,
                message: "'select-menu' requires the property 'customId' to be specified"
            );
            
            Diagnostic(Diagnostics.EmptyStringSelectMenu.Id);
            
            EOF();
        }
    }

    [Fact]
    public void StringSelectWithoutOptions()
    {
        Graph(
            """
            <select-menu type="string" customId="abc"/>
            """
        );
        {
            Node<SelectMenuComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.EmptyStringSelectMenu.Id
            );
            
            EOF();
        }
    }

    [Fact]
    public void StringSelectWithOptions()
    {
        Graph(
            """
            <select-menu type='string' customId="abc">
                <select-menu-option
                    label="label1"
                    value="value1"
                    description="description1"
                    emoji="😀"
                    default
                />
            </select-menu>
            """
        );
        {
            Node<SelectMenuComponentNode>();
            {
                Node<SelectMenuOptionComponentNode>();
            }
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.StringSelect,
                    customId: "abc",
                    options: 
                    [
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "label1",
                            value: "value1",
                            description: "description1",
                            emoji: global::Discord.Emoji.Parse("😀"),
                            isDefault: true
                        )
                    ]
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void TooManyStringOptions()
    {
        var optionsCount = Constants.STRING_SELECT_MAX_VALUES + 1;
        
        var options = Enumerable
            .Range(0, optionsCount)
            .Select(x =>
                $"    <select-menu-option label=\"label{x}\" value=\"value{x}\"/>"
            );

        Graph(
            $"""
             <select-menu type='string' customId="abc">
             {string.Join(Environment.NewLine, options)}
             </select-menu>
             """
        );
        {
            Node<SelectMenuComponentNode>();
            {
                for (var i = 0; i < optionsCount; i++)
                    Node<SelectMenuOptionComponentNode>();
            }
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.TooManyStringSelectMenuChildren.Id
            );
            
            EOF();
        }
    }
}