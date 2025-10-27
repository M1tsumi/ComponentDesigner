using Discord.CX.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components.SelectMenus;

public enum SelectKind
{
    String,
    User,
    Role,
    Channel,
    Mentionable
}

public enum ChannelType
{
    Text = 0,
    DM = 1,
    Voice = 2,
    Group = 3,
    Category = 4,
    News = 5,
    Store = 6,
    NewsThread = 10,
    PublicThread = 11,
    PrivateThread = 12,
    Stage = 13,
    GuildDirectory = 14,
    Forum = 15,
    Media = 16,
}

public sealed class SelectMenuComponentNode : ComponentNode
{
    public sealed class MissingTypeState : ComponentState;

    public sealed class InvalidTypeState : ComponentState
    {
        public string? Kind { get; init; }
    }

    public sealed class SelectState : ComponentState
    {
        public SelectKind Kind { get; init; }
        public IReadOnlyList<SelectMenuDefaultValue> Defaults { get; init; } = [];
    }

    public override string Name => "select-menu";

    public override IReadOnlyList<string> Aliases { get; } = ["select"];

    public ComponentProperty Id { get; }
    public ComponentProperty Type { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty Placeholder { get; }
    public ComponentProperty MinValues { get; }
    public ComponentProperty MaxValues { get; }
    public ComponentProperty Required { get; }
    public ComponentProperty Disabled { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public override bool HasChildren => true;

    public SelectMenuComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Type = new(
                "type",
                isOptional: true,
                synthetic: true
            ),
            CustomId = new(
                "customId",
                isOptional: false,
                renderer: Renderers.String
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                renderer: Renderers.String
            ),
            MinValues = new(
                "minValues",
                isOptional: true,
                aliases: ["min"],
                renderer: Renderers.Integer
            ),
            MaxValues = new(
                "maxValues",
                isOptional: true,
                aliases: ["max"],
                renderer: Renderers.Integer
            ),
            Required = new(
                "required",
                isOptional: true,
                renderer: Renderers.Boolean
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                renderer: Renderers.Boolean
            )
        ];
    }

    public override ComponentState? Create(ComponentStateInitializationContext context)
    {
        if (context.Node is not CXElement element) return null;

        var typeAttribute = element.Attributes
            .FirstOrDefault(x => x.Identifier.Value.ToLowerInvariant() is "type");

        if (typeAttribute is null) return new MissingTypeState() { Source = context.Node };

        if (typeAttribute.Value is not CXValue.StringLiteral { HasInterpolations: false } typeValue)
            return new InvalidTypeState() { Source = context.Node, };

        if (!TryGetSelectKind(typeValue.Tokens.ToString(), out var kind))
            return new InvalidTypeState()
            {
                Source = context.Node,
                Kind = typeValue.Tokens.ToString()
            };

        var defaults = new List<SelectMenuDefaultValue>();

        switch (kind)
        {
            case SelectKind.Channel or SelectKind.Role or SelectKind.User or SelectKind.Mentionable:
                defaults.AddRange(element.Children.Select(SelectMenuDefaultValue.Create));
                break;
            case SelectKind.String:
                context.AddChildren(element.Children);
                break;
        }

        return new SelectState()
        {
            Source = element,
            Kind = kind,
            Defaults = defaults
        };


        static bool TryGetSelectKind(string name, out SelectKind kind)
        {
            switch (name)
            {
                case "string" or "text":
                    kind = SelectKind.String;
                    return true;
                case "user":
                    kind = SelectKind.User;
                    return true;
                case "role":
                    kind = SelectKind.Role;
                    return true;
                case "channel":
                    kind = SelectKind.Channel;
                    return true;
                case "mention" or "mentionable":
                    kind = SelectKind.Mentionable;
                    return true;
            }

            kind = default;
            return false;
        }
    }

    public override void Validate(ComponentState state, ComponentContext context)
    {
        base.Validate(state, context);

        switch (state)
        {
            case MissingTypeState:
                context.AddDiagnostic(
                    Diagnostics.MissingSelectMenuType,
                    state.Source
                );
                return;
            case InvalidTypeState { Kind: var kind }:
                context.AddDiagnostic(
                    kind is not null
                        ? Diagnostics.SpecifiedInvalidSelectMenuType
                        : Diagnostics.InvalidSelectMenuType,
                    state.Source,
                    kind is not null ? [kind] : null
                );
                return;
            case SelectState selectState:

                Validators.Range(context, state, MinValues, MaxValues);
                
                switch (selectState.Kind)
                {
                    case SelectKind.String:
                        ValidateStringSelectMenu(selectState, context);
                        break;
                    default:
                        foreach (var defaultValue in selectState.Defaults)
                        {
                            defaultValue.Validate(context, selectState);
                        }

                        break;
                }

                break;
        }
    }

    private void ValidateStringSelectMenu(SelectState state, ComponentContext context)
    {
        var validChildrenCount = 0;

        foreach (var child in state.Children)
        {
            if (!IsValidStringSelectChild(child.Inner))
            {
                context.AddDiagnostic(
                    Diagnostics.InvalidStringSelectChild,
                    child.State.Source,
                    child.Inner.Name
                );
            }
            else validChildrenCount++;
        }

        switch (validChildrenCount)
        {
            case 0:
                context.AddDiagnostic(
                    Diagnostics.EmptyStringSelectMenu,
                    state.Source
                );
                break;
            case > Constants.STRING_SELECT_MAX_VALUES:
                context.AddDiagnostic(
                    Diagnostics.TooManyStringSelectMenuChildren,
                    TextSpan.FromBounds(
                        state.Children.Skip(Constants.STRING_SELECT_MAX_VALUES).First()
                            .State.Source.Span.Start,
                        state.Children.Skip(Constants.STRING_SELECT_MAX_VALUES).Last()
                            .State.Source.Span.End
                    )
                );
                break;
        }
    }

    private static bool IsValidStringSelectChild(ComponentNode node)
        => node is IDynamicComponentNode or SelectMenuOptionComponentNode;

    public override string Render(ComponentState state, ComponentContext context)
    {
        if (state is not SelectState selectState) return string.Empty;

        var props = new StringBuilder();

        props.Append("type: ").Append(ToDiscordComponentType(context, selectState.Kind));

        var componentProps = state.RenderProperties(this, context);

        if (!string.IsNullOrWhiteSpace(componentProps))
            props.AppendLine(",").Append(componentProps);

        if (selectState.Defaults.Count > 0)
        {
            props.AppendLine(",").AppendLine("defaultValues: ").AppendLine("[");

            for (var i = 0; i < selectState.Defaults.Count; i++)
            {
                if (i > 0) props.AppendLine(",");
                var defaultValue = selectState.Defaults[i];
                props.Append("    ");

                var render = defaultValue.Render(context, selectState);

                if (render == string.Empty) return string.Empty;

                props.Append(render.WithNewlinePadding(4));
            }

            props.AppendLine().Append("]");
        }

        if (selectState.Kind is SelectKind.String)
        {
            var children = state.RenderChildren(context);

            if (!string.IsNullOrWhiteSpace(children))
            {
                props
                    .AppendLine(",")
                    .AppendLine("options: ")
                    .AppendLine("[")
                    .AppendLine(children.Prefix(4).WithNewlinePadding(4))
                    .Append("]");
            }
        }

        return
            $"""
             new {context.KnownTypes.SelectMenuBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                 {props.ToString().WithNewlinePadding(4)}
             )
             """;
    }

    private static string ToDiscordComponentType(ComponentContext context, SelectKind kind)
        => $"{context.KnownTypes.ComponentTypeEnumType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{
            kind switch {
                SelectKind.Channel => "ChannelSelect",
                SelectKind.Role => "RoleSelect",
                SelectKind.User => "UserSelect",
                SelectKind.Mentionable => "MentionableSelect",
                SelectKind.String => "StringSelect",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            }
        }";

    //     public override string Render(ComponentState state, ComponentContext context)
//         => $"""
//             new {context.KnownTypes.SelectMenuBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
//                 string
//                     .Join(
//                         $",{Environment.NewLine}",
//                         ((IEnumerable<string>)
//                         [
//                             (
//                                 state switch
//                                 {
//                                     UserSelectState => "UserSelect",
//                                     RoleSelectState => "RoleSelect",
//                                     MentionableSelectState => "MentionableSelect",
//                                     ChannelSelectState => "ChannelSelect",
//                                     _ => string.Empty
//                                 }
//                             ).Map(x => $"type: {context.KnownTypes.ComponentTypeEnumType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{x}"),
//                             state.RenderProperties(this, context),
//                             state.RenderChildren(context, x => x.Inner is StringSelectOptionComponentNode)
//                                 .Map(x =>
//                                     $"""
//                                      options:
//                                      [
//                                          {x.WithNewlinePadding(4)}
//                                      ]
//                                      """
//                                 ),
//                             state is SelectStateWithDefaults { Defaults: var defaults }
//                                 ? string
//                                     .Join(
//                                         $",{Environment.NewLine}",
//                                         defaults.Select(x => RenderDefaultValue(context, x))
//                                     )
//                                     .Map(x =>
//                                         $"""
//                                          defaultValues:
//                                          [
//                                              {x.WithNewlinePadding(4)}
//                                          ]
//                                          """
//                                     )
//                                 : string.Empty
//                         ]).Where(x => !string.IsNullOrEmpty(x))
//                     )
//                     .PrefixIfSome(4)
//                     .WithNewlinePadding(4)
//                     .WrapIfSome(Environment.NewLine)
//             })
//             """;
}