using Discord.WebSocket;

namespace Discord;

public sealed class CXRenderer
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _provider;

    public CXRenderer(DiscordSocketClient client, IServiceProvider provider)
    {
        _client = client;
        _provider = provider;
    }
}