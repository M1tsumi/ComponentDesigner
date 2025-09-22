using Discord.CX.Parser;

var x = Parse("<butto />");


Console.WriteLine(x);

CXDoc Parse(string source, CXDoc? other = null)
{
    var source = other is not null
        ? 
    
    var reader = new CXSourceReader(
        new CXSourceText.StringSource(source),
        new(0, source.Length),
        [],
        3
    );

    return CXParser.Parse(reader);
}