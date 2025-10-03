// using Discord;
// using static Discord.ComponentDesigner;
//
// namespace Sandbox.Examples.Spyfall;
//
// public class PackComponentsExample : CXElement
// {
//     public const int PACK_LOCATIONS_PER_PAGE = 3;
//
//     public required Pack Pack { get; set; }
//     public int PageLocation { get; set; }
//
//     
//     public int PageNumber => PageLocation + 1;
//     public int PageLower => (PageLocation * PACK_LOCATIONS_PER_PAGE) + 1;
//     public Location[] DisplayedLocations => Pack.Locations
//         .Skip(PageLocation * PACK_LOCATIONS_PER_PAGE)
//         .Take(PACK_LOCATIONS_PER_PAGE)
//         .ToArray();
//
//     public int PageUpper => PageLower + DisplayedLocations.Length;
//     
//     public override IMessageComponentBuilder Render()
//         => cx(
//             $"""
//             <container>
//                 <Header />
//                 <Author />
//                 
//                 <separator size="large" />
//                 
//                 <Locations />
//                 <PageControls />
//                 
//                 <separator />
//                 
//                 <text>
//                     -# pack id: {Pack.Id}
//                 </text>
//             </container>
//             """
//         );
//
//     private IMessageComponentBuilder PageControls()
//         => cx(
//             $"""
//              <row>
//                  <button onClick={() => PageLocation--} disabled={PageLocation == 0}>Previous</button>
//                  <button onClick={() => PageLocation++} disabled={Pack.Locations.Count <= PageUpper}>Next</button>
//              </row>
//              """
//         );
//
//     private IMessageComponentBuilder Locations()
//     {
//         var displayedLocations = Pack.Locations
//             .Skip(PageLocation * PACK_LOCATIONS_PER_PAGE)
//             .Take(PACK_LOCATIONS_PER_PAGE)
//             .ToArray();
//
//         return cx(
//             $"""
//              <text>
//                  ## {Pack.Locations.Count} Locations
//                  -# Showing page {PageNumber}, locations {PageLower} to {PageUpper}
//              </text>
//              {
//                  displayedLocations
//                      .Select(Location)
//              }
//              """
//         );
//
//         IMessageComponentBuilder Location(Location location, int index)
//         {
//             var locationComponent = cx(
//                 $"""
//                  <text> 
//                      ### {index + PageLower}. {location.Name}
//                      {
//                          string.Join(Environment.NewLine, location.Roles.Select(x => $"- {x.Name} ({x.Chance}%)"))
//                      }
//                  </text>
//                  """
//             );
//
//             if (location.Icon is not null)
//                 locationComponent = cx(
//                     $"""
//                      <section>
//                          {locationComponent}
//                          <accessory>
//                              <thumbnail url={location.Icon} />
//                          </accessory>
//                      </section>
//                      """
//                 );
//
//             return cx(
//                 """
//                 <separator />
//                 {locationComponent}
//                 """
//             );
//         }
//     }
//
//     private IMessageComponentBuilder? Author()
//     {
//         if (Pack.Author is null) return null;
//
//         return cx(
//             $"""
//              <section>
//                 <text>
//                     Created by {Pack.Author.DisplayName}
//                 </text>
//                 <accessory>
//                     <button style="secondary" customId="view-profile-{Pack.Author.Id}">
//                         View Profile
//                     </button>
//                 </accessory>
//              </section>
//              """
//         );
//     }
//
//     private IMessageComponentBuilder Header()
//     {
//         var header = cx(
//             $"""
//              <text>
//                  # {Pack.Name}
//                  {Pack.Description}
//              </text>
//              """
//         );
//
//         if (Pack.Icon is null) return header;
//
//         return cx(
//             $"""
//              <section>
//                 {header}
//                 <accessory>
//                     <thumbnail url={Pack.Icon}/>
//                 </accessory>
//              </section>
//              """
//         );
//     }
// }