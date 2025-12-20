using Discord.CX.Parser;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public sealed class IncrementalParsingTests(ITestOutputHelper output) : BaseIncrementalTests(output)
{
    // [Fact]
    public void AttributeAdded()
    {
        Parse("<foo />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                Ident("foo");
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo bar />");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                Ident("foo", reused: true);

                Attribute(reused: false);
                {
                    Ident("bar", reused: false);
                }

                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
}