using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
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

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        var validItemCount = 0;

        foreach (var child in state.Children)
        {
            if (!IsValidChild(child.Inner))
            {
                diagnostics.Add(
                    Diagnostics.InvalidMediaGalleryChild(child.Inner.Name),
                    child.State.Source
                );
            }
            else validItemCount++;
        }

        if (validItemCount is 0)
        {
            diagnostics.Add(
                Diagnostics.MediaGalleryIsEmpty,
                state.Source
            );
        }
        else if (validItemCount > Constants.MAX_MEDIA_ITEMS)
        {
            var extra = state
                .Children
                .Where(x => IsValidChild(x.Inner))
                .Skip(Constants.MAX_MEDIA_ITEMS)
                .ToArray();

            var span = TextSpan.FromBounds(
                extra[0].State.Source.Span.Start,
                extra[extra.Length - 1].State.Source.Span.End
            );

            diagnostics.Add(
                Diagnostics.TooManyItemsInMediaGallery,
                span
            );
        }

        base.Validate(state, context, diagnostics);
    }

    private static bool IsValidChild(ComponentNode node)
        => node is IDynamicComponentNode
            or MediaGalleryItemComponentNode;

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context, asInitializers: true)
        .Combine(state.RenderChildren(context))
        .Map(x =>
        {
            var (props, children) = x;

            var init = new StringBuilder(props);

            if (!string.IsNullOrWhiteSpace(children))
            {
                if (!string.IsNullOrWhiteSpace(props)) init.Append(',').AppendLine();

                init.Append(
                    $"""
                     Items =
                     [
                         {children.WithNewlinePadding(4)}
                     ]
                     """
                );
            }

            return
                $"new {context.KnownTypes.MediaGalleryBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(){
                    init.ToString()
                        .WithNewlinePadding(4)
                        .PrefixIfSome($"{Environment.NewLine}{{{Environment.NewLine}".Postfix(4))
                        .PostfixIfSome($"{Environment.NewLine}}}")}";
        });
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
                aliases: ["media"],
                renderer: Renderers.UnfurledMediaItem,
                dotnetParameterName: "media"
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

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Map(x =>
            $"""
             new {context.KnownTypes.MediaGalleryItemPropertiesType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(
                 {x.WithNewlinePadding(4)}
             )
             """
        );
}