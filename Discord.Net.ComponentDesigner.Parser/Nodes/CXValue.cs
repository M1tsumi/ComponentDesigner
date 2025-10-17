using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;

namespace Discord.CX.Parser;

public abstract class CXValue : CXNode
{
    public sealed class Invalid : CXValue;

    public class Multipart : CXValue
    {
        public bool HasInterpolations => Tokens.Any(x => x.Kind is CXTokenKind.Interpolation);
        public CXCollection<CXToken> Tokens { get; }

        public Multipart(CXCollection<CXToken> tokens)
        {
            Slot(Tokens = tokens);
        }
    }
    
    public sealed class StringLiteral : Multipart
    {
        public CXToken StartToken { get; }
        public CXToken EndToken { get; }

        public StringLiteral(
            CXToken start,
            CXCollection<CXToken> tokens,
            CXToken end
        ) : base(tokens)
        {
            Slot(StartToken = start);
            Slot(EndToken = end);
            
            // hack: we flip the slot order due to inheritance with the constructor
            SwapSlots(0, 1);
        }
    }

    public sealed class Interpolation : CXValue
    {
        public CXToken Token { get; }
        public int InterpolationIndex { get; }

        public Interpolation(CXToken token, int interpolationIndex)
        {
            Slot(Token = token);
            InterpolationIndex = interpolationIndex;
        }
    }

    public sealed class Scalar : CXValue
    {
        public string Value => Token.ToString();
        public CXToken Token { get; }

        public Scalar(CXToken token)
        {
            Slot(Token = token);
        }
    }
}
