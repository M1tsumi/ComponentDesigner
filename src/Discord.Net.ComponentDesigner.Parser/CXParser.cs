using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Discord.CX.Parser;

public sealed partial class CXParser
{
    public CXToken CurrentToken => Lex(_tokenIndex);
    public CXToken NextToken => Lex(_tokenIndex + 1);

    public ICXNode? CurrentNode
        => (_currentBlendedNode ??= GetCurrentBlendedNode())?.Value;

    public CXLexer Lexer { get; }

    private readonly List<CXToken> _tokens;
    private int _tokenIndex;

    public IReadOnlyList<ICXNode> BlendedNodes =>
    [
        .._blendedNodes
            .Select(x => x.Value)
            .Where(x => x is not null)!
    ];

    public IReadOnlyList<CXToken> Tokens
        => IsIncremental ? [..BlendedNodes.OfType<CXToken>()] : [.._tokens];

    private readonly List<BlendedNode> _blendedNodes;

    public CXSourceReader Reader { get; }

    public bool IsIncremental => Blender is not null;

    public CXBlender? Blender { get; }

    public CancellationToken CancellationToken { get; }


    private BlendedNode? _currentBlendedNode;

    public CXParser(CXSourceReader reader, CancellationToken token = default)
    {
        CancellationToken = token;
        Reader = reader;
        Lexer = new CXLexer(Reader, token);
        _tokens = [];
        _blendedNodes = [];
    }

    public CXParser(CXSourceReader reader, CXDoc document, TextChangeRange change, CancellationToken token = default)
        : this(reader, token)
    {
        Blender = new CXBlender(Lexer, document, change);
    }

    public static CXDoc Parse(CXSourceReader reader, CancellationToken token = default)
    {
        var parser = new CXParser(reader, token: token);

        return new CXDoc(parser, [..parser.ParseRootNodes()]);
    }

    internal IEnumerable<CXNode> ParseRootNodes()
    {
        while (CurrentToken.Kind is not CXTokenKind.EOF and not CXTokenKind.Invalid)
        {
            var node = ParseRootNode();
            yield return node;
            CancellationToken.ThrowIfCancellationRequested();
            if (node.Width is 0) yield break;
        }
    }

    internal CXNode ParseRootNode()
    {
        if (IsIncremental && CurrentNode is CXNode && EatNode() is { } node) return node;

        using var _ = Lexer.SetMode(CXLexer.LexMode.Default);

        switch (CurrentToken.Kind)
        {
            case CXTokenKind.LessThan: return ParseElement();
            default:
                using (Lexer.SetMode(CXLexer.LexMode.ElementValue))
                {
                    if (TryParseElementChild([], out var child))
                        return child;
                }

                break;
        }

        return new CXValue.Invalid()
        {
            Diagnostics =
            [
                CXDiagnostic.InvalidRootElement(CurrentToken)
            ]
        };
    }

    internal CXElement ParseElement()
    {
        if (IsIncremental && CurrentNode is CXElement incElement)
        {
            EatNode();
            return incElement;
        }

        using var _ = Lexer.SetMode(CXLexer.LexMode.Default);

        var diagnostics = new List<CXDiagnostic>();

        var start = Expect(CXTokenKind.LessThan);

        // for element identifiers, we allow fragments, which can have attributes.

        CXToken? identifier;

        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            if (
                (CurrentToken.Kind is CXTokenKind.Identifier && NextToken.Kind is CXTokenKind.Equals) ||
                CurrentToken.Kind is CXTokenKind.GreaterThan
            )
            {
                // this is a fragment
                identifier = null;
            }
            else
            {
                // just expect an identifier
                identifier = Expect(CXTokenKind.Identifier);
            }
        }

        var attributes = ParseAttributes();

        switch (CurrentToken.Kind)
        {
            case CXTokenKind.GreaterThan:
                var end = Eat();
                // parse children
                var children = ParseElementChildren();

                ParseClosingElement(
                    end,
                    out var endStart,
                    out var endIdent,
                    out var endClose, 
                    out var onCreate
                );

                var element = new CXElement(
                    start,
                    identifier,
                    attributes,
                    end,
                    children,
                    endStart,
                    endIdent,
                    endClose
                ) { Diagnostics = diagnostics };
                
                onCreate?.Invoke(element);

                return element;
            default:
            case CXTokenKind.ForwardSlashGreaterThan:
                return new CXElement(
                    start,
                    identifier,
                    attributes,
                    Expect(CXTokenKind.ForwardSlashGreaterThan),
                    new()
                );
        }

        void ParseClosingElement(
            CXToken elementEnd,
            out CXToken elementEndStart,
            out CXToken? elementEndIdent,
            out CXToken elementEndClose,
            out Action<CXElement>? onCreate
        )
        {
            onCreate = null;
            var sentinel = _tokenIndex;

            elementEndStart = Expect(CXTokenKind.LessThanForwardSlash);

            if (identifier is null)
            {
                // it's a fragment, don't expect a name
                elementEndIdent = null;
            }
            else
            {
                elementEndIdent = ParseIdentifier();
            }

            elementEndClose = Expect(CXTokenKind.GreaterThan);

            var missingStructure
                = elementEndStart.IsMissing ||
                  (elementEndIdent?.IsMissing ?? false) ||
                  elementEndClose.IsMissing;

            var missingNamedClosing = identifier is null
                ? elementEndIdent is not null
                : elementEndIdent is null || identifier.Value != elementEndIdent.Value;

            if (
                missingNamedClosing ||
                missingStructure
            )
            {
                onCreate = node =>
                {
                    node.AddDiagnostic(CXDiagnostic.MissingElementClosingTag(identifier, node));
                };

                elementEndStart = CXToken.CreateMissing(CXTokenKind.LessThanForwardSlash);
                elementEndIdent = identifier is not null
                    ? CXToken.CreateMissing(CXTokenKind.Identifier, identifier.Value)
                    : null;
                elementEndClose = CXToken.CreateMissing(CXTokenKind.GreaterThan);

                // rollback
                _tokenIndex = sentinel;
            }
        }

        CXCollection<CXNode> ParseElementChildren()
        {
            if (IsIncremental && CurrentNode is CXCollection<CXNode> incrementalChildren)
            {
                EatNode();
                return incrementalChildren;
            }

            // valid children are:
            //  - other elements
            //  - interpolations
            //  - text
            var children = new List<CXNode>();
            var diagnostics = new List<CXDiagnostic>();

            using (Lexer.SetMode(CXLexer.LexMode.ElementValue))
            {
                while (TryParseElementChild(diagnostics, out var child))
                    children.Add(child);

                CancellationToken.ThrowIfCancellationRequested();
            }

            return new CXCollection<CXNode>(children) { Diagnostics = diagnostics };
        }
    }

    internal bool TryParseElementChild(List<CXDiagnostic> diagnostics, out CXNode node)
    {
        if (IsIncremental && CurrentNode is CXValue or CXElement)
        {
            node = EatNode()!;
            return true;
        }

        var parts = new List<CXToken>();

        while (CurrentToken.Kind is not CXTokenKind.EOF or CXTokenKind.Invalid)
        {
            switch (CurrentToken.Kind)
            {
                case CXTokenKind.Interpolation:
                    parts.Add(Eat());
                    continue;
                case CXTokenKind.Text:
                    parts.Add(Eat());
                    continue;
                case CXTokenKind.LessThan:
                    node = parts.Count > 0 ? BuildParts() : ParseElement();
                    return true;
                case CXTokenKind.LessThanForwardSlash:
                case CXTokenKind.EOF:
                case CXTokenKind.Invalid: goto end;
                default:
                    if (parts.Count is 0)
                    {
                        diagnostics.Add(CXDiagnostic.InvalidElementChildToken(CurrentToken));
                    }

                    goto case CXTokenKind.Invalid;
            }
        }

        end:

        if (parts.Count > 0)
        {
            node = BuildParts();
            return true;
        }

        node = null!;
        return false;

        CXValue BuildParts()
        {
            if (parts.Count is 0) return new CXValue.Invalid();

            if (parts.Count is 1)
                return parts[0] switch
                {
                    { Kind: CXTokenKind.Interpolation } token
                        => new CXValue.Interpolation(token, Lexer.InterpolationMap.IndexOf(token)),
                    { Kind: CXTokenKind.Text } token => new CXValue.Scalar(token),
                    _ => throw new InvalidOperationException($"Unknown single value kind '{parts[0].Kind}'")
                };

            return new CXValue.Multipart(new(parts));
        }
    }

    internal CXCollection<CXAttribute> ParseAttributes()
    {
        if (IsIncremental && CurrentNode is CXCollection<CXAttribute> incrementalNode)
        {
            EatNode();
            return incrementalNode;
        }

        var attributes = new List<CXAttribute>();

        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            while (CurrentToken.Kind is CXTokenKind.Identifier)
                attributes.Add(ParseAttribute());

            CancellationToken.ThrowIfCancellationRequested();
        }

        return new CXCollection<CXAttribute>(attributes);
    }

    internal CXAttribute ParseAttribute()
    {
        if (IsIncremental && CurrentNode is CXAttribute attribute)
        {
            EatNode();
            return attribute;
        }

        using (Lexer.SetMode(CXLexer.LexMode.Attribute))
        {
            var identifier = ParseIdentifier();

            if (!Eat(CXTokenKind.Equals, out var equalsToken))
            {
                return new CXAttribute(
                    identifier,
                    null,
                    null
                );
            }

            // parse attribute values
            var value = ParseAttributeValue();

            return new CXAttribute(
                identifier,
                equalsToken,
                value
            );
        }
    }

    internal CXValue ParseAttributeValue()
    {
        if (IsIncremental && CurrentNode is CXValue value)
        {
            EatNode();
            return value;
        }

        using (Lexer.SetMode(CXLexer.LexMode.Attribute))
        {
            switch (CurrentToken.Kind)
            {
                case CXTokenKind.OpenParenthesis:
                    return new CXValue.Element(
                        Eat(),
                        ParseElement(),
                        Expect(CXTokenKind.CloseParenthesis)
                    );

                case CXTokenKind.Interpolation:
                    return new CXValue.Interpolation(
                        Eat(),
                        Lexer.InterpolationIndex!.Value
                    );
                case CXTokenKind.StringLiteralStart:
                    return ParseStringLiteral();
                default:
                    return new CXValue.Invalid()
                    {
                        Diagnostics =
                        [
                            CXDiagnostic.InvalidAttributeValue(CurrentToken)
                        ]
                    };
            }
        }
       
    }

    internal CXValue ParseStringLiteral()
    {
        if (IsIncremental && CurrentNode is CXValue value)
        {
            EatNode();
            return value;
        }

        var diagnostics = new List<CXDiagnostic>();

        var tokens = new List<CXToken>();

        var quoteToken = CurrentToken.Kind;

        var start = Expect(CXTokenKind.StringLiteralStart);

        using var _ = Lexer.SetMode(CXLexer.LexMode.StringLiteral);

        // we grab the last char to ensure it's a quote in case its actually escaped
        Lexer.QuoteChar = start.Value[start.Value.Length - 1];

        while (CurrentToken.Kind is not CXTokenKind.StringLiteralEnd)
        {
            CancellationToken.ThrowIfCancellationRequested();

            switch (CurrentToken.Kind)
            {
                case CXTokenKind.Text:
                case CXTokenKind.Interpolation:
                    tokens.Add(Eat());
                    continue;

                case CXTokenKind.Invalid or CXTokenKind.EOF: goto end;

                default:
                    diagnostics.Add(
                        CXDiagnostic.InvalidStringLiteralToken(CurrentToken)
                    );
                    goto end;
            }
        }

        end:
        var end = Expect(CXTokenKind.StringLiteralEnd);

        return new CXValue.StringLiteral(
            start,
            new CXCollection<CXToken>(tokens),
            end
        ) { Diagnostics = diagnostics };
    }

    internal bool TryScanElementIdentifier(out CXToken? identifier)
    {
        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            if (CurrentToken.Kind is CXTokenKind.Identifier && NextToken.Kind is not CXTokenKind.Equals)
            {
                identifier = Eat();
                return true;
            }
        }

        identifier = null;
        return false;
    }

    internal CXToken ParseIdentifier()
    {
        using (Lexer.SetMode(CXLexer.LexMode.Identifier))
        {
            return Expect(CXTokenKind.Identifier);
        }
    }

    internal CXToken Eat()
    {
        var token = CurrentToken;
        _tokenIndex++;
        return token;
    }

    internal bool Eat(CXTokenKind kind, out CXToken token)
    {
        token = CurrentToken;

        if (token.Kind == kind)
        {
            _tokenIndex++;
            return true;
        }

        return false;
    }

    internal CXToken Expect(params ReadOnlySpan<CXTokenKind> kinds)
    {
        var current = CurrentToken;

        switch (kinds.Length)
        {
            case 0: throw new InvalidOperationException("Missing expected token");
            case 1: return Expect(kinds[0]);
            default:
                foreach (var kind in kinds)
                {
                    if (current.Kind == kind) return Eat();
                }

                return new CXToken(
                    kinds[0],
                    0,
                    0,
                    Flags: CXTokenFlags.Missing,
                    FullValue: string.Empty,
                    CXDiagnostic.UnexpectedToken(current, kinds.ToArray())
                );
        }
    }

    internal CXToken Expect(CXTokenKind kind)
    {
        var token = CurrentToken;

        if (token.Kind != kind)
        {
            return new CXToken(
                kind,
                0,
                0,
                Flags: CXTokenFlags.Missing,
                FullValue: string.Empty,
                CXDiagnostic.UnexpectedToken(token, kind)
            );
        }

        _tokenIndex++;
        return token;
    }

    private BlendedNode? GetCurrentBlendedNode()
        => Blender?.NextNode(
            _tokenIndex is 0
                ? Blender.StartingCursor
                : _blendedNodes[Math.Min(_tokenIndex - 1, _blendedNodes.Count - 1)].Cursor
        );

    private CXNode? EatNode()
    {
        if (_currentBlendedNode?.Value is not CXNode node) return null;

        _blendedNodes.Add(_currentBlendedNode!.Value);
        _tokenIndex++;

        _currentBlendedNode = null;

        node.ResetCachedState();
        return node;
    }

    internal CXToken Lex(int index)
    {
        if (Blender is not null) return FetchBlended();

        while (_tokens.Count <= index)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var token = Lexer.Next();

            _tokens.Add(token);

            if (token.Kind is CXTokenKind.EOF) return token;
        }

        return _tokens[index];

        CXToken FetchBlended()
        {
            // remove any non-token nodes
            while (_blendedNodes.Count > 0 && _blendedNodes[_blendedNodes.Count - 1].Value is CXNode)
            {
                _blendedNodes.RemoveAt(_blendedNodes.Count - 1);
                index--;
            }

            while (_blendedNodes.Count <= index)
            {
                CancellationToken.ThrowIfCancellationRequested();

                var cursor = _blendedNodes.Count is 0
                    ? Blender.StartingCursor
                    : _blendedNodes[_blendedNodes.Count - 1].Cursor;

                var node = Blender.NextToken(cursor);

                _blendedNodes.Add(node);
                _currentBlendedNode = null;

                if (node.Value is CXToken { Kind: CXTokenKind.EOF } eof) return eof;
            }

            return (CXToken)_blendedNodes[index].Value;
        }
    }
}