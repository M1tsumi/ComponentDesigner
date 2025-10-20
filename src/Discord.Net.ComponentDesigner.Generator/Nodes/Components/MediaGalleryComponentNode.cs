using System.Collections.Generic;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed class MediaGalleryComponentNode : ComponentNode
{
    public override string Name => "media-gallery";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery"];

    public override IReadOnlyList<ComponentProperty> Properties { get; }
    
    public override bool HasChildren => true;

    public MediaGalleryComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
        ];
    }

    public override void Validate(ComponentState state, ComponentContext context)
    {
        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Inner))
            {
                context.AddDiagnostic(
                    Diagnostics.InvalidMediaGalleryChild,
                    child.State.Source,
                    child.Inner.Name
                );
            }   
        }
        
        base.Validate(state, context);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or MediaGalleryItemComponentNode;

    public override string Render(ComponentState state, ComponentContext context)
        => $$"""
            new {{context.KnownTypes.MediaGalleryBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}{{
                $"{
                    state
                        .RenderProperties(this, context, asInitializers: true)
                        .PostfixIfSome("\n")
                }{
                    state.RenderChildren(context)
                        .Map(x =>
                            $"""
                             Items =
                             [
                                 {x.WithNewlinePadding(4)}
                             ]
                             """
                        )
                }"
                    .TrimEnd()
                    .WithNewlinePadding(4)
                    .PrefixIfSome("\n{\n".Postfix(4))
                    .PostfixIfSome("\n}")
            }}
            """;
}

public sealed class MediaGalleryItemComponentNode : ComponentNode
{
    public override string Name => "media-gallery-item";

    public override IReadOnlyList<string> Aliases { get; } = ["gallery-item", "media", "item"];

    public ComponentProperty Url { get; }
    public ComponentProperty Description { get; }
    public ComponentProperty Spoiler { get; }

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public MediaGalleryItemComponentNode()
    {
        Properties =
        [
            Url = new(
                "url",
                renderer: Renderers.UnfurledMediaItem
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: Renderers.String
            ),
            Spoiler = new(
                "spoiler",
                isOptional: true,
                renderer: Renderers.Boolean,
                dotnetParameterName: "isSpoiler"
            )
        ];
    }

    public override string Render(ComponentState state, ComponentContext context)
        => $"""
            new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                {state.RenderProperties(this, context).WithNewlinePadding(4)}
            )
            """;
}
