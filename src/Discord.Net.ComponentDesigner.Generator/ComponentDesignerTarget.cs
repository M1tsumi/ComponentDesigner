using System;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX;

public sealed class ComponentDesignerTarget(
    Compilation compilation,
    SyntaxTree syntaxTree,
    InterceptableLocation interceptLocation,
    string? parentKey,
    string cxDesigner,
    LocationInfo cxDesignerLocation,
    EquatableArray<DesignerInterpolationInfo> interpolations,
    bool usesDesigner,
    int cxQuoteCount
) : IEquatable<ComponentDesignerTarget>
{
    // both compilation and syntax tree is used sparingly; try not to rely on these
    public Compilation Compilation { get; } = compilation;
    public SyntaxTree SyntaxTree { get; } = syntaxTree;

    public InterceptableLocation InterceptLocation { get; } = interceptLocation;
    public string? ParentKey { get; } = parentKey;
    public string CXDesigner { get; } = cxDesigner;
    public LocationInfo CXDesignerLocation { get; } = cxDesignerLocation;
    public EquatableArray<DesignerInterpolationInfo> Interpolations { get; } = interpolations;
    public bool UsesDesigner { get; } = usesDesigner;
    public int CXQuoteCount { get; } = cxQuoteCount;

    public TextSpan CXDesignerSpan => CXDesignerLocation.TextSpan;

    public override int GetHashCode()
        => Hash.Combine(
            InterceptLocation,
            ParentKey,
            CXDesigner,
            CXDesignerLocation,
            Interpolations,
            UsesDesigner,
            CXQuoteCount
        );

    public bool Equals(ComponentDesignerTarget other)
        => InterceptLocation.Equals(other.InterceptLocation) &&
           ParentKey == other.ParentKey &&
           CXDesigner == other.CXDesigner &&
           CXDesignerLocation.Equals(other.CXDesignerLocation) &&
           Interpolations.Equals(other.Interpolations) &&
           UsesDesigner == other.UsesDesigner &&
           CXQuoteCount == other.CXQuoteCount;
}