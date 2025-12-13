using Discord;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class TextInputTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void EmptyInput()
    {
        Graph(
            """
            <text-input />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);
            
            Diagnostic(
                Diagnostics.MissingRequiredProperty("text-input", "customId")
            );
            
            EOF();
        }
    }

    [Fact]
    public void MinimalInput()
    {
        Graph(
            """
            <text-input customId="abc"/>
            """
        );
        {
            var input = Node<TextInputComponentNode>(out var inputNode);

            var customId = inputNode.State.GetProperty(input.CustomId);
            
            Assert.True(customId is {HasValue: true, IsSpecified: true});
            
            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.TextInputBuilder(
                    customId: "abc"
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void FullInput()
    {
        Graph(
            """
            <text-input
                id='123'
                customId="abc"
                style="paragraph"
                min={1}
                max={32}
                required
                value="value"
                placeholder="placeholder"
            />
            """
        );
        {
            var input = Node<TextInputComponentNode>(out var inputNode);
            
            var id = inputNode.State.GetProperty(input.Id);
            var customId = inputNode.State.GetProperty(input.CustomId);
            var style = inputNode.State.GetProperty(input.Style);
            var min = inputNode.State.GetProperty(input.MinLength);
            var max = inputNode.State.GetProperty(input.MaxLength);
            var required = inputNode.State.GetProperty(input.Required);
            var placeholder = inputNode.State.GetProperty(input.Placeholder);
            
            Assert.True(customId is {HasValue: true, IsSpecified: true});
            Assert.True(style is {HasValue: true, IsSpecified: true});
            Assert.True(min is {HasValue: true, IsSpecified: true});
            Assert.True(max is {HasValue: true, IsSpecified: true});
            Assert.True(required is {HasValue: false, IsSpecified: true});
            Assert.True(placeholder is {HasValue: true, IsSpecified: true});
            
            Validate(hasErrors: false);
            
            Renders(
                """
                new global::Discord.TextInputBuilder(
                    id: 123,
                    customId: "abc",
                    style: global::Discord.TextInputStyle.Paragraph,
                    minLength: 1,
                    maxLength: 32,
                    required: true,
                    value: "value",
                    placeholder: "placeholder"
                )
                """
            );
            
            EOF();
        }
    }

    [Fact]
    public void MinMaxLengthInvalid()
    {
        Graph(
            """
            <text-input 
                customId="abc"
                min='5'
                max='4'
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.InvalidRange("minLength", "maxLength")
            );
            
            EOF();
        }
    }

    [Fact]
    public void MinOutOfRange()
    {
        Graph(
            """
            <text-input
                customId="abc"
                min='-1'
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("minLength", "between 0 and 4000")
            );
            
            EOF();
        }
        
        Graph(
            """
            <text-input
                customId="abc"
                min='4001'
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("minLength", "between 0 and 4000")
            );
            
            EOF();
        }
    }

    [Fact]
    public void MaxOutOfRange()
    {
        Graph(
            """
            <text-input
                customId="abc"
                max='0'
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("maxLength", "between 1 and 4000")
            );
            
            EOF();
        }
        
        Graph(
            """
            <text-input
                customId="abc"
                max='4002'
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("maxLength", "between 1 and 4000")
            );
            
            EOF();
        }
    }

    [Fact]
    public void PlaceholderTooLong()
    {
        Graph(
            """
            <text-input
                customId="abc"
                placeholder="This placeholder is too long, the max placeholder length is 100 characters so this should report a diagnostic"
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("placeholder", "at most 100 characters in length")
            );
            
            EOF();
        }
    }
    
    [Fact]
    public void ValueTooLong()
    {
        var longValue = new string('a', 4001);
        
        Graph(
            $"""
            <text-input
                customId="abc"
                value="{longValue}"
            />
            """
        );
        {
            Node<TextInputComponentNode>();
            
            Validate(hasErrors: true);

            Diagnostic(
                Diagnostics.OutOfRange("value", "at most 4000 characters in length")
            );
            
            EOF();
        }
    }
}