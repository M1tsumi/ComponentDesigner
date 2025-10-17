using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.CX;

public readonly record struct RenderedInterceptor(
    InterceptableLocation InterceptLocation,
    Location Location,
    string CX,
    string Source,
    ImmutableArray<Diagnostic> Diagnostics,
    bool UsesDesigner
)
{
    public bool Equals(RenderedInterceptor other)
        => InterceptLocation.Data == other.InterceptLocation.Data &&
           InterceptLocation.Version == other.InterceptLocation.Version &&
           Source == other.Source &&
           Diagnostics.SequenceEqual(other.Diagnostics);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = InterceptLocation.GetHashCode();
            hashCode = (hashCode * 397) ^ Source.GetHashCode();
            hashCode = (hashCode * 397) ^ Diagnostics.Aggregate(0, (a, b) => (a * 397) ^ b.GetHashCode());
            return hashCode;
        }
    }
}
