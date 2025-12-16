using System;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed class ComponentDesignerTarget(
    InterceptableLocation interceptLocation,
    string? parentKey,
    CXDesignerGeneratorState cx
) : IEquatable<ComponentDesignerTarget>
{
    // both compilation and syntax tree is used sparingly; try not to rely on these
    public Compilation Compilation => CX.SemanticModel.Compilation;
    public SyntaxTree SyntaxTree => CX.SyntaxTree;

    public InterceptableLocation InterceptLocation { get; } = interceptLocation;
    public string? ParentKey { get; } = parentKey;
    public CXDesignerGeneratorState CX { get; } = cx;

    public override int GetHashCode()
        => Hash.Combine(
            InterceptLocation,
            ParentKey,
            CX
        );

    public bool Equals(ComponentDesignerTarget other)
        => InterceptLocation.Equals(other.InterceptLocation) &&
           ParentKey == other.ParentKey &&
           CX.Equals(other.CX);
}