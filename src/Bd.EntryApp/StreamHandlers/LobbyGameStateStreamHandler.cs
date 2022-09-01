using System.Threading.Channels;
using Bd.EntryApp.Models.Games;
using Bd.GrainInterfaces.Games;
using Bd.GrainInterfaces.Games.Models;
using Orleans.Streams;

namespace Bd.EntryApp.StreamHandlers;

public class LobbyGameStateStreamHandler : IAsyncObserver<LobbyStateUpdate>
{
    private readonly Channel<LobbyState> _channel;

    public LobbyGameStateStreamHandler(Channel<LobbyState> channel)
    {
        _channel = channel;
    }

    public async Task OnNextAsync(LobbyStateUpdate item, StreamSequenceToken? token = null)
    {
        var mappedPlayers = item.Players.ConvertAll(x => new LobbyPlayer(x.CharacterId, x.Name, x.Color, x.Wins));
        
        await _channel.Writer.WriteAsync(new LobbyState(
            State: item.State.ToString(),
            Players: mappedPlayers));
    }

    public Task OnCompletedAsync()
    {
        // do nothing
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        // do nothing
        return Task.CompletedTask;
    }
}