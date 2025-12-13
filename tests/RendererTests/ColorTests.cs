using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;

namespace UnitTests.RendererTests;

public sealed class ColorTests : BaseRendererTest
{
    [Fact]
    public void PredefinedColors()
    {
        AssertRenders(
            "'red'",
            Renderers.Color,
            "global::Discord.Color.Red"
        );
        
        AssertRenders(
            "'blue'",
            Renderers.Color,
            "global::Discord.Color.Blue"
        );
    }

    [Fact]
    public void NotAColorRuntimeFallback()
    {
        AssertRenders(
            "'blah'",
            Renderers.Color,
            "global::Discord.Color.Parse(\"blah\")"
        );
        {
            Diagnostic(Diagnostics.FallbackToRuntimeValueParsing("Discord.Color.Parse"));
            EOF();
        }
    }

    [Fact]
    public void Hex()
    {
        AssertRenders(
            "'00FF00'",
            Renderers.Color,
            "new global::Discord.Color(65280)"
        );
        
        AssertRenders(
            "'#00FF00'",
            Renderers.Color,
            "new global::Discord.Color(65280)"
        );
    }

    [Fact]
    public void InterpolatedConstantHex()
    {
        // color has an implicit conversion from uint
        AssertRenders(
            "'{Interp}'",
            Renderers.Color,
            "designer.GetValue<global::Discord.Color>(0)",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_UInt32),
                    new(0x00FF00)
                )
            ]
        );
    }
}