using System.Diagnostics.CodeAnalysis;
using System.Text;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis.Text;

namespace UnitTests.ParseTests;

public abstract class BaseParsingTest
{
    protected CXDoc? Document { get; private set; }
    private IEnumerator<ICXNode>? _enumerator;


    protected sealed class SourceBuilder
    {
        public StringBuilder StringBuilder { get; } = new();
        public List<TextSpan> Interpolations { get; } = [];
        
        public SourceBuilder AddSource(string source)
        {
            StringBuilder.Append(source);
            return this;
        }

        public SourceBuilder AddInterpolation(string interpolation)
        {
            var actual = $"{{{interpolation}}}";
            
            var span = new TextSpan(StringBuilder.Length, actual.Length);
            Interpolations.Add(span);
            
            return AddSource(actual);
        }
    }

    [MemberNotNull(nameof(Document))]
    protected void Parses(
        SourceBuilder source,
        Func<CXParser, IEnumerable<CXNode>>? parseFunc = null
    ) => Parses(source.StringBuilder.ToString(), parseFunc, source.Interpolations.ToArray());
    
    [MemberNotNull(nameof(Document))]
    protected void Parses(
        string cx,
        Func<CXParser, IEnumerable<CXNode>>? parseFunc = null,
        TextSpan[]? interpolations = null
    )
    {
        parseFunc ??= (parser) => parser.ParseRootNodes();
        
        var parser = new CXParser(CXSourceText.From(cx).CreateReader(interpolations: interpolations));
        var nodes = parseFunc(parser).ToList();

        Document = new CXDoc(parser, nodes);
        
        Assert.False(Document.HasErrors);

        _enumerator = nodes
            .SelectMany(Enumerate)
            .GetEnumerator();
    }

    private IEnumerable<ICXNode> Enumerate(ICXNode node)
    {
        yield return node;

        if (node.Slots.Count is 0) yield break;

        foreach (var slot in node.Slots)
        foreach (var next in ProcessNode(slot.Value))
        {
            yield return next;
        }

        IEnumerable<ICXNode> ProcessNode(ICXNode node)
        {
            if (node is ICXCollection collection)
            {
                // don't return the collection type
                return collection.ToList().SelectMany(ProcessNode);
            }

            return Enumerate(node);
        }
    }

    private void Reset()
    {
        _enumerator = null;
    }

    private ICXNode GetNextNode()
    {
        Assert.NotNull(_enumerator);
        Assert.True(_enumerator.MoveNext());

        return _enumerator.Current;
    }

    protected CXElement Element(string? content = null) => Node<CXElement>(content);
    protected CXAttribute Attribute(string? content = null) => Node<CXAttribute>(content);

    protected CXValue.StringLiteral StringLiteral(string? content = null) => Node<CXValue.StringLiteral>(content);

    protected CXValue.Interpolation Interpolation(string? content = null, int? index = null)
    {
        var interpolation = Node<CXValue.Interpolation>(content);
        
        if(index is not null) Assert.Equal(index.Value, interpolation.InterpolationIndex);

        return interpolation;
    }
    
    protected CXValue.Scalar Scalar(string? content = null) => Node<CXValue.Scalar>(content);
    
    protected CXValue.Multipart Multipart(string? content = null) => Node<CXValue.Multipart>(content);

    protected T Node<T>(string? content = null) where T : ICXNode
    {
        var current = GetNextNode();

        Assert.IsType<T>(current);

        if (content is not null) Assert.Equal(content, current.ToString());

        return (T)current;
    }

    protected CXToken Identifier(string content)
        => Token(CXTokenKind.Identifier, content);

    protected CXToken InterpolationToken(string? content = null, int? index = null)
    {
        Assert.NotNull(Document);
        
        var token = Token(CXTokenKind.Interpolation, content);
        
        if(index.HasValue) Assert.Equal(index.Value, Document.GetInterpolationIndex(token));
        
        return token;
    }

    protected CXToken Token(CXTokenKind kind, string? content = null)
    {
        var current = GetNextNode();

        Assert.IsType<CXToken>(current);

        var token = (CXToken)current;

        Assert.Equal(kind, token.Kind);

        if (content is not null)
            Assert.Equal(content, token.ToString());

        return token;
    }

    protected void EOF()
    {
        Assert.NotNull(_enumerator);
        Assert.False(_enumerator.MoveNext());
    }
}