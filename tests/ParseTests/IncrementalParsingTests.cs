using Discord.CX.Parser;
using Xunit.Abstractions;

namespace UnitTests.ParseTests;

public sealed class IncrementalParsingTests(ITestOutputHelper output) : BaseIncrementalTests(output)
{
    //[Fact]
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
    
    //[Fact]
    public void AttributeRemoved()
    {
        Parse("<foo bar />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                Ident("foo");
                
                Attribute();
                {
                    Ident("bar");
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo />");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                Ident("foo", reused: true);
                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
    
    //[Fact]
    public void AttributeNameChange()
    {
        Parse("<foo bar />");
        {
            Element();
            {
                T(CXTokenKind.LessThan);
                Ident("foo");
                
                Attribute();
                {
                    Ident("bar");
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan);
            }
        }

        Parse("<foo baz/>");
        {
            Element(reused: false);
            {
                T(CXTokenKind.LessThan, reused: true);
                Ident("foo", reused: true);
                
                Attribute(reused: false);
                {
                    Ident("baz", reused: false);
                }
                
                T(CXTokenKind.ForwardSlashGreaterThan, reused: true);
            }
        }
    }
}