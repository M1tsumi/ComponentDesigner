using Discord.CX;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace UnitTests.GeneratorTests;

public sealed class ExternalIncrementalTests(ITestOutputHelper output) : BaseGeneratorTest(output)
{
    [Fact]
    public void GraphDoesntReRender()
    {
        var a = RunCX(
            "<text>Foo</text>"
        );

        var b = RunCX(
            "<text>Foo</text>",
            additionalMethods:
            "public void Foo(){}"
        );

        AssertStepResult(b, TrackingNames.CREATE_GRAPH, IncrementalStepRunReason.Cached);
        AssertStepResult(b, TrackingNames.RENDER_GRAPH, IncrementalStepRunReason.Cached);

        AssertRenders(
            b,
            """
            new global::Discord.TextDisplayBuilder(
                content: "Foo"
            )
            """
        );
        
        LogRunVisual(b);
    }

    [Fact]
    public void FunctionalComponentDependencyUpdates()
    {
        var a = RunCX(
            "<MyFunc arg=\"foo\" />",
            additionalMethods:
            "public static CXMessageComponent MyFunc(string arg) => CXMessageComponent.Empty;"
        );

        var b = RunCX(
            "<MyFunc arg=\"foo\" />",
            additionalMethods:
            "public static CXMessageComponent MyFunc(int arg) => CXMessageComponent.Empty;"
        );

        // the graph should be cached, while the update graph state should be modified
        AssertStepResult(b, TrackingNames.CREATE_GRAPH, IncrementalStepRunReason.Cached);
        AssertStepResult(b, TrackingNames.UPDATE_GRAPH_STATE, IncrementalStepRunReason.Modified);
        

        var render1 = GetStepValue<RenderedGraph>(a, TrackingNames.RENDER_GRAPH);
        {
            Assert.Equal(
                """
                ..global::TestClass.MyFunc(
                    arg: "foo"
                ).Builders
                """,
                render1.EmittedSource
            );
            Assert.Empty(render1.Diagnostics);
        }
        var render2 = GetStepValue<RenderedGraph>(b, TrackingNames.RENDER_GRAPH);
        {
            // second run should error with a type mismatch
            Assert.NotEmpty(render2.Diagnostics);
            Assert.Collection(
                render2.Diagnostics,
                x => AssertDiagnostic(
                    x,
                    Diagnostics.FallbackToRuntimeValueParsing("int.Parse")
                )
            );
        }
        
        LogRunVisual(b);
    }
}