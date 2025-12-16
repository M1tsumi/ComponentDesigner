using Discord.CX.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Microsoft.CodeAnalysis;

namespace Discord.CX.Nodes;

public delegate Result<string> ComponentNodeRenderer<in TState>(
    TState state,
    IComponentContext context,
    ComponentRenderingOptions options = default
) where TState : ComponentState;

public delegate string ComponentNodeRenderer(
    ComponentState state,
    IComponentContext context,
    ComponentRenderingOptions options = default
);

public abstract class ComponentNode<TState> : ComponentNode
    where TState : ComponentState
{
    public abstract Result<string> Render(TState state, IComponentContext context, ComponentRenderingOptions options);

    public virtual TState UpdateState(TState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
        => state;

    public sealed override ComponentState UpdateState(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    ) => UpdateState((TState)state, context, diagnostics);

    public abstract TState? CreateState(ComponentStateInitializationContext context, IList<DiagnosticInfo> diagnostics);

    public sealed override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    ) => CreateState(context, diagnostics);

    public sealed override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => Render((TState)state, context, options);

    public virtual void Validate(TState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        base.Validate(state, context, diagnostics);
    }

    public sealed override void Validate(ComponentState state, IComponentContext context,
        IList<DiagnosticInfo> diagnostics)
        => Validate((TState)state, context, diagnostics);
}

public abstract class ComponentNode
{
    protected virtual bool IsUserAccessible => true;

    public abstract string Name { get; }
    public virtual IReadOnlyList<string> Aliases { get; } = [];

    public virtual bool HasChildren => false;

    public virtual IReadOnlyList<ComponentProperty> Properties { get; } = [];

    protected virtual bool AllowChildrenInCX => HasChildren;

    public virtual void Validate(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        // validate properties
        foreach (var property in Properties)
        {
            var propertyValue = state.GetProperty(property);

            propertyValue.ReportPropertyConfigurationDiagnostics(context, state, diagnostics);

            foreach (var validator in property.Validators)
            {
                validator(context, propertyValue, diagnostics);
            }
        }

        if (state.Source is CXElement element)
        {
            // report any unknown properties
            foreach (var attribute in element.Attributes)
            {
                if (!TryGetPropertyFromName(attribute.Identifier.Value, out _))
                {
                    diagnostics.Add(
                        Diagnostics.UnknownProperty(
                            attribute.Identifier.Value,
                            Name
                        ),
                        attribute
                    );
                }
            }

            // report invalid children
            if (!AllowChildrenInCX && !HasChildren && element.Children.Count > 0)
            {
                diagnostics.Add(
                    Diagnostics.ComponentDoesntAllowChildren(Name),
                    element.Children
                );
            }
        }
    }

    private bool TryGetPropertyFromName(string name, out ComponentProperty result)
    {
        foreach (var property in Properties)
        {
            if (property.Name == name || property.Aliases.Contains(name))
            {
                result = property;
                return true;
            }
        }

        result = null!;
        return false;
    }

    public abstract Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    );

    public virtual ComponentState UpdateState(
        ComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    ) => state;

    public virtual ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        return new ComponentState(
            context.GraphNode,
            context.CXNode
        );
    }

    public virtual void AddGraphNode(ComponentGraphInitializationContext context)
    {
        context.Push(
            this,
            cxNode: context.CXNode,
            children: HasChildren && context.CXNode is CXElement element
                ? element.Children
                : null
        );
    }


    private static readonly Dictionary<string, ComponentNode> _nodes;

    static ComponentNode()
    {
        _nodes = typeof(ComponentNode)
            .Assembly
            .GetTypes()
            .Where(x =>
                !x.IsAbstract &&
                typeof(ComponentNode).IsAssignableFrom(x) &&
                x.GetConstructor(Type.EmptyTypes) is not null
            )
            .Select(x => (ComponentNode)Activator.CreateInstance(x)!)
            .Where(x => x.IsUserAccessible)
            .SelectMany(x => x
                .Aliases
                .Prepend(x.Name)
                .Select(y => new KeyValuePair<string, ComponentNode>(y, x)))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public static bool TryGetComponentNode<T>(out T node)
        where T : ComponentNode
        => (node = (T?)_nodes.Values
                .FirstOrDefault(x => x.GetType() == typeof(T))!)
            is not null;

    public static T GetComponentNode<T>() where T : ComponentNode
        => _nodes.Values.OfType<T>().First();

    public static bool TryGetNode(string name, out ComponentNode node)
        => _nodes.TryGetValue(name, out node);

    public static bool TryGetProviderNode(
        SemanticModel cxSemanticModel,
        int position,
        string name,
        out ComponentNode node
    )
    {
        foreach (var candidate in cxSemanticModel.LookupSymbols(position, name: name))
        {
            if (candidate is INamedTypeSymbol typeSymbol)
            {
                var providerInterface = typeSymbol
                    .AllInterfaces
                    .FirstOrDefault(x =>
                        x.IsGenericType &&
                        x.ConstructedFrom.Equals(
                            cxSemanticModel.Compilation.GetKnownTypes().ICXProviderType,
                            SymbolEqualityComparer.Default
                        )
                    );

                if (providerInterface is null ||
                    providerInterface.TypeArguments[0] is not INamedTypeSymbol stateSymbol) continue;

                node = new ProviderComponentNode(stateSymbol, typeSymbol, cxSemanticModel.Compilation);
                return true;
            }

            if (candidate is IMethodSymbol methodSymbol)
            {
                if (!methodSymbol.IsStatic)
                {
                    continue;
                }

                if (methodSymbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
                    continue;

                if (
                    !ComponentBuilderKindUtils.IsValidComponentBuilderType(
                        methodSymbol.ReturnType,
                        cxSemanticModel.Compilation,
                        out var kind
                    )
                ) continue;

                node = FunctionalComponentNode.Create(methodSymbol, kind, cxSemanticModel.Compilation);
                return true;
            }
        }


        node = null!;
        return false;
    }
}