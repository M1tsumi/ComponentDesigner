using System.Collections.Generic;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes.Components.SelectMenus;

public sealed record SelectMenuInterpolatedOption(
    CXToken Interpolation,
    int InterpolationId,
    bool IsCollection,
    bool IsBuilder
)
{
    public Result<string> Render(
        SelectMenuComponentNode.SelectState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        var source = context.GetDesignerValue(
            InterpolationId,
            context
                .GetInterpolationInfo(InterpolationId)
                .Symbol
                !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );

        switch (IsCollection, IsBuilder)
        {
            case (false, false): return source;
            case (true, false): return $"..{source}";
            case (false, true):
                return $"new {
                    context.KnownTypes
                        .SelectMenuOptionBuilderType
                        !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                }({source})";
            case (true, true):
                return
                    $"""
                     ..{source}.Select(x => 
                         new {
                             context.KnownTypes
                                 .SelectMenuOptionBuilderType
                                 !.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                         }(x)
                     )
                     """;
        }
    }

    public static bool TryCreate(
        IComponentContext context,
        ICXNode interpolation,
        IList<DiagnosticInfo> diagnostics,
        out SelectMenuInterpolatedOption option
    )
    {
        CXToken interpolationToken;
        DesignerInterpolationInfo info;

        switch (interpolation)
        {
            case CXToken { Kind: CXTokenKind.Interpolation } token:
                info = context.GetInterpolationInfo(token);
                interpolationToken = token;
                break;
            case CXValue.Interpolation value:
                info = context.GetInterpolationInfo(value);
                interpolationToken = value.Token;
                break;
            default:
                option = null!;
                return false;
        }

        var symbol = info.Symbol;

        bool isCollection;

        // ReSharper disable once AssignmentInConditionalExpression
        if (isCollection = symbol.TryGetEnumerableType(out var innerSymbol))
            symbol = innerSymbol;

        var isBuilderType = context.Compilation.HasImplicitConversion(
            symbol,
            context.KnownTypes.SelectMenuOptionBuilderType
        );

        var isComponentType = context.Compilation.HasImplicitConversion(
            symbol,
            context.KnownTypes.SelectMenuOptionType
        );

        if (!isBuilderType && !isComponentType)
        {
            diagnostics.Add(
                Diagnostics.InvalidStringSelectChild(info.Symbol!.ToDisplayString()),
                interpolation
            );
            option = null!;
            return false;
        }

        option = new(
            interpolationToken,
            info.Id,
            isCollection,
            isBuilderType
        );
        return true;
    }
}