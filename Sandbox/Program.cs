using Discord;
using Discord.CX.Parser;
using static Discord.ComponentDesigner;
using CXElement = Discord.CXElement;

var user = new User("test", null);

var x = cx(
    $"""
     <container>
         <TestProvider User={user}/>
         <TestFunc />
     </container>
     """
);

public static IMessageComponentBuilder TestFunc(int foo)
{
    return null!;
}

class TestState
{
    [CXProperty] public User User { get; set; }
}

class TestProvider : ICXProvider<TestState>
{
    public static IMessageComponentBuilder Render(TestState state)
        => (new UserHeader() { User = state.User }).Render();
}

class UserHeader : CXElement
{
    public required User User { get; init; }

    public override IMessageComponentBuilder Render()
    {
        var header = cx($"<text>{User.Name}</text>");

        if (User.Avatar is null) return header;

        return cx(
            $"""
             <section>
                 {header}
                 <accessory>
                     <thumbnail url={User.Avatar}/>
                 </accessory>
             </section>
             """
        );
    }
}

record User(string Name, string? Avatar);