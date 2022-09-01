namespace Bd.EntryApp.Models.Games;

public record LobbyPlayer(
    Guid CharacterId,
    string Name,
    string Color,
    int Wins);