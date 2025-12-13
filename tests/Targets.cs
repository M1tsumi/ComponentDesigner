using System.Reflection;
using Discord.CX;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnitTests;

public static class Targets
{
    public static ComponentDesignerTarget FromCX(string cx)
        => FromSource(CXToCSharp(cx));

    public static string CXToCSharp(string cx)
        => $""""
            using Discord;

            ComponentDesigner.cx(
                $"""
                 {cx.WithNewlinePadding(5)}
                 """
            );
            """";

    public static ComponentDesignerTarget FromResource(string name)
        => FromSource(GetResourceFileContent(name));

    public static ComponentDesignerTarget FromSource(string source, CancellationToken token = default)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        var comp = Compilations
            .Create()
            .AddSyntaxTrees(tree);

        return FromSource(comp, tree, token);
    }

    public static ComponentDesignerTarget FromSource(Compilation compilation, SyntaxTree tree, CancellationToken token = default)
    {
        var invocation = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(x =>
                x.Expression is IdentifierNameSyntax {Identifier.Value: "cx"}
                    or MemberAccessExpressionSyntax {Name.Identifier.ValueText: "cx"}
            );

        Assert.NotNull(invocation);

        var semanticModel = compilation.GetSemanticModel(tree);

        var target = SourceGenerator.MapPossibleComponentDesignerCall(
            semanticModel,
            invocation,
            token
        );

        Assert.NotNull(target);

        return target;
    }

    private static string GetResourceFileContent(string name)
    {
        var resourceName = $"Generator.Tests.TestFiles.{name}";

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

        Assert.NotNull(stream);

        return new StreamReader(stream).ReadToEnd();
    }
}