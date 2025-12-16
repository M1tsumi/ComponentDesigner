using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Custom;

public sealed class ComponentChildrenAdapter
{
    public delegate Result<string> Renderer(
        IComponentContext context,
        ComponentState state,
        EquatableArray<ComponentChild> children
    );

    public Renderer ChildrenRenderer { get; }

    public bool IsOptional { get; }

    public bool IsCollectionType
        => _collectionInnerTypeSymbol is not null;

    public bool IsCX => _componentBuilderKind is not ComponentBuilderKind.None;

    private readonly ITypeSymbol _targetSymbol;
    private readonly ITypeSymbol? _collectionInnerTypeSymbol;
    private readonly ComponentBuilderKind _componentBuilderKind;
    private readonly ComponentNode _owner;

    public abstract record ComponentChild(ICXNode Node)
    {
        // any text literal
        public sealed record Text(CXToken Token) : ComponentChild(Token);

        // an interpolation
        public sealed record Interpolation(CXToken Token) : ComponentChild(Token);

        // another element
        public sealed record Element(CXElement CX) : ComponentChild(CX);
    }

    public ComponentChildrenAdapter(
        ITypeSymbol targetSymbol,
        ITypeSymbol? collectionInnerTypeSymbol,
        ComponentBuilderKind componentBuilderKind,
        bool isOptional,
        ComponentNode owner
    )
    {
        IsOptional = isOptional;
        _targetSymbol = targetSymbol;
        _collectionInnerTypeSymbol = collectionInnerTypeSymbol;
        _componentBuilderKind = componentBuilderKind;
        _owner = owner;

        ChildrenRenderer = (componentBuilderKind & ComponentBuilderKind.ComponentMask) is not ComponentBuilderKind.None
            ? RenderComponent
            : RenderNonComponent;
    }

    public static ComponentChildrenAdapter Create(
        Compilation compilation,
        ITypeSymbol target,
        bool isOptional,
        ComponentNode owner
    )
    {
        var inner = target.SpecialType is SpecialType.System_String
            ? null
            : target
                .AllInterfaces
                .FirstOrDefault(x =>
                    x.IsGenericType &&
                    x.ConstructedFrom.Equals(
                        compilation.GetKnownTypes().IEnumerableOfTType,
                        SymbolEqualityComparer.Default
                    )
                )
                ?.TypeArguments[0];

        ComponentBuilderKindUtils.IsValidComponentBuilderType(inner ?? target, compilation, out var kind);

        return new(
            target,
            inner,
            kind,
            isOptional,
            owner
        );
    }

    public EquatableArray<ComponentChild> AdaptToState(ComponentStateInitializationContext context)
    {
        if (context.CXNode is not CXElement element) return [];

        var children = new List<ComponentChild>();

        foreach (var child in element.Children)
        {
            switch (child)
            {
                case CXValue.Multipart multipart and not CXValue.StringLiteral:
                    foreach (var part in multipart.Tokens)
                    {
                        switch (part.Kind)
                        {
                            case CXTokenKind.Interpolation:
                                children.Add(new ComponentChild.Interpolation(part));
                                break;
                            case CXTokenKind.Text:
                                children.Add(new ComponentChild.Text(part));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(part));
                        }
                    }

                    break;
                case CXValue.Interpolation interpolation:
                    children.Add(new ComponentChild.Interpolation(interpolation.Token));
                    break;
                case CXValue.Scalar scalar:
                    children.Add(new ComponentChild.Text(scalar.Token));
                    break;
                case CXElement childElement:
                    children.Add(new ComponentChild.Element(childElement));
                    context.AddChildren(childElement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(child));
            }
        }

        return [..children];
    }

    private Result<string> RenderComponent(
        IComponentContext context,
        ComponentState state,
        EquatableArray<ComponentChild> children
    )
    {
        if (_componentBuilderKind is ComponentBuilderKind.None) return string.Empty;

        var cardinalityOfMany = IsCollectionType || _componentBuilderKind.SupportsCardinalityOfMany();

        if (!cardinalityOfMany)
        {
            // expect one child
            switch (children.Count)
            {
                case 0:
                    if (IsOptional) return "default";

                    return Result<string>.FromDiagnostic(
                        Diagnostics.MissingRequiredProperty(_owner.Name, "children"),
                        state.Source
                    );
                case 1:
                    return RenderComponentInner(_componentBuilderKind, context, children[0], state);
                default:
                    // too many children
                    var lower = children[1].Node.Span.Start;
                    var upper = children.Last().Node.Span.End;

                    return Result<string>.FromDiagnostic(
                        Diagnostics.InvalidChildComponentCardinality("children"),
                        TextSpan.FromBounds(lower, upper)
                    );
            }
        }

        if (IsCollectionType)
        {
            if (children.Count is 0)
            {
                if (IsOptional) return "[]";

                return Result<string>.FromDiagnostic(
                    Diagnostics.MissingRequiredProperty(_owner.Name, "children"),
                    state.Source
                );
            }

            return RenderChildrenAsCollection(_componentBuilderKind);
        }
        else
        {
            /*
             * A non-collection, cardinality of many type.
             * There are 2 cases for this point, either a 'MessageComponent' or 'CXMessageComponent'.
             * For construction, they both take a collection of either builder interfaces or component
             * interfaces, so we'll need to convert the children to one of those types.
             *
             * Since CX renders to the builders, we'll do the same, and wrap the construction manually
             */

            var typeName = _componentBuilderKind switch
            {
                ComponentBuilderKind.CXMessageComponent => "global::Discord.CXMessageComponent",
                ComponentBuilderKind.MessageComponent => "global::Discord.MessageComponent",
                _ => throw new ArgumentOutOfRangeException(nameof(_componentBuilderKind))
            };

            if (children.Count is 0)
            {
                if (IsOptional) return $"{typeName}.Empty";

                return Result<string>.FromDiagnostic(
                    Diagnostics.MissingRequiredProperty(_owner.Name, "children"),
                    state.Source
                );
            }

            return RenderChildrenAsCollection(ComponentBuilderKind.IMessageComponentBuilder)
                .Map(x => $"new {typeName}({x})");
        }

        Result<string> RenderChildrenAsCollection(ComponentBuilderKind kind)
        {
            var parts = new List<Result<string>>();

            foreach (var child in children)
            {
                parts.Add(
                    RenderComponentInner(
                        kind,
                        context,
                        child,
                        state,
                        spreadCollections: true
                    )
                );
            }

            return parts
                .FlattenAll()
                .Map(x =>
                    $"[{
                        string.Join(
                            Environment.NewLine,
                            x.Select(x => x.Prefix(4).WithNewlinePadding(4))
                        )
                        .WrapIfSome(Environment.NewLine)
                    }]"
                );
        }
    }

    private Result<string> RenderComponentInner(
        ComponentBuilderKind kind,
        IComponentContext context,
        ComponentChild child,
        ComponentState state,
        bool spreadCollections = false
    )
    {
        /*
         * 'target' is guaranteed to be one of
         *  - CXMessageComponent
         *  - IMessageComponentBuilder
         *  - IMessageComponent
         *  - MessageComponent
         */

        switch (child)
        {
            case ComponentChild.Interpolation(var token):
                var info = context.GetInterpolationInfo(token);

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        info.Symbol,
                        context.Compilation,
                        out var interpolationKind
                    ) ||
                    !ComponentBuilderKindUtils.TryConvert(
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        ),
                        interpolationKind,
                        kind,
                        out var converted,
                        spreadCollections
                    )
                )
                {
                    return Result<string>.FromDiagnostic(
                        Diagnostics.TypeMismatch(kind.ToString(), info.Symbol!.ToDisplayString()),
                        token
                    );
                }

                return converted;
            case ComponentChild.Element(var element):
                if (!state.TryGetChildGraphNode(element, out var childNode))
                {
                    // can happen if the child node wasn't valid in the graph, we can just not render
                    return string.Empty;
                }

                return childNode
                    .Render(context)
                    .Map(x =>
                    {
                        if (
                            !ComponentBuilderKindUtils.TryConvert(
                                x,
                                ComponentBuilderKind.IMessageComponentBuilder,
                                kind,
                                out converted,
                                spreadCollections
                            )
                        )
                        {
                            return Result<string>.FromDiagnostic(
                                Diagnostics.TypeMismatch(kind.ToString(), "CX"),
                                element
                            );
                        }

                        return converted;
                    });
            case ComponentChild.Text(var token):
                return Result<string>.FromDiagnostic(
                    Diagnostics.TypeMismatch(kind.ToString(), "text"),
                    token
                );
            default: throw new ArgumentOutOfRangeException(nameof(child));
        }
    }

    private Result<string> RenderNonComponent(
        IComponentContext context,
        ComponentState state,
        EquatableArray<ComponentChild> children
    )
    {
        if (!IsCollectionType)
        {
            // expect only 1 child
            switch (children.Count)
            {
                case 0:
                    if (IsOptional) return "default";

                    return Result<string>.FromDiagnostic(
                        Diagnostics.MissingRequiredProperty(_owner.Name, "children"),
                        state.Source
                    );
                case 1:
                    return RenderNonComponentInner(_targetSymbol, context, children[0]);
                default:
                    // too many children
                    var lower = children[1].Node.Span.Start;
                    var upper = children.Last().Node.Span.End;

                    return Result<string>.FromDiagnostic(
                        Diagnostics.InvalidChildComponentCardinality("children"),
                        TextSpan.FromBounds(lower, upper)
                    );
            }
        }

        // should not be null if it's a collection
        if (_collectionInnerTypeSymbol is null) return string.Empty;

        if (children.Count is 0)
        {
            if (IsOptional) return "[]";

            return Result<string>.FromDiagnostic(
                Diagnostics.MissingRequiredProperty(_owner.Name, "children"),
                state.Source
            );
        }

        var parts = new List<Result<string>>();

        foreach (var child in children)
        {
            if (child is ComponentChild.Interpolation(var token))
            {
                var info = context.GetInterpolationInfo(token);
                // check if we can spread

                var enumerableType = info
                    .Symbol
                    ?.AllInterfaces
                    .FirstOrDefault(x =>
                        x.IsGenericType &&
                        x.ConstructedFrom.Equals(
                            context.KnownTypes.IEnumerableOfTType,
                            SymbolEqualityComparer.Default
                        )
                    );

                if (enumerableType is not null)
                {
                    if (
                        !context.Compilation.HasImplicitConversion(
                            enumerableType.TypeArguments[0],
                            _collectionInnerTypeSymbol
                        )
                    )
                    {
                        // cant spread, element types don't match
                        return Result<string>.FromDiagnostic(
                            Diagnostics.TypeMismatch(
                                _collectionInnerTypeSymbol.ToDisplayString(),
                                enumerableType.TypeArguments[0].ToDisplayString()
                            ),
                            token
                        );
                    }

                    parts.Add(
                        $"..{
                            context.GetDesignerValue(
                                info,
                                enumerableType
                                    .TypeArguments[0]
                                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                        }"
                    );
                    continue;
                }
            }

            parts.Add(RenderNonComponentInner(_collectionInnerTypeSymbol, context, child));
        }

        return parts
            .FlattenAll()
            .Map(x =>
                $"[{
                    string.Join(
                            Environment.NewLine,
                            x.Select(x => x.Prefix(4).WithNewlinePadding(4))
                        )
                        .WrapIfSome(Environment.NewLine)
                }]"
            );
    }

    private Result<string> RenderNonComponentInner(
        ITypeSymbol target,
        IComponentContext context,
        ComponentChild child
    )
    {
        switch (child)
        {
            case ComponentChild.Element(var element):
                // TODO: maybe check for conversion from message builder to the target?
                return Result<string>.FromDiagnostic(
                    Diagnostics.TypeMismatch(target.ToDisplayString(), "CX"),
                    element
                );
            case ComponentChild.Interpolation(var token):
                // validate the type
                var info = context.GetInterpolationInfo(token);
                if (
                    !context.Compilation.HasImplicitConversion(
                        info.Symbol,
                        target
                    )
                )
                {
                    return Result<string>.FromDiagnostic(
                        Diagnostics.TypeMismatch(target.ToDisplayString(), info.Symbol!.ToDisplayString()),
                        token
                    );
                }

                // return out the designer value
                return context.GetDesignerValue(info, target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            case ComponentChild.Text(var token):
                /*
                 * TODO: we could make use of the renderers for this, ex parsing colors, ints, etc
                 * for now, check if strings are allowed
                 */

                if (
                    !context.Compilation.HasImplicitConversion(
                        context.Compilation.GetKnownTypes().StringType,
                        target
                    )
                )
                {
                    return Result<string>.FromDiagnostic(
                        Diagnostics.TypeMismatch(
                            context.Compilation.GetKnownTypes().StringType.ToDisplayString(),
                            target.ToDisplayString()
                        ),
                        token
                    );
                }

                return Renderers.ToCSharpString(token.Value);
            default:
                throw new ArgumentOutOfRangeException(nameof(child));
        }
    }

    // public static ComponentChildrenAdapter Create(
    //     CXElement element,
    //     
    //     )
    // {

    // }
}