using System.Diagnostics.CodeAnalysis;
using Discord.CX;
using Discord.CX.Nodes.Components;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public sealed class TextControlRenderingTests(ITestOutputHelper output) : BaseTestWithDiagnostics(output)
{
    [Fact]
    public void EscapesPreserved()
    {
        Renders(
            "Some <br/> text&copy; and &pi;",
            $"Some {Environment.NewLine} text© and π"
        );
    }
    
    [Fact]
    public void LineBreak()
    {
        Renders(
            "this is<br/>on a newline",
            """
            this is
            on a newline
            """
        );
    }
    
    [Fact]
    public void Spoiler()
    {
        Renders("<spoiler>foo</spoiler>", "||foo||");
    }

    [Fact]
    public void MultilineSpoiler()
    {
        Renders(
            """
            <spoiler>This spoiler
            is multi line</spoiler>
            """,
            """
            ||This spoiler
            is multi line||
            """
        );
    }

    [Fact]
    public void SyntaxIndentedSpoiler()
    {
        Renders(
            """
            <spoiler>
                spoiler
            </spoiler>
            """,
            """
            ||spoiler||
            """
        );
    }

    [Fact]
    public void Quote()
    {
        Renders("<q>quote</q>", "> quote");
    }

    [Fact]
    public void MultilineQuote()
    {
        Renders(
            """
            <q>This quote
            is multi line</q>
            """,
            """
            > This quote
            > is multi line
            """
        );
    }

    [Fact]
    public void SyntaxIndentedQuote()
    {
        Renders(
            """
            <q>
                quote
            </q>
            """,
            """
            > quote
            """
        );
    }

    [Fact]
    public void CodeblockLanguage()
    {
        Renders(
            """
            <c lang="csharp">
                int x = 1;
            </c>
            """,
            """
            ```csharp
            int x = 1;
            ```
            """
        );
    }

    [Fact]
    public void CodeblockInference()
    {
        Renders("<c>inline</c>", "`inline`");
        Renders(
            """
            <c>
                block
            </c>
            """,
            """
            ```
            block
            ```
            """
        );
    }

    [Fact]
    public void InlineCodeBlock()
    {
        Renders(
            "<c>inline</c>",
            "`inline`"
        );
    }

    [Fact]
    public void MultilineInlineCodeblock()
    {
        Renders(
            """
            <c inline>
                multiline
                inline
                codeblock
            </c>
            """,
            """
            `multiline
            inline
            codeblock`
            """
        );
    }

    [Fact]
    public void SyntaxIndentedInlineCodeblock()
    {
        Renders(
            """
            <c inline>
                indented codeblock
            </c>
            """,
            "`indented codeblock`"
        );
    }

    [Fact]
    public void UnorderedList()
    {
        Renders(
            """
            <ul>
                <li>First item</li>
                <li>
                    Second item
                </li>
            </ul>
            """,
            """
            - First item
            - Second item
            """
        );
    }

    [Fact]
    public void OrderedList()
    {
        Renders(
            """
            <ol>
                <li>First item</li>
                <li>
                    Second item
                </li>
            </ol>
            """,
            """
              1. First item
              2. Second item
            """
        );
    }

    [Fact]
    public void OrderedListProperPrefixIndentation()
    {
        Renders(
            """
            <ol>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
                <li>item</li>
            </ol>
            """,
            """
              1. item
              2. item
              3. item
              4. item
              5. item
              6. item
              7. item
              8. item
              9. item
             10. item
             11. item
            """
        );
    }

    [Fact]
    public void Link()
    {
        Renders(
            "<a href=\"example.com\">Click me</a>",
            "[Click me](example.com)"
        );
    }

    [Fact]
    public void SyntaxIndentedLink()
    {
        Renders(
            """
            <a href="https://example.com">
                click me
            </a>
            """,
            "[click me](https://example.com)"
        );
    }

    [Fact]
    public void LinkWithNewlines()
    {
        Renders(
            """
            <a href="https://example.com">
                click me
                with multiple
                lines
            </a>
            """,
            """
            [click me
            with multiple
            lines](https://example.com)
            """
        );
    }

    [Fact]
    public void H1Header()
    {
        Renders(
            "<h1>This is a header</h1>",
            "# This is a header"
        );
    }

    [Fact]
    public void H2Header()
    {
        Renders(
            "<h2>This is a header</h2>",
            "## This is a header"
        );
    }

    [Fact]
    public void H3Header()
    {
        Renders(
            "<h3>This is a header</h3>",
            "### This is a header"
        );
    }

    [Fact]
    public void HeaderWithSyntaxIndentation()
    {
        Renders(
            """
            <h1>
                This is a header
            </h1>
            """,
            "# This is a header"
        );
    }

    [Fact]
    public void MultilineHeader()
    {
        Renders(
            """
            <h1>
                This is a header
                across multiple
                lines
            </h1>
            """,
            "# This is a header across multiple lines"
        );
    }

    [Fact]
    public void SubtextWithSyntaxIndentation()
    {
        Renders(
            """
            <sub>
                Hello
            </sub>
            world
            """,
            """
            -# Hello
            world
            """
        );
    }

    [Fact]
    public void MultilineSubtext()
    {
        Renders(
            """
            <sub>
                This text spans
                across multiple
                lines
            </sub>
            and this doesn't
            """,
            """
            -# This text spans across multiple lines
            and this doesn't
            """
        );
    }

    [Fact]
    public void Subtext()
    {
        Renders(
            "<sub>Hello</sub> world",
            """
            -# Hello
            world
            """
        );
    }

    [Fact]
    public void UnderlineWithSyntaxIndentation()
    {
        Renders(
            """
            <mark>
                Hello
            </mark>
            world
            """,
            """
            __Hello__
            world
            """
        );
    }

    [Fact]
    public void MultilineUnderline()
    {
        Renders(
            """
            <mark>
                This text spans
                across multiple
                lines
            </mark>
            and this doesn't
            """,
            """
            __This text spans
            across multiple
            lines__
            and this doesn't
            """
        );
    }

    [Fact]
    public void Underline()
    {
        Renders(
            "<mark>Hello</mark> world",
            "__Hello__ world"
        );
    }

    [Fact]
    public void StrikethroughWithSyntaxIndentation()
    {
        Renders(
            """
            <del>
                Hello
            </del>
            world
            """,
            """
            ~~Hello~~
            world
            """
        );
    }

    [Fact]
    public void MultilineStrikethrough()
    {
        Renders(
            """
            <del>
                This text spans
                across multiple
                lines
            </del>
            and this doesn't
            """,
            """
            ~~This text spans
            across multiple
            lines~~
            and this doesn't
            """
        );
    }

    [Fact]
    public void Strikethrough()
    {
        Renders(
            "<del>Hello</del> world",
            "~~Hello~~ world"
        );
    }

    [Fact]
    public void BoldWithSyntaxIndentation()
    {
        Renders(
            """
            <b>
                Hello
            </b>
            world
            """,
            """
            **Hello**
            world
            """
        );
    }

    [Fact]
    public void MultilineBold()
    {
        Renders(
            """
            <b>
                This text spans
                across multiple
                lines
            </b>
            and this doesn't
            """,
            """
            **This text spans
            across multiple
            lines**
            and this doesn't
            """
        );
    }

    [Fact]
    public void Bold()
    {
        Renders(
            "<b>Hello</b> world",
            "**Hello** world"
        );
    }

    [Fact]
    public void ItalicWithSyntaxIndentation()
    {
        Renders(
            """
            <i>
                Hello
            </i>
            world
            """,
            """
            _Hello_
            world
            """
        );
    }

    [Fact]
    public void MultilineItalic()
    {
        Renders(
            """
            <i>
                This text spans
                across multiple
                lines
            </i>
            and this doesn't
            """,
            """
            _This text spans
            across multiple
            lines_
            and this doesn't
            """
        );
    }

    [Fact]
    public void Italic()
    {
        Renders(
            "<i>Hello</i> world",
            "_Hello_ world"
        );
    }

    private void Renders(
        [StringSyntax("html")] string cx,
        string? expected,
        DesignerInterpolationInfo[]? interpolations = null,
        int? wrappingQuoteCount = null,
        bool allowFail = false
    )
    {
        var parser = new CXParser(
            CXSourceText.From(cx).CreateReader(
                interpolations?.Select(x => x.Span).ToArray(),
                wrappingQuoteCount
            )
        );

        var parsed = parser.ParseElementChildren();

        PushDiagnostics(
            parsed.AllDiagnostics.Select(x => new DiagnosticInfo(
                Diagnostics.CreateParsingDiagnostic(x),
                x.Span
            ))
        );

        var diagnostics = new List<DiagnosticInfo>();
        TextControlElement? element = null;

        var context = new MockComponentContext(
            Compilations.Create(),
            new CXDesignerGeneratorState(
                cx ?? string.Empty,
                default,
                wrappingQuoteCount ?? 1,
                true,
                [..interpolations ?? []],
                null!,
                null!
            ),
            GeneratorOptions.Default
        );
        
        if (
            !TextControlElement.TryCreate(
                context,
                parsed,
                diagnostics,
                out element,
                out var nodesUsed
            )
        )
        {
            if (!allowFail) Assert.Fail("Failed to create text control elements");
        }

        Assert.Equal(parsed.Count, nodesUsed);

        if (element is not null)
        {
            element.Validate(context, diagnostics);

            PushDiagnostics(diagnostics);

            var result = element.Render(context);

            PushDiagnostics(result.Diagnostics);

            if (expected is not null)
            {
                Assert.True(result.HasResult);
                Assert.Equal(expected, result.Value);
            }
        }
        else
        {
            PushDiagnostics(diagnostics);
        }
    }
}