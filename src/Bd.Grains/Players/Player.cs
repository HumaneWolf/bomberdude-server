using System.Security.Cryptography;
using System.Text;
using Bd.GrainInterfaces.Games;
using Bd.GrainInterfaces.Players;
using Orleans;

namespace Bd.Grains.Players;

public class Player : Grain, IPlayer
{
    private IPlayerCharacter? _currentCharacter;
    private string _name;

    public Player()
    {
        _name = GenerateRandomName();
    }
    
    public async Task SetCharacter(IPlayerCharacter? character)
    {
        if (_currentCharacter != null)
        {
            await _currentCharacter.RemovePlayer();
        }
        
        _currentCharacter = character;
        if (_currentCharacter != null) await _currentCharacter.SetPlayer(this);
    }

    public Task<IPlayerCharacter?> GetCharacter()
    {
        return Task.FromResult(_currentCharacter);
    }

    public Task SetName(string name)
    {
        _name = name;
        return Task.CompletedTask;
    }

    public Task<string> GetName()
    {
        return Task.FromResult(_name);
    }

    public async Task HandleLeave()
    {
        if (_currentCharacter != null)
        {
            var lobby = await _currentCharacter.GetLobby();
            await lobby.RemovePlayer(_currentCharacter);
            await _currentCharacter.RemovePlayer();
        }
        
        DeactivateOnIdle();
    }


    private string GenerateRandomName()
    {
        var alphabet = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        var sb = new StringBuilder();
        sb.Append("New-Player-");
        var suffix = Enumerable.Range(0, 7)
            .Select(i => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
        sb.AppendJoin(string.Empty, suffix);
        return sb.ToString();
    }
}