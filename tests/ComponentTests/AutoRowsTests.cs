using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.SelectMenus;

namespace UnitTests.ComponentTests;

public sealed class AutoRowsTests : BaseComponentTest
{
    [Fact]
    public void MixOfButtonsAndSelects()
    {
        Graph(
            """
            <container>
                <select-menu type='role' customId='1'/>
                <button customId="2" label="2" />
                <button customId="3" label="3" />
                <button customId="4" label="4" />
                <select-menu type='role' customId='5'/>
                <select-menu type='role' customId='6'/>
                <button customId="7" label="7" />
                <button customId="8" label="8" />
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "1"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "4",
                                    customId: "4"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "5"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "6"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "7",
                                    customId: "7"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "8",
                                    customId: "8"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void SingleSelectMenu()
    {
        Graph(
            """
            <container>
                <select-menu type='role' customId='abc'/>
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "abc"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void ManySelectMenus()
    {
        Graph(
            """
            <container>
                <select-menu type='role' customId='1'/>
                <select-menu type='user' customId='2'/>
                <select-menu type='channel' customId='3'/>
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<SelectMenuComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.RoleSelect,
                                    customId: "1"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.UserSelect,
                                    customId: "2"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.SelectMenuBuilder(
                                    type: global::Discord.ComponentType.ChannelSelect,
                                    customId: "3"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void SingleButtonInAutoRow()
    {
        Graph(
            """
            <container>
                <button 
                    customId="abc"
                    label="abc"
                />
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "abc",
                                    customId: "abc"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void MultipleButtonsInSingleRow()
    {
        Graph(
            """
            <container>
                <button 
                    customId="1"
                    label="1"
                />
                <button 
                    customId="2"
                    label="2"
                />
                <button 
                    customId="3"
                    label="3"
                />
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "1",
                                    customId: "1"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void MultipleAutoRows()
    {
        Graph(
            """
            <container>
                <button 
                    customId="1"
                    label="1"
                />
                <button 
                    customId="2"
                    label="2"
                />
                <button 
                    customId="3"
                    label="3"
                />
                <button 
                    customId="4"
                    label="4"
                />
                <button 
                    customId="5"
                    label="5"
                />
                <button 
                    customId="6"
                    label="6"
                />
                <button 
                    customId="7"
                    label="7"
                />
            </container>
            """,
            options: new(
                EnableAutoRows: true
            )
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }

                Node<AutoActionRowComponentNode>();
                {
                    Node<ButtonComponentNode>();
                    Node<ButtonComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "1",
                                    customId: "1"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "2",
                                    customId: "2"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "3",
                                    customId: "3"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "4",
                                    customId: "4"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "5",
                                    customId: "5"
                                )
                            ]
                        },
                        new global::Discord.ActionRowBuilder()
                        {
                            Components =
                            [
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "6",
                                    customId: "6"
                                ),
                                new global::Discord.ButtonBuilder(
                                    style: global::Discord.ButtonStyle.Primary,
                                    label: "7",
                                    customId: "7"
                                )
                            ]
                        }
                    ]
                }
                """
            );

            EOF();
        }
    }
}