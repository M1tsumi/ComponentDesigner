namespace Discord.CX.Parser;

public enum CXTokenKind : byte
{
    Invalid,
    EOF,

    LessThan,
    GreaterThan,
    ForwardSlashGreaterThan,
    LessThanForwardSlash,
    Equals,

    Text,
    Interpolation,

    StringLiteralStart,
    StringLiteralEnd,
    
    OpenParenthesis,
    CloseParenthesis,

    Identifier,
}

public static class CXTokenKindExtensions
{
    public static bool TryGetText(this CXTokenKind kind, out string text)
    {
        text = kind switch
        {
            CXTokenKind.LessThan => "<",
            CXTokenKind.GreaterThan => ">",
            CXTokenKind.ForwardSlashGreaterThan => "/>",
            CXTokenKind.LessThanForwardSlash => "</",
            CXTokenKind.Equals => "=",
            CXTokenKind.OpenParenthesis => "(",
            CXTokenKind.CloseParenthesis => ")",
            CXTokenKind.EOF or CXTokenKind.Invalid => string.Empty,
            _ => null!
        };
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return text is not null;
    }
}