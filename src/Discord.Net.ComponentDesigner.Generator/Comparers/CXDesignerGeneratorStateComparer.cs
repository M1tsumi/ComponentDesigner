using System.Collections.Generic;
using System.Linq;
using Discord.CX.Util;

namespace Discord.CX.Comparers;

public sealed class CXDesignerGeneratorStateComparer(
    bool compareLocations = true
) : IEqualityComparer<CXDesignerGeneratorState>
{
    public static readonly CXDesignerGeneratorStateComparer Default = new();
    public static readonly CXDesignerGeneratorStateComparer WithoutSpan = new(compareLocations: false);
    
    private IEqualityComparer<DesignerInterpolationInfo> InterpolationComparer
        => compareLocations ? DesignerInterpolationInfoComparer.Default : DesignerInterpolationInfoComparer.WithoutSpan;

    public bool Equals(CXDesignerGeneratorState x, CXDesignerGeneratorState y)
    {
        if (ReferenceEquals(x, y)) return true;

        if (compareLocations && !x.Location.Equals(y.Location)) return false;

        return
            x.UsesDesignerParameter == y.UsesDesignerParameter &&
            x.InterpolationInfos.SequenceEqual(y.InterpolationInfos, InterpolationComparer) &&
            x.Designer == y.Designer &&
            x.QuoteCount == y.QuoteCount;
    }

    public int GetHashCode(CXDesignerGeneratorState obj)
    {
        var hash = Hash.Combine(
            obj.QuoteCount,
            obj.UsesDesignerParameter,
            obj.Designer,
            obj.InterpolationInfos.Aggregate(0, (a, b) => Hash.Combine(a, InterpolationComparer.GetHashCode(b)))
        );

        if (compareLocations) hash = Hash.Combine(hash, obj.Location);

        return hash;
    }
}