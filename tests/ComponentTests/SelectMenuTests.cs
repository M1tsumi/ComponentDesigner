using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components.SelectMenus;
using SelectMenuDefaultValue = Discord.SelectMenuDefaultValue;

namespace UnitTests.ComponentTests;

public sealed class SelectMenuTests : BaseComponentTest
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
                    type: global::Discord.ComponentType.SelectMenu,
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

    [Fact]
    public void StringSelectWithEnumerableOptions()
    {
        Graph(
            """
            <select-menu 
                type="string" 
                customId="abc"
                    
            >
                <select-menu-option label='1' value='1'/>
                {opts}
                <select-menu-option label='2' value='2'/>
            </select-menu>
            """,
            pretext:
            """
            IEnumerable<SelectMenuOptionBuilder> opts = null!;
            """
        );
        {
            Node<SelectMenuComponentNode>();
            {
                Node<SelectMenuOptionComponentNode>();
                Node<SelectMenuOptionComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.SelectMenu,
                    customId: "abc",
                    options: 
                    [
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "1",
                            value: "1"
                        ),
                        ..designer.GetValue<IEnumerable<global::Discord.SelectMenuOptionBuilder>>(0).Select(x => 
                            new global::Discord.SelectMenuOptionBuilder(x)
                        ),
                        new global::Discord.SelectMenuOptionBuilder(
                            label: "2",
                            value: "2"
                        )
                    ]
                )
                """
            );
        }
    }

    [Fact]
    public void EmptyMentionableSelectMenu()
    {
        Graph(
            """
            <select-menu type='mention' customId="abc" />
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.MentionableSelect,
                    customId: "abc"
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void BasicMentionableSelectMenu()
    {
        Graph(
            """
            <select-menu type='mention' customId="abc">
                <role>123</role>
                <User>456</User>
                <CHANNEL>789</CHANNEL>
            </select-menu>
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.MentionableSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        new global::Discord.SelectMenuDefaultValue(
                            id: 123,
                            type: global::Discord.SelectDefaultValueType.Role
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: 456,
                            type: global::Discord.SelectDefaultValueType.User
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: 789,
                            type: global::Discord.SelectDefaultValueType.Channel
                        )
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void MentionableSelectWithInterpolatedValues()
    {
        Graph(
            """
            <select-menu type='mention' customId="abc">
                {user1}
                {channel1}
                {channel2}
                {role}
                {user2}
                <user>{id1}</user>
                <role>{id2}</role>
                {opt}
            </select-menu>
            """,
            pretext:
            """
            IUser user1 = null!;
            ITextChannel channel1 = null!;
            IVoiceChannel channel2 = null!;
            IRole role = null!;
            IGuildUser user2 = null!;
            ulong id1 = 123;
            uint id2 = 456;
            SelectMenuDefaultValue opt = default!;
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.MentionableSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IUser>(0).Id,
                            type: global::Discord.SelectDefaultValueType.User
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.ITextChannel>(1).Id,
                            type: global::Discord.SelectDefaultValueType.Channel
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IVoiceChannel>(2).Id,
                            type: global::Discord.SelectDefaultValueType.Channel
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IRole>(3).Id,
                            type: global::Discord.SelectDefaultValueType.Role
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IGuildUser>(4).Id,
                            type: global::Discord.SelectDefaultValueType.User
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<ulong>(5),
                            type: global::Discord.SelectDefaultValueType.User
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<uint>(6),
                            type: global::Discord.SelectDefaultValueType.Role
                        ),
                        designer.GetValue<global::Discord.SelectMenuDefaultValue>(7)
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void BasicUserSelectMenu()
    {
        Graph(
            """
            <select-menu type='user' customId="abc">
                <user>123</user>
                {user}
            </select-menu>
            """,
            pretext:
            """
            IUser user = null;
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.UserSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        new global::Discord.SelectMenuDefaultValue(
                            id: 123,
                            type: global::Discord.SelectDefaultValueType.User
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IUser>(0).Id,
                            type: global::Discord.SelectDefaultValueType.User
                        )
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void UserSelectWithIncorrectDefaultValue()
    {
        Graph(
            """
            <select-menu type='user' customId="abc">
                <user>123</user>
                <channel>456</channel>
                <role>789</role>
                <user>101112</user>
            </select-menu>
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'Channel' is not a valid default kind for the menu 'User'"
            );
            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'Role' is not a valid default kind for the menu 'User'"
            );

            EOF();
        }
    }

    [Fact]
    public void BasicChannelSelectMenu()
    {
        Graph(
            """
            <select-menu type='channel' customId="abc">
                <channel>123</channel>
                {channel}
            </select-menu>
            """,
            pretext:
            """
            IChannel channel = null;
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.ChannelSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        new global::Discord.SelectMenuDefaultValue(
                            id: 123,
                            type: global::Discord.SelectDefaultValueType.Channel
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IChannel>(0).Id,
                            type: global::Discord.SelectDefaultValueType.Channel
                        )
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ChannelSelectWithIncorrectDefaultValue()
    {
        Graph(
            """
            <select-menu type='channel' customId="abc">
                <user>123</user>
                <channel>456</channel>
                <role>789</role>
            </select-menu>
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'User' is not a valid default kind for the menu 'Channel'"
            );
            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'Role' is not a valid default kind for the menu 'Channel'"
            );

            EOF();
        }
    }

    [Fact]
    public void BasicRoleSelectMenu()
    {
        Graph(
            """
            <select-menu type='role' customId="abc">
                <role>123</role>
                {role}
            </select-menu>
            """,
            pretext:
            """
            IRole role = null;
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.RoleSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        new global::Discord.SelectMenuDefaultValue(
                            id: 123,
                            type: global::Discord.SelectDefaultValueType.Role
                        ),
                        new global::Discord.SelectMenuDefaultValue(
                            id: designer.GetValue<global::Discord.IRole>(0).Id,
                            type: global::Discord.SelectDefaultValueType.Role
                        )
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void RoleSelectWithIncorrectDefaultValue()
    {
        Graph(
            """
            <select-menu type='ROLE' customId="abc">
                <user>123</user>
                <channel>456</channel>
                <role>789</role>
            </select-menu>
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'User' is not a valid default kind for the menu 'Role'"
            );
            Diagnostic(
                Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu.Id,
                message: "'Channel' is not a valid default kind for the menu 'Role'"
            );

            EOF();
        }
    }

    [Fact]
    public void MentionableSelectMenuWithEnumerableOptions()
    {
        Graph(
            """
            <select-menu type='mentionable' customId="abc">
                {spread1}
                {spread2}
                {spread3}
            </select-menu>
            """,
            pretext:
            """
            IEnumerable<IUser> spread1 = null!;
            IEnumerable<IChannel> spread2 = null!;
            IEnumerable<SelectMenuDefaultValue> spread3 = null!;
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.MentionableSelect,
                    customId: "abc",
                    defaultValues:
                    [
                        ..designer.GetValue<IEnumerable<global::Discord.IUser>>(0).Select(x => 
                            new global::Discord.SelectMenuDefaultValue(
                                id: x.Id,
                                type: global::Discord.SelectDefaultValueType.User
                            )    
                        ),
                        ..designer.GetValue<IEnumerable<global::Discord.IChannel>>(1).Select(x => 
                            new global::Discord.SelectMenuDefaultValue(
                                id: x.Id,
                                type: global::Discord.SelectDefaultValueType.Channel
                            )    
                        ),
                        ..designer.GetValue<IEnumerable<global::Discord.SelectMenuDefaultValue>>(2)
                    ]
                )
                """
            );

            EOF();
        }
    }

    [Fact]
    public void SelectMenuMinMaxRange()
    {
        Graph(
            """
            <select-menu 
                type='user' 
                customId="abc"
                min='0'
                max='25'
            />
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.SelectMenuBuilder(
                    type: global::Discord.ComponentType.UserSelect,
                    customId: "abc",
                    minValues: 0,
                    maxValues: 25
                )
                """
            );

            EOF();
        }

        // out of range
        Graph(
            """
            <select-menu 
                type='user' 
                customId="abc"
                min='-1'
                max='26'
            />
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange.Id,
                message: "'minValues' must be between 0 and 25"
            );

            Diagnostic(
                Diagnostics.OutOfRange.Id,
                message: "'maxValues' must be between 1 and 25"
            );

            EOF();
        }
        
        // invalid range
        Graph(
            """
            <select-menu 
                type='user' 
                customId="abc"
                min='10'
                max='5'
            />
            """
        );
        {
            Node<SelectMenuComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidRange.Id,
                message: "'minValues' must be less than or equal to 'maxValues'"
            );
            
            EOF();
        }
    }
}