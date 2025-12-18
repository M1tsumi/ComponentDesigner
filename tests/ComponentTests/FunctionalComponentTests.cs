using Discord.CX.Nodes.Components;
using Discord.CX.Nodes.Components.Custom;
using Xunit.Abstractions;

namespace UnitTests.ComponentTests;

public sealed class FunctionalComponentTests(ITestOutputHelper output) : BaseComponentTest(output)
{
    [Fact]
    public void ChildOfContainer()
    {
        Graph(
            """
            <container>
                <MyFunc />
            </container>
            """,
            additionalMethods:
            """
            public static MessageComponent MyFunc() => null!;
            """
        );
        {
            Node<ContainerComponentNode>();
            {
                Node<FunctionalComponentNode>();
            }

            Validate(hasErrors: false);

            Renders(
                """
                new global::Discord.ContainerBuilder()
                {
                    Components =
                    [
                        ..global::TestClass.MyFunc().Components.Select(x => x.ToBuilder())
                    ]
                }
                """
            );

            EOF();
        }
    }

    [Fact]
    public void CommonScalarParameterTypes()
    {
        Graph(
            """
            <MyFunc 
                p1="str"
                p2="123"
                p3="blue"
            />
            """,
            additionalMethods:
            """
            public static MessageComponent MyFunc(
                string p1,
                int p2,
                Discord.Color p3
            ) => null!;
            """
        );
        {
            Node<FunctionalComponentNode>();

            Validate(hasErrors: false);

            Renders(
                """
                ..global::TestClass.MyFunc(
                    p1: "str",
                    p2: 123,
                    p3: global::Discord.Color.Blue
                ).Components.Select(x => x.ToBuilder())
                """
            );

            EOF();
        }
    }

    [Fact]
    public void AllValidComponentTypesAsReturnType()
    {
        TestReturnType(
            "MessageComponent",
            "..global::TestClass.MyFunc().Components.Select(x => x.ToBuilder())"
        );

        TestReturnType(
            "CXMessageComponent",
            "..global::TestClass.MyFunc().Builders"
        );

        TestReturnType(
            "IMessageComponentBuilder",
            "global::TestClass.MyFunc()"
        );

        TestReturnType(
            "IMessageComponent",
            "global::TestClass.MyFunc().ToBuilder()"
        );

        TestReturnType(
            "IEnumerable<IMessageComponent>",
            "..global::TestClass.MyFunc().Select(x => x.ToBuilder())"
        );

        // ..global::TestClass.MyFunc().Select(x => x)
        TestReturnType(
            "IEnumerable<IMessageComponentBuilder>",
            "..global::TestClass.MyFunc()"
        );

        TestReturnType(
            "IEnumerable<MessageComponent>",
            "..global::TestClass.MyFunc().SelectMany(x => x.Components.Select(x => x.ToBuilder()))"
        );

        TestReturnType(
            "IEnumerable<CXMessageComponent>",
            "..global::TestClass.MyFunc().SelectMany(x => x.Builders)"
        );


        void TestReturnType(string type, string expectedRenderedValue)
        {
            Graph(
                """
                <container>
                    <MyFunc />
                </container>
                """,
                additionalMethods:
                $"""
                 public static {type} MyFunc() => null!;
                 """
            );
            {
                Node<ContainerComponentNode>();
                {
                    Node<FunctionalComponentNode>();
                }

                Validate(hasErrors: false);

                Renders(
                    $$"""
                      new global::Discord.ContainerBuilder()
                      {
                          Components =
                          [
                              {{expectedRenderedValue}}
                          ]
                      }
                      """
                );

                EOF();
            }
        }
    }
}