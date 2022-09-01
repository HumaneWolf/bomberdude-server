namespace Bd.GrainInterfaces.Games.Models;

public record LobbyStateUpdate(
    GameState State,
    List<PlayerListing> Players);