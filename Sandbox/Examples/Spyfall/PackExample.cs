// using Discord;
// using static Discord.ComponentDesigner;
//
// namespace Sandbox.Examples.Spyfall;
//
// public class PackExample
// {
//     public IMessageComponentBuilder BasicPackInfo(Pack pack, int locationPage)
//     {
//         var header = cx(
//             $"""
//              <text>
//                  # {pack.Name}
//                  {pack.Description}
//              </text>
//              """
//         );
//
//         if (pack.Icon is not null)
//             header = cx(
//                 $"""
//                  <section>
//                      {header}
//                      <accessory>
//                          <thumbnail url={pack.Icon}
//                      </accessory>
//                  </section>
//                  """
//             );
//         
//         return cx(
//             $"""
//             <container>
//                 {header}
//                 
//             </container>
//             """
//         );
//     }
//     
//     
// }