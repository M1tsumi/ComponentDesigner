using System;
using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Custom;

public sealed class FunctionalComponentNodeState : ComponentState
{
    public IReadOnlyList<ComponentChildrenAdapter.ComponentChild> Children { get; init; }
}

public class FunctionalComponentNode : ComponentNode<FunctionalComponentNodeState>, IDynamicComponentNode
{
    public override bool HasChildren => _childrenParameter is not null;

    public override string Name => $"<functional {_method.ToDisplayString()}>";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    private readonly IMethodSymbol _method;
    private readonly InterleavedKind _kind;
    private readonly IParameterSymbol? _childrenParameter;
    private readonly ComponentChildrenAdapter? _adapter;

    private FunctionalComponentNode(
        IMethodSymbol method,
        InterleavedKind kind,
        IReadOnlyList<ComponentProperty> properties,
        IParameterSymbol? childrenParameter,
        Compilation compilation
    )
    {
        _method = method;
        _kind = kind;
        _childrenParameter = childrenParameter;
        Properties = properties;

        _adapter = childrenParameter is not null
            ? ComponentChildrenAdapter.Create(
                compilation,
                childrenParameter.Type,
                childrenParameter.HasExplicitDefaultValue,
                this
            )
            : null;
    }

    public static FunctionalComponentNode Create(IMethodSymbol method, InterleavedKind kind, Compilation compilation)
    {
        var properties = new List<ComponentProperty>();
        IParameterSymbol? childrenParameter = null;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];

            // TODO: should we restrict children to the last parameter only?
            if (i == method.Parameters.Length - 1)
            {
                // check for child parameter
                if (
                    parameter
                    .GetAttributes()
                    .Any(x =>
                        compilation
                            .GetKnownTypes()
                            .CXChildrenAttribute!
                            .Equals(x.AttributeClass, SymbolEqualityComparer.Default)
                    )
                )
                {
                    childrenParameter = parameter;
                }
            }

            properties.Add(new(
                parameter.Name,
                parameter.HasExplicitDefaultValue || ReferenceEquals(childrenParameter, parameter),
                renderer: Renderers.CreateRenderer(parameter.Type)
            ));
        }

        return new FunctionalComponentNode(
            method,
            kind,
            properties,
            childrenParameter,
            compilation
        );
    }

    public override FunctionalComponentNodeState? CreateState(ComponentStateInitializationContext context)
        => new()
        {
            Source = context.Node,
            Children = _adapter?.AdaptToState(context) ?? []
        };

    private string MethodReference =>
        $"{_method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{_method.Name}";

    public override string Render(FunctionalComponentNodeState state, ComponentContext context)
        => InterleavedComponentNode.ExtrapolateKindToBuilders(
            _kind,
            $"{MethodReference}({
                string
                    .Join(
                        ", ",
                        ((string[])
                        [
                            state.RenderProperties(this, context),
                            (_adapter?.ChildrenRenderer(context, state, state.Children) ?? string.Empty)
                            .PrefixIfSome($"{_childrenParameter?.Name}: ")
                        ])
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                    )
                    .WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome("\n")
            })"
        );
}