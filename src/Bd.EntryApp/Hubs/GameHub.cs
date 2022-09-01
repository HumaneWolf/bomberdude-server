using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Bd.EntryApp.Models.Games;
using Bd.EntryApp.Models.Players;
using Bd.EntryApp.StreamHandlers;
using Bd.EntryApp.Utilities;
using Bd.GrainInterfaces.Games;
using Bd.GrainInterfaces.Games.Models;
using Bd.GrainInterfaces.Players;
using Microsoft.AspNetCore.SignalR;
using Orleans;
using ActiveBomb = Bd.EntryApp.Models.Games.ActiveBomb;

namespace Bd.EntryApp.Hubs;

public class GameHub : Hub<IGameHubClient>
{
    private readonly IClusterClient _cluster;

    public GameHub(IClusterClient cluster)
    {
        _cluster = cluster;
    }

    private string? GetPlayerKey()
    {
        return (string?)Context.Items["PlayerKey"];
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var playerKey = GetPlayerKey();
        if (playerKey != null)
        {
            var player = _cluster.GetGrain<IPlayer>(playerKey);

            await player.HandleLeave();
        }
    }

    public async Task<PlayerResponse> RegisterPlayer(PlayerRegisterRequest request)
    {
        var playerKey = StringGenerator.GenerateRandomString(64);
        var player = _cluster.GetGrain<IPlayer>(playerKey);

        Context.Items["PlayerKey"] = playerKey;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            await player.SetName(request.Name.Trim());
        }
        var name = await player.GetName();
        return new PlayerResponse(name);
    }
    
    public async Task<PlayerJoinResponse> JoinGame(PlayerJoinRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode)) throw new InvalidOperationException();

        var p = _cluster.GetGrain<IPlayer>(GetPlayerKey());
        var game = _cluster.GetGrain<IGameLobby>(request.JoinCode);

        // Check game state
        if (await game.GetState() != GameState.Waiting)
        {
            return new PlayerJoinResponse(false, "Game is not waiting for players", null, null, null, null);
        }
        
        // Check player count
        var players = await game.GetPlayers();
        if (players.Count >= 4)
        {
            return new PlayerJoinResponse(false, "Game is full.", null, null, null, null);
        }

        // Create and add character
        var characterId = Guid.NewGuid();
        var character = _cluster.GetGrain<IPlayerCharacter>(characterId);
        await p.SetCharacter(character);
        await game.AddPlayer(character);

        await Groups.AddToGroupAsync(Context.ConnectionId, request.JoinCode);
        
        players = await game.GetPlayers();
        return new PlayerJoinResponse(true, null, characterId, request.JoinCode,
            players.ConvertAll(x => new LobbyPlayer(x.CharacterId, x.Name, x.Color, x.Wins)), 4);
    }

    public async Task StartGame()
    {
        var playerKey = GetPlayerKey();
        if (playerKey == null) throw new InvalidOperationException("No player key.");

        var player = _cluster.GetGrain<IPlayer>(playerKey);
        var character = await player.GetCharacter();
        if (character == null) throw new InvalidOperationException("No character.");
        var lobby = await character.GetLobby();
        if (lobby == null) throw new InvalidOperationException("No lobby.");

        await lobby.StartGame();
    }

    public async Task<ChannelReader<LobbyState>> GetGameState(CancellationToken cancellationToken)
    {
        var playerKey = GetPlayerKey();
        if (playerKey == null) throw new InvalidOperationException("No player key.");

        var player = _cluster.GetGrain<IPlayer>(playerKey);
        var character = await player.GetCharacter();
        if (character == null) throw new InvalidOperationException("No character.");
        var lobby = await character.GetLobby();
        if (lobby == null) throw new InvalidOperationException("No lobby.");

        var streamProvider = _cluster.GetStreamProvider("SMSProvider");
        var stream = streamProvider.GetStream<LobbyStateUpdate>(await lobby.GetStreamId(), "GameLobby:GameState");

        var channel = Channel.CreateUnbounded<LobbyState>();
        await stream.SubscribeAsync(new LobbyGameStateStreamHandler(channel));

        return channel.Reader;
    }

    public async IAsyncEnumerable<ActiveBoard> GetGameBoard([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(10));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            var playerKey = GetPlayerKey();
            if (playerKey == null) throw new InvalidOperationException("No player key.");

            var player = _cluster.GetGrain<IPlayer>(playerKey);
            var character = await player.GetCharacter();
            if (character == null) throw new InvalidOperationException("No character.");
            var lobby = await character.GetLobby();
            if (lobby == null) throw new InvalidOperationException("No lobby.");

            var map = await lobby.GetMap();
            var bombs = await lobby.GetBombs();
            var players = await lobby.GetPlayers();

            var winner = await lobby.GetWinner();
            LobbyPlayer? mappedWinner = null;
            if (winner != null)
                mappedWinner = new LobbyPlayer(winner.CharacterId, winner.Name, winner.Color, winner.Wins);

            yield return new ActiveBoard(mappedWinner,
                map.ConvertAll(x => x.ConvertAll(y => y.ToString())),
                bombs.ConvertAll(b => new ActiveBomb(b.X, b.Y)),
                players.ConvertAll(p => new GamePlayer(p.CharacterId, p.Name, p.Color, p.Wins, p.IsAlive, p.Health,
                    p.PositionX, p.PositionY))
            );
        }
    }
    
    public async Task SetPosition(PositionRequest request)
    {
        var playerKey = GetPlayerKey();
        if (playerKey == null) throw new InvalidOperationException("No player key.");

        var player = _cluster.GetGrain<IPlayer>(playerKey);
        var character = await player.GetCharacter();
        if (character == null) throw new InvalidOperationException("No character.");
        var lobby = await character.GetLobby();
        if (lobby == null) throw new InvalidOperationException("No lobby.");

        await character.SetPosition(request.X, request.Y);
    }

    public async Task DropBomb()
    {
        var playerKey = GetPlayerKey();
        if (playerKey == null) throw new InvalidOperationException("No player key.");

        var player = _cluster.GetGrain<IPlayer>(playerKey);
        var character = await player.GetCharacter();
        if (character == null) throw new InvalidOperationException("No character.");
        var lobby = await character.GetLobby();
        if (lobby == null) throw new InvalidOperationException("No lobby.");

        await lobby.DropBomb(character);
    }
    
    public async Task PlayAgain()
    {
        var playerKey = GetPlayerKey();
        if (playerKey == null) throw new InvalidOperationException("No player key.");

        var player = _cluster.GetGrain<IPlayer>(playerKey);
        var character = await player.GetCharacter();
        if (character == null) throw new InvalidOperationException("No character.");
        var lobby = await character.GetLobby();
        if (lobby == null) throw new InvalidOperationException("No lobby.");

        await lobby.PlayAgain();
    }
}
