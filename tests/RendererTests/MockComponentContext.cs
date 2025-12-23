using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace UnitTests.RendererTests;

public sealed class MockComponentContext : IComponentContext
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