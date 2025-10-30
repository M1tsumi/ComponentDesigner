using System.Linq;
using Microsoft.CodeAnalysis;

namespace Discord.Net.ComponentDesignerGenerator.Utils;

public static class TypeUtils
{
    public static bool TryGetEnumerableType(this ITypeSymbol? symbol, out ITypeSymbol inner)
    {
        if (symbol is not INamedTypeSymbol named)
        {
            inner = null!;
            return false;
        }
        
        if (IsEnumerableType(named) && named.TypeArguments.Length is 1)
        {
            inner = named.TypeArguments[0];
            return true;
        }

        inner = named
            .AllInterfaces
            .FirstOrDefault(IsEnumerableType)
            ?.TypeArguments
            .FirstOrDefault()!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return inner is not null;
    }

    private static bool IsEnumerableType(this INamedTypeSymbol symbol)
        => symbol.IsGenericType && symbol.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) is "IEnumerable<>";
}