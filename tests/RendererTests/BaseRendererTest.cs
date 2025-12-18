using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit.Abstractions;

namespace UnitTests.RendererTests;

public abstract class BaseRendererTest(ITestOutputHelper output) : BaseTestWithDiagnostics(output)
{
    protected Compilation Compilation => _compilation ??= Compilations.Create();

    private Compilation? _compilation;

    protected enum ParseMode
    {
        AttributeValue,
        ElementValue
    }


    protected void AssertRenders(
        string? cx,
        PropertyRenderer renderer,
        string? expected,
        ParseMode mode = ParseMode.AttributeValue,
        DesignerInterpolationInfo[]? interpolations = null,
        bool isAttributeValue = false,
        bool requiresValue = true,
        bool isOptional = false,
        string? propertyName = null,
        int? wrappingQuoteCount = null,
        PropertyRenderingOptions? options = null
    )
    {
        AssertEmptyDiagnostics();

        ClearDiagnostics();

        CXValue? value = null;
        CXParser? parser = null;

        if (cx is not null)
        {
            var source = CXSourceText.From(cx);

            var interpolationSpans = interpolations?.Select(x => x.Span).ToArray();

            parser = new CXParser(source.CreateReader(
                wrappingQuoteCount: wrappingQuoteCount,
                interpolations: interpolationSpans
            ));

            switch (mode)
            {
                case ParseMode.AttributeValue:
                    value = parser.ParseAttributeValue();
                    break;
                case ParseMode.ElementValue:
                    if (!parser.TryParseElementChild([], out var child))
                        Assert.Fail("Can't parse as element value");

                    Assert.IsAssignableFrom<CXValue>(child);

                    value = (CXValue)child;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var document = new CXDoc(
                parser,
                [value]
            );
        }


        var context = new MockComponentContext(
            Compilation,
            new CXDesignerGeneratorState(
                cx ?? string.Empty,
                default,
                wrappingQuoteCount ?? 1,
                true,
                [..interpolations ?? []],
                null!,
                null!
            )
        );

        var propValue = new MockPropertyValue(
            value,
            null,
            value is not null,
            value is not null,
            isAttributeValue,
            requiresValue,
            isOptional,
            propertyName ?? "test-property",
            value?.Span ?? default
        );

        var result = renderer(context, propValue, options ?? PropertyRenderingOptions.Default);

        PushDiagnostics(result.Diagnostics);

        if (expected is not null)
        {
            Assert.True(result.HasResult);
            Assert.Equal(expected, result.Value);
        }
    }

    private sealed record MockPropertyValue(
        CXValue? Value,
        GraphNode? Node,
        bool IsSpecified,
        bool HasValue,
        bool IsAttributeValue,
        bool RequiresValue,
        bool IsOptional,
        string PropertyName,
        TextSpan Span
    ) : IComponentPropertyValue;

    private sealed class MockComponentContext : IComponentContext
    {
        private readonly Dictionary<string, int> _varsCount = [];

        public KnownTypes KnownTypes => Compilation.GetKnownTypes();
        public Compilation Compilation { get; }
        public CXDesignerGeneratorState CX { get; }

        public ComponentTypingContext RootTypingContext => ComponentTypingContext.Default;

        public string GetVariableName(string? hint = null)
        {
            hint ??= "local_";

            if (!_varsCount.TryGetValue(hint, out var count))
                _varsCount[hint] = 1;
            else
                _varsCount[hint] = count + 1;

            return $"{hint}{count}";
        }

        public MockComponentContext(
            Compilation compilation,
            CXDesignerGeneratorState cx
        )
        {
            Compilation = compilation;
            CX = cx;
        }


        public string GetDesignerValue(int index, string? type = null)
            => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";


        public DesignerInterpolationInfo GetInterpolationInfo(int index)
            => CX.InterpolationInfos[index];

        public DesignerInterpolationInfo GetInterpolationInfo(CXToken token)
            => GetInterpolationInfo(token.InterpolationIndex!.Value);
    }
}