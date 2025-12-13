using Discord.CX;
using Discord.CX.Nodes;
using Microsoft.CodeAnalysis;

namespace UnitTests.RendererTests;

public sealed class BooleanTests : BaseRendererTest
{
    [Fact]
    public void BasicBoolean()
    {
        AssertRenders(
            "'true'",
            Renderers.Boolean,
            "true"
        );
        
        AssertRenders(
            "'false'",
            Renderers.Boolean,
            "false"
        );
    }

    [Fact]
    public void BooleanWithOddCasing()
    {
        AssertRenders(
            "'tRUe'",
            Renderers.Boolean,
            "true"
        );
        
        AssertRenders(
            "'FALSE'",
            Renderers.Boolean,
            "false"
        );
    }

    [Fact]
    public void UnspecifiedBooleanProperty()
    {
        AssertRenders(
            cx: null,
            Renderers.Boolean,
            "true",
            requiresValue: false
        );
    }

    [Fact]
    public void InvalidBooleanValue()
    {
        AssertRenders(
            "'blah'",
            Renderers.Boolean,
            null
        );
        {
            Diagnostic(Diagnostics.TypeMismatch(expected: "bool", actual: "string"));
            
            EOF();
        }
    }

    [Fact]
    public void InterpolatedConstant()
    {
        AssertRenders(
            "'{Interp}'",
            Renderers.Boolean,
            "false",
            interpolations:
            [
                new DesignerInterpolationInfo(
                    0,
                    new(1, 8),
                    Compilation.GetSpecialType(SpecialType.System_Boolean),
                    new(false)
                )
            ]
        );
    }

   
}