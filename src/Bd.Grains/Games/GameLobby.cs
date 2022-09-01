using Bd.GrainInterfaces.Games;
using Bd.GrainInterfaces.Games.Models;
using Orleans;
using Orleans.Streams;

namespace Bd.Grains.Games;

public class GameLobby : Grain, IGameLobby
{
    private readonly Guid _streamId = Guid.NewGuid();

    private GameState _state = GameState.Waiting;
    
    private List<List<TileType>> _map = new();
    private List<IPlayerCharacter> _players = new();

    private List<ActiveBomb> _bombs = new();

    private PlayerListing? _winner;

    private int _mapWidth = 5;
    private int _mapHeight = 5;
    

    public Task<Guid> GetStreamId()
    {
        return Task.FromResult(_streamId);
    }

    private IAsyncStream<LobbyStateUpdate> GetStateStream()
    {
        var streamProvider = GetStreamProvider("SMSProvider");
        return streamProvider.GetStream<LobbyStateUpdate>(_streamId, "GameLobby:GameState");
    }


    public async Task StartGame()
    {
        if (_state != GameState.Waiting) throw new InvalidOperationException("Cannot start game that's not waiting.");
        
        // Make map
        await GenerateMap();
        
        // Position players
        // todo: Support other player amounts dyanmically.
        if (_players.Count >= 1) await _players[0].SetPosition(0.1, 0.1);
        if (_players.Count >= 2) await _players[1].SetPosition(_mapWidth - 0.9, 0.1);
        if (_players.Count >= 3) await _players[2].SetPosition(0.1, _mapHeight - 0.9);
        if (_players.Count >= 4) await _players[3].SetPosition(_mapWidth - 0.9, _mapHeight - 0.9);
        
        _state = GameState.InProgress;
        await GetStateStream().OnNextAsync(new LobbyStateUpdate(_state, await GetPlayers()));
    }

    public async Task PlayAgain()
    {
        if (_state != GameState.CompletionScreen)
            throw new InvalidOperationException("Cannot reset game that's not finished.");

        foreach (var pc in _players)
        {
            await pc.ResetStats();
        }
        _map = new();
        _state = GameState.Waiting;
        _winner = null;
        _bombs = new();
        await GetStateStream().OnNextAsync(new LobbyStateUpdate(_state, await GetPlayers()));
    }

    public Task<GameState> GetState()
    {
        return Task.FromResult(_state);
    }

    
    public Task GenerateMap()
    {
        // todo: Generate better.
        _map = new List<List<TileType>>
        {
            new() { TileType.Clear, TileType.Clear, TileType.Wall, TileType.Clear, TileType.Clear },
            new() { TileType.Clear, TileType.Unbreakable, TileType.Wall, TileType.Unbreakable, TileType.Clear },
            new() { TileType.Wall, TileType.Wall, TileType.Wall, TileType.Wall, TileType.Wall },
            new() { TileType.Clear, TileType.Unbreakable, TileType.Wall, TileType.Unbreakable, TileType.Clear },
            new() { TileType.Clear, TileType.Clear, TileType.Wall, TileType.Clear, TileType.Clear },
        };
        return Task.CompletedTask;
    }

    
    public async Task AddPlayer(IPlayerCharacter character)
    {
        if (_state != GameState.Waiting) throw new InvalidOperationException("Game is not waiting for players.");
        
        _players.Add(character);
        await character.AddToLobby(this);
        await GetStateStream().OnNextAsync(new LobbyStateUpdate(_state, await GetPlayers()));
    }

    public async Task RemovePlayer(IPlayerCharacter character)
    {
        var id = character.GetGrainIdentity().IdentityString;
        var index = _players.FindIndex(x => x.GetGrainIdentity().IdentityString == id);
        _players.RemoveAt(index);
        await character.RemoveLobby();

        if (_players.Count == 0)
        {
            DeactivateOnIdle();
        }
        
        await GetStateStream().OnNextAsync(new LobbyStateUpdate(_state, await GetPlayers()));
    }

    public async Task<List<PlayerListing>> GetPlayers()
    {
        var players = new List<PlayerListing>();
        foreach (var character in _players)
        {
            var player = await character.GetPlayer();
            var name = await player.GetName();
            var color = await character.GetColor();
            var health = await character.GetHealth();
            var (posX, posY) = await character.GetPosition();
            var wins = await character.GetWins();
            
            players.Add(new PlayerListing(character.GetPrimaryKey(), name, color, health > 0, health, posX, posY, wins));
        }

        return players;
    }

    
    public Task SetTileType(int x, int y, TileType type)
    {
        _map[x][y] = type;
        return Task.CompletedTask;
    }

    public Task<TileType> GetTileType(int x, int y)
    {
        return Task.FromResult(_map[x][y]);
    }

    public Task<List<List<TileType>>> GetMap()
    {
        return Task.FromResult(_map);
    }

    
    public async Task<bool> ExplodeTile(int x, int y)
    {
        var tile = await GetTileType(x, y);
        if (tile == TileType.Wall)
        {
            await SetTileType(x, y, TileType.Clear);
        }

        // Make explosion effect on tile.
        if (tile != TileType.Unbreakable)
        {
            await SetTileType(x, y, TileType.Explosion);
            IDisposable? v = null;
            v = RegisterTimer(async (_) =>
                {
                    await SetTileType(x, y, TileType.Clear);
                    v?.Dispose();
                }, null,
                TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200));
        }

        foreach (var player in _players)
        {
            var (playerX, playerY) = await player.GetPosition();
            var pX1 = (int)Math.Floor(playerX);
            var pY1 = (int)Math.Floor(playerY);
            // +0.8 to count both corners of character.
            var pX2 = (int)Math.Floor(playerX + 0.8);
            var pY2 = (int)Math.Floor(playerY + 0.8);

            if ((x == pX1 || x == pX2) && (y == pY1 || y == pY2))
            {
                var health = await player.GetHealth();
                await player.SetHealth(health - 1);
            }
        }

        await CheckWinner();

        // Return true if it should continue.
        return tile == TileType.Clear;
    }

    public async  Task ExplodeFromTile(int x, int y, int power)
    {
        await ExplodeTile(x, y);
        
        var continueNorth = true;
        var continueSouth = true;
        var continueEast = true;
        var continueWest = true;
        
        for (int i = 1; i <= power; i++)
        {
            // North
            if (continueNorth && await IsInMap(x, y - i))
                continueNorth = await ExplodeTile(x, y - i);

            // South
            if (continueSouth && await IsInMap(x, y + i))
                continueSouth = await ExplodeTile(x, y + i);

            // East
            if (continueEast && await IsInMap(x + i, y))
                continueEast = await ExplodeTile(x + i, y);

            // West
            if (continueWest && await IsInMap(x - i, y))
                continueWest = await ExplodeTile(x - i, y);
        }
    }

    public async Task DropBomb(IPlayerCharacter character)
    {
        if (_state != GameState.InProgress) return;

        var canDrop = await character.CanDropBomb();
        if (!canDrop) return;

        var fuseTime = 1500;
        
        var power = await character.GetPower();
        var (tileX, tileY) = await character.GetTilePosition();

        var bombId = Guid.NewGuid();
        var bomb = new ActiveBomb(bombId, tileX, tileY, power);
        _bombs.Add(bomb);
        IDisposable? timer = null;

        timer = RegisterTimer(async _ =>
        {
            var inArray = _bombs.Any(x => x.Id == bombId);
            if (inArray)
            {
                await ExplodeFromTile(tileX, tileY, power);
                _bombs = _bombs.Where(x => x.Id != bombId).ToList();
            }
            timer?.Dispose();
        }, null, TimeSpan.FromMilliseconds(fuseTime), TimeSpan.FromMilliseconds(fuseTime));
    }

    public Task<List<ActiveBomb>> GetBombs()
    {
        return Task.FromResult(_bombs);
    }


    public async Task CheckWinner()
    {
        if (_state != GameState.InProgress) return;
        if (_winner != null) return;

        var livingPlayers = 0;
        IPlayerCharacter? livingPlayer = null;
        foreach (var player in _players)
        {
            if (await player.GetHealth() > 0)
            {
                livingPlayers++;
                livingPlayer = player;
            }
        }

        if (livingPlayers == 1 && livingPlayer != null)
        {
            await livingPlayer.AddWin();
            
            var player = await livingPlayer.GetPlayer();
            var name = await player.GetName();
            var color = await livingPlayer.GetColor();
            var health = await livingPlayer.GetHealth();
            var wins = await livingPlayer.GetWins();
            _winner = new PlayerListing(livingPlayer.GetPrimaryKey(), name, color, true, health, 0, 0, wins);

            IDisposable? d = null;
            d = RegisterTimer(async _ =>
            {
                _state = GameState.CompletionScreen;
                await GetStateStream().OnNextAsync(new LobbyStateUpdate(_state, await GetPlayers()));
                d?.Dispose();
            }, null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4));
        }
    }

    public Task<PlayerListing?> GetWinner()
    {
        return Task.FromResult(_winner);
    }


    public Task<bool> IsInMap(double x, double y)
    {
        if (x < 0 || x >= _mapWidth) return Task.FromResult(false);
        if (y < 0 || y >= _mapHeight) return Task.FromResult(false);
        return Task.FromResult(true);
    }
}