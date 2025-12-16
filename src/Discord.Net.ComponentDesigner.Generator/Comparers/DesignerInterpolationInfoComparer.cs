using System.Collections.Generic;
using Discord.CX.Util;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Comparers;

public sealed class DesignerInterpolationInfoComparer(
    bool compareLocations = true
) : IEqualityComparer<DesignerInterpolationInfo>
{
    public static readonly DesignerInterpolationInfoComparer Default = new();
    public static readonly DesignerInterpolationInfoComparer WithoutSpan = new(compareLocations: false);
    
    public bool Equals(DesignerInterpolationInfo x, DesignerInterpolationInfo y)
        => x.Id == y.Id &&
           (x.Constant.HasValue, y.Constant.HasValue) switch
           {
               (false, false) => true,
               (true, false) or (false, true) => false,
               (true, true) => (x.Constant.Value?.Equals(y.Constant.Value) ?? y.Constant.Value is null)
           } &&
           x.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == y.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) &&
           (!compareLocations || x.Span.Equals(y.Span));

    public int GetHashCode(DesignerInterpolationInfo obj)
    {
        var hash = Hash.Combine(
            obj.Id,
            obj.Constant,
            obj.Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );

        if (compareLocations) hash = Hash.Combine(hash, obj.Span);

        return hash;
    }
}