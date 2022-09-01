using System.Security.Cryptography;
using Bd.GrainInterfaces.Games;
using Bd.GrainInterfaces.Players;
using Orleans;

namespace Bd.Grains.Games;

public class PlayerCharacter : Grain, IPlayerCharacter
{
    private IGameLobby? _lobby;
    private IPlayer? _player;
    
    private int _health = 3;
    private string _color = "#000000";

    private double _positionX;
    private double _positionY;

    private int _power = 1;
    private DateTime? _bombCooldownUntil;

    private int _wins = 0;


    public PlayerCharacter()
    {
        var colors = new[]
        {
            "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3", "#03A9F4",
            "#00BCD4", "#009688", "#4CAF50", "#8BC34A", "#FFEB3B", "#FF9800", "#FF5722"
        };
        _color = colors[RandomNumberGenerator.GetInt32(colors.Length)];
    }


    public Task ResetStats()
    {
        _power = 1;
        _bombCooldownUntil = null;
        _health = 3;

        _positionX = 0;
        _positionY = 0;
        
        return Task.CompletedTask;
    }
    
    
    public async Task AddToLobby(IGameLobby lobby)
    {
        if (await lobby.GetState() != GameState.Waiting)
            throw new InvalidOperationException("Lobby is not waiting for players");
        _lobby = lobby;
    }

    public Task RemoveLobby()
    {
        _lobby = null;
        if (_player == null && _lobby == null) DeactivateOnIdle();
        return Task.CompletedTask;
    }

    public Task<IGameLobby> GetLobby()
    {
        if (_lobby == null) throw new InvalidOperationException("Character has no lobby.");
        return Task.FromResult(_lobby);
    }

    
    public Task SetPlayer(IPlayer player)
    {
        _player = player;
        return Task.CompletedTask;
    }

    public Task RemovePlayer()
    {
        _player = null;
        if (_player == null && _lobby == null) DeactivateOnIdle();
        return Task.CompletedTask;
    }

    public Task<IPlayer> GetPlayer()
    {
        if (_player == null)
            throw new InvalidOperationException("Character should have player before anything else is done");
        return Task.FromResult(_player);
    }


    public Task SetHealth(int health)
    {
        _health = health;
        return Task.CompletedTask;
    }

    public Task<int> GetHealth()
    {
        return Task.FromResult(_health);
    }


    public Task SetColor(string hex)
    {
        _color = hex;
        return Task.CompletedTask;
    }

    public Task<string> GetColor()
    {
        return Task.FromResult(_color);
    }

    
    public Task SetPosition(double x, double y)
    {
        _positionX = x;
        _positionY = y;
        return Task.CompletedTask;
    }

    public Task<(double, double)> GetPosition()
    {
        return Task.FromResult((_positionX, _positionY));
    }
    
    public Task<(int, int)> GetTilePosition()
    {
        // +0.4 to center on character.
        var x = (int)Math.Floor(_positionX + 0.4);
        var y = (int)Math.Floor(_positionY + 0.4);
        return Task.FromResult((x, y));
    }

    
    public Task IncreasePower()
    {
        _power++;
        return Task.CompletedTask;
    }

    public Task<int> GetPower()
    {
        return Task.FromResult(_power);
    }

    
    public Task SetBombCooldown()
    {
        if (_lobby == null) throw new InvalidOperationException("Must join lobby before dropping bombs.");
        
        _bombCooldownUntil = DateTime.UtcNow.AddMilliseconds(1500);
        return Task.CompletedTask;
    }

    public Task<bool> CanDropBomb()
    {
        var canDrop = _bombCooldownUntil == null || _bombCooldownUntil <= DateTime.UtcNow;
        return Task.FromResult(canDrop);
    }

    
    public Task AddWin()
    {
        _wins++;
        return Task.CompletedTask;
    }

    public Task<int> GetWins()
    {
        return Task.FromResult(_wins);
    }
}