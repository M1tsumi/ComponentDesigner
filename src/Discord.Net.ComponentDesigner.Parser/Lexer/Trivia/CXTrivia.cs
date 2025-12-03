using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Parser;

public abstract record CXTrivia
{
    public abstract int Length { get; }

    public bool IsWhitespaceTrivia
        => this is Token { Kind: CXTriviaTokenKind.Newline or CXTriviaTokenKind.Whitespace };

    public abstract override string ToString();

    public sealed record Token(
        CXTriviaTokenKind Kind,
        string Value
    ) : CXTrivia()
    {
        public override int Length => Value.Length;
        
        public override string ToString() => Value;
    }

    public sealed record XmlComment(
        Token Start,
        Token Value,
        Token? End
    ) : CXTrivia
    {
        public override int Length => Start.Length + Value.Length + (End?.Length ?? 0);
        
        public override string ToString()
            => $"{Start}{Value}{End}";
    }
}