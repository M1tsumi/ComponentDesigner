using System;
using Discord.CX.Parser;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

[Flags]
public enum InterleavedKind
{
    None = 0,

    MessageComponentBuilder = 1,
    MessageComponent = 2,
    CXMessageComponent = 3,

    CollectionOf = 1 << 2,

    ComponentMask = MessageComponentBuilder | MessageComponent | CXMessageComponent,
    
    CollectionOfBuilders = MessageComponentBuilder | CollectionOf,
    CollectionOfComponents = MessageComponent | CollectionOf,
    CollectionOfCXComponents = CXMessageComponent | CollectionOf,
}

public sealed class InterleavedComponentNode : ComponentNode
{
    public InterleavedKind Kind { get; }
    public ITypeSymbol Symbol { get; }

    public bool IsSingleCardinality
        => Kind == InterleavedKind.MessageComponentBuilder;

    public override string Name => "<interpolated component>";

    public InterleavedComponentNode(
        InterleavedKind kind,
        ITypeSymbol symbol
    )
    {
        Kind = kind;
        Symbol = symbol;
    }

    public static bool IsValidInterleavedType(
        ITypeSymbol? symbol,
        Compilation compilation
    ) => IsValidInterleavedType(symbol, compilation, out _);

    public static bool IsValidInterleavedType(
        ITypeSymbol? symbol,
        Compilation compilation,
        out InterleavedKind kind
    )
    {
        kind = InterleavedKind.None;

        if (symbol is null) return false;

        var current = symbol;

        var enumerableType = current
            .AllInterfaces
            .FirstOrDefault(x =>
                x.IsGenericType &&
                x.ConstructedFrom.Equals(
                    compilation.GetKnownTypes().IEnumerableOfTType!,
                    SymbolEqualityComparer.Default
                )
            );

        if (enumerableType is not null)
        {
            kind |= InterleavedKind.CollectionOf;
            current = enumerableType.TypeArguments[0];
        }

        if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().CXMessageComponentType
            )
        )
        {
            kind |= InterleavedKind.CXMessageComponent;
        }
        else if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().IMessageComponentBuilderType
            )
        )
        {
            kind |= InterleavedKind.MessageComponentBuilder;
        }
        else if (
            compilation.HasImplicitConversion(
                current,
                compilation.GetKnownTypes().MessageComponentType
            )
        )
        {
            kind |= InterleavedKind.MessageComponent;
        }

        return (kind & InterleavedKind.ComponentMask) is not 0;
    }

    public static bool TryCreate(
        ITypeSymbol? symbol,
        Compilation compilation,
        out InterleavedComponentNode node
    )
    {
        if (IsValidInterleavedType(symbol, compilation, out var kind))
        {
            node = new(kind, symbol!);
            return true;
        }

        node = null!;
        return false;
    }

    public static string ExtrapolateKindToBuilders(InterleavedKind kind, string source)
    {
        switch (kind)
        {
            // case 1: standard builder, we do nothing to the source
            case InterleavedKind.MessageComponentBuilder: return source;
            
            case InterleavedKind.CollectionOfBuilders: return $"..{source}";
            
            case InterleavedKind.CXMessageComponent:
            case InterleavedKind.MessageComponent: return $"..({source}).Components.Select(x => x.ToBuilder())";
            
            case InterleavedKind.CollectionOfCXComponents:
            case InterleavedKind.CollectionOfComponents:
                return $"..({source}).SelectMany(x => x.Components.Select(x => x.ToBuilder()))";
            
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
    
    public override ComponentState? Create(ICXNode source, List<CXNode> children)
    {
        if (source is not CXValue.Interpolation interpolation) return null;

        return base.Create(source, children);
    }


    // TODO: extrapolate the kind to correct buidler conversion
    public override string Render(ComponentState state, ComponentContext context)
        => context.GetDesignerValue(
            (CXValue.Interpolation)state.Source,
            context.KnownTypes.IMessageComponentBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
}