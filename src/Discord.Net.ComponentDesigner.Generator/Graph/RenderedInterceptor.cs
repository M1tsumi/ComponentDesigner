using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using Discord.CX.Util;

namespace Discord.CX;

public sealed class RenderedInterceptor(
    SyntaxTree syntaxTree,
    InterceptableLocation interceptLocation,
    LocationInfo location,
    string cx,
    string source,
    EquatableArray<DiagnosticInfo> diagnostics,
    bool usesDesigner
) : IEquatable<RenderedInterceptor>
{
    public SyntaxTree SyntaxTree { get; } = syntaxTree;
    public InterceptableLocation InterceptLocation { get; } = interceptLocation;
    public LocationInfo Location { get; } = location;
    public string CX { get; } = cx;
    public string Source { get; } = source;
    public EquatableArray<DiagnosticInfo> Diagnostics { get; } = diagnostics;
    public bool UsesDesigner { get; } = usesDesigner;

    public override bool Equals(object? obj)
        => obj is RenderedInterceptor other && Equals(other);

    public bool Equals(RenderedInterceptor other)
        => InterceptLocation.Equals(other.InterceptLocation) &&
           Location.Equals(other.Location) &&
           CX == other.CX &&
           Source == other.Source &&
           Diagnostics.Equals(other.Diagnostics) &&
           UsesDesigner == other.UsesDesigner;

    public override int GetHashCode()
        => Hash.Combine(
            InterceptLocation,
            Location,
            CX,
            Source,
            Diagnostics,
            UsesDesigner
        );
}