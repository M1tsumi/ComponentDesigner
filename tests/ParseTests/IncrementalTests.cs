using System.Diagnostics;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace UnitTests.ParseTests;

public class IncrementalTests
{
//     [Fact]
//     public void IncrementalWithChanges()
//     {
//         IncrementalRanges(
//             "<button />",
//             new TextChange(new(1, 0), " ")
//         );
//     }
//
//     [Fact]
//     public void LargeIncrementalFullRanges()
//     {
//         FullRangeIncremental(
//             """
//             <container>
//                 <text>Hello, World!</text>
//                 <button id="12" name="value" />
//                 <section>
//                     <text>## Foo</text>
//                     <accessory><thumbnail url="example" /></accessory>
//                 </section>
//             </container>
//             """
//         );
//     }
//     
//     
//     [Fact]
//     public void IncrementalButton()
//     {
//         foreach (
//             var result
//             in IterativeIncrementalParse(
//                 """
//                 <button>
//
//                 </button>
//                 """
//             )
//         )
//         {
//         }
//     }
    
    public void FullRangeIncremental(string cx)
    {
        var source = new CXSourceText.StringSource(cx);
        var reader = source.CreateReader();
        var doc = CXParser.Parse(reader);

        Assert.False(doc.HasErrors);
        
        foreach (var node in doc.GetFlatGraph())
        {
            // removing the entire graph wont do anything
            if (node is CXDoc || node.Width == doc.Width) continue;
            
            // empty nodes won't change anything
            if(node.Width is 0) continue;
            
            // dont remove attribute values
            if(node is CXValue && node.Parent is CXAttribute) continue;
            
            // dont remove individual tokens
            if(node is CXToken) continue;
            
            // remove the node from the text and try parsing again
            var subSource = new CXSourceText.StringSource(
                source.Text.Substring(0, node.FullSpan.Start) +
                source.Text.Substring(node.FullSpan.End)
            );
            var subReader = subSource.CreateReader();

            var newDoc = doc.IncrementalParse(
                subReader,
                [
                    new TextChange(node.FullSpan, string.Empty)
                ],
                out var incrementalParseResult
            );
            
            // ensure we have no errors
            Assert.False(newDoc.HasErrors);
        }
    }
    

    private (CXDoc, CXDoc, IncrementalParseResult) IncrementalRanges(string source, params IReadOnlyList<TextChange> changes)
    {
        var doc = CXParser.Parse(new CXSourceText.StringSource(source).CreateReader());

        var newSource = doc.Source.WithChanges(changes);

        var newDoc = doc.IncrementalParse(newSource.CreateReader(), changes, out var inc);

        return (doc, newDoc, inc);
    }
    
    private IEnumerable<CXDoc> Incremental(params string[] sources)
    {
        CXDoc? doc = null;

        foreach (var cx in sources)
        {
            if (doc is null)
            {
                yield return doc = CXParser.Parse(
                    new CXSourceText.StringSource(cx).CreateReader()
                );
                
                continue;
            }

            var changes = new CXSourceText.StringSource(cx).GetTextChanges(doc.Source);

            var newSource = doc.Source.WithChanges(changes);

            yield return doc = doc.IncrementalParse(newSource.CreateReader(), changes, out _);
        }
    }

    private IEnumerable<(CXDoc, IncrementalParseResult?)> IterativeIncrementalParse(
        string cx,
        CancellationToken token = default)
    {
        CXDoc? doc = null;
        for (var i = 0; i <= cx.Length; i++)
        {
            var source = new CXSourceText.StringSource(i == 0 ? string.Empty : cx.Substring(0, i));
            if (doc is null)
            {
               
                yield return (doc = CXParser.Parse(source.CreateReader(), token), null);
            }
            else
            {
                var changes = doc.Source.GetTextChanges(source);

                yield return (
                    doc.IncrementalParse(source.CreateReader(), changes, out var inc, token),
                    inc
                );
            }
        }
    }
}