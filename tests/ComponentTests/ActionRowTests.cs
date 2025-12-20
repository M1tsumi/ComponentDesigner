using Discord.CX;
using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.SelectMenus;
using Discord.CX.Parser;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class ActionRowTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyRow()
    {
        Graph(
            """
            <row />
            """
        );
        {
            Node<ActionRowComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(Diagnostics.EmptyActionRow.Id);

            EOF();
        }

        Graph(
            """
            <row>

            </row>
            """
        );
        {
            Node<ActionRowComponentNode>();

            Validate(hasErrors: true);

            Diagnostic(Diagnostics.EmptyActionRow.Id);

            EOF();
        }
    }

    [Fact]
    public void RowWithIdAndChild()
    {
        Graph(
            """
            <row id="123">
                <button url="abc" label="label"/>
            </row>
            """
        );
        {
            var row = Node<ActionRowComponentNode>(out var rowNode);
            {
                Node<ButtonComponentNode>();
            }

            var id = rowNode.State.GetProperty(row.Id);

            Assert.True(id.IsSpecified);

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ActionRowBuilder()
                {
                    Id = 123,
                    Components =
                    [
                        new global::Discord.ButtonBuilder(
                            style: global::Discord.ButtonStyle.Link,
                            label: "label",
                            url: "abc"
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void TooManyButtonsInRow()
    {
        Graph(
            """
            <row>
                <button url="url-1" label="label-1"/>
                <button url="url-2" label="label-2"/>
                <button url="url-3" label="label-3"/>
                <button url="url-4" label="label-4"/>
                <button url="url-5" label="label-5"/>
                <button url="url-6" label="label-6"/>
            </row>
            """
        );
        {
            GraphNode extraButtonGraphNode;

            Node<ActionRowComponentNode>();
            {
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>(out extraButtonGraphNode);
            }

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.ActionRowInvalidChild,
                span: extraButtonGraphNode.State.Source.Span
            );

            EOF();
        }
    }

    [Fact]
    public void MixOfSelectMenusAndButton()
    {
        Graph(
            """
            <row>
                <button url="url-1" label="label-1" />
                <select type="string" customId="abc">
                    <option value="foo">Foo</option>
                </select>
                <button url="url-2" label="label-2" />
            </row>
            """
        );
        {
            GraphNode selectMenuGraphNode;
            Node<ActionRowComponentNode>();
            {
                Node<ButtonComponentNode>();
                Node<SelectMenuComponentNode>(out selectMenuGraphNode);
                {
                    Node<SelectMenuOptionComponentNode>();
                }
                Node<ButtonComponentNode>();
            }

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.ActionRowInvalidChild,
                selectMenuGraphNode.State.Source.Span
            );

            EOF();
        }
    }

    [Fact]
    public void RowWithSelectMenu()
    {
        Graph(
            """
            <row>
                <select type="string" customId="abc">
                    <option value="1">Foo</option>
                </select>
            </row>
            """
        );
        {
            Node<ActionRowComponentNode>();
            {
                Node<SelectMenuComponentNode>();
                {
                    Node<SelectMenuOptionComponentNode>();
                }
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ActionRowBuilder()
                {
                    Components =
                    [
                        new global::Discord.SelectMenuBuilder(
                            type: global::Discord.ComponentType.SelectMenu,
                            customId: "abc",
                            options:
                            [
                                new global::Discord.SelectMenuOptionBuilder(
                                    label: "Foo",
                                    value: "1"
                                )
                            ]
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void RowWithButtons()
    {
        Graph(
            """
            <row>
                <button url="url-1" label="label-1"/>
                <button url="url-2" label="label-2"/>
                <button url="url-3" label="label-3"/>
            </row>
            """
        );
        {
            Node<ActionRowComponentNode>();
            {
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
                Node<ButtonComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ActionRowBuilder()
                {
                    Components =
                    [
                        new global::Discord.ButtonBuilder(
                            style: global::Discord.ButtonStyle.Link,
                            label: "label-1",
                            url: "url-1"
                        ),
                        new global::Discord.ButtonBuilder(
                            style: global::Discord.ButtonStyle.Link,
                            label: "label-2",
                            url: "url-2"
                        ),
                        new global::Discord.ButtonBuilder(
                            style: global::Discord.ButtonStyle.Link,
                            label: "label-3",
                            url: "url-3"
                        )
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void RowWithInvalidProperty()
    {
        Graph(
            """
            <row abc someProp="123">
                <button url="url-1" label="label-1" />
            </row>
            """
        );
        {
            Node<ActionRowComponentNode>(out var rowNode);
            {
                Node<ButtonComponentNode>();
            }

            var abcAttr = ((CXElement)rowNode.State!.Source).Attributes.First(x => x.Identifier == "abc");
            var somePropAttr =
                ((CXElement)rowNode.State.Source).Attributes.First(x => x.Identifier == "someProp");

            Validate();

            Diagnostic(
                Diagnostics.UnknownProperty(property: "abc", owner: "action-row"),
                span: abcAttr.Span
            );
            Diagnostic(
                Diagnostics.UnknownProperty(property: "someProp", owner: "action-row"),
                span: somePropAttr.Span
            );

            EOF();
        }
    }

    [Fact]
    public void RowWithInvalidChild()
    {
        Graph(
            """
            <row>
                <button url="url-1" label="label-1" />
                <container />
            </row>
            """
        );
        {
            GraphNode containerGraphNode;
            Node<ActionRowComponentNode>();
            {
                Node<ButtonComponentNode>();
                Node<ContainerComponentNode>(out containerGraphNode);
            }

            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.ActionRowInvalidChild,
                span: containerGraphNode.State.Source.Span
            );
            
            EOF();
        }
    }
}