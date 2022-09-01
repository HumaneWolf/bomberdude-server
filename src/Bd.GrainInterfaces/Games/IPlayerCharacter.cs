using Bd.GrainInterfaces.Players;
using Orleans;

namespace Bd.GrainInterfaces.Games;

public interface IPlayerCharacter : IGrainWithGuidKey
{
    Task ResetStats();
    
    Task AddToLobby(IGameLobby lobby);
    Task RemoveLobby();
    Task<IGameLobby> GetLobby();

    Task SetPlayer(IPlayer player);
    Task RemovePlayer();
    Task<IPlayer> GetPlayer();

    Task SetHealth(int health);
    Task<int> GetHealth();

    Task SetColor(string hex);
    Task<string> GetColor();

    Task SetPosition(double x, double y);
    Task<(double, double)> GetPosition();
    Task<(int, int)> GetTilePosition();

    Task IncreasePower();
    Task<int> GetPower();

    Task SetBombCooldown();
    Task<bool> CanDropBomb();

    Task AddWin();
    Task<int> GetWins();
}