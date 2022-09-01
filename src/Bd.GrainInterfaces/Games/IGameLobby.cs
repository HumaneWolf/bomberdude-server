using Bd.GrainInterfaces.Games.Models;
using Orleans;

namespace Bd.GrainInterfaces.Games;

public interface IGameLobby : IGrainWithStringKey
{
    Task<Guid> GetStreamId();
    
    Task StartGame();
    Task PlayAgain();
    Task<GameState> GetState();
    
    Task GenerateMap();
    
    Task AddPlayer(IPlayerCharacter character);
    Task RemovePlayer(IPlayerCharacter character);
    Task<List<PlayerListing>> GetPlayers();

    Task SetTileType(int x, int y, TileType type);
    Task<TileType> GetTileType(int x, int y);
    Task<List<List<TileType>>> GetMap();
    
    Task<bool> ExplodeTile(int x, int y);
    Task ExplodeFromTile(int x, int y, int power);
    Task DropBomb(IPlayerCharacter character);
    Task<List<ActiveBomb>> GetBombs();
    
    Task CheckWinner();
    Task<PlayerListing?> GetWinner();

    Task<bool> IsInMap(double x, double y);
}