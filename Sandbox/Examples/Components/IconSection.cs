using Discord;
using static Discord.ComponentDesigner;

namespace Sandbox.Examples.Components;

public class IconSection : CXElement
{
    public string? Url { get; set; }

    public override IMessageComponentBuilder Render()
        => Url is null
            ? Children
            : cx(
                $"""
                 <section>
                     {Children}
                     <accessory>
                         <thumbnail url={Url} />
                     </accessory>
                 </section>
                 """
            );
}