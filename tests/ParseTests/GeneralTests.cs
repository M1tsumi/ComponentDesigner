namespace UnitTests.ParseTests;

public class GeneralTests
{
    [Fact]
    public void Test()
    {
        var driver = Generation.RunGenerator(
            $$""""
              using Discord;
              using static Discord.ComponentDesigner;
              
              public class FunctionComponents
              {
                  public static CXMessageComponent Consumer()
                      => cx(
                          $"""
                           <CompWithScalarChild foo="b">
                               {1}
                               {2}
                           </CompWithScalarChild>
                           """
                      );
                  
                  public static CXMessageComponent CompWithChildren(string name, [CXChildren] CXMessageComponent component)
                      => cx(
                          $"""
                           <container>
                               {component}
                           </container>
                           """
                      );
                      
                  public static CXMessageComponent CompWithScalarChild(string foo, [CXChildren] int bar)
                  {
                      return null!;
                  }
              }
              """"
        );
    }
}