using Discord.CX;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnitTests;

public static class Generation
{
    public static GeneratorDriver RunGenerator(string source)
    {
        var compilation = Compilations.Create()
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));

        var generator = new SourceGenerator();

        return CSharpGeneratorDriver
            .Create(generator)
            .RunGenerators(compilation);
    }
}