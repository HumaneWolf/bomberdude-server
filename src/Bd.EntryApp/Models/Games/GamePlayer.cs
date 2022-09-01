namespace Bd.EntryApp.Models.Games;

public record GamePlayer(
    Guid CharacterId,
    string Name,
    string Color,
    int Wins,
    bool IsAlive,
    int Health,
    double PositionX,
    double PositionY
) : LobbyPlayer(CharacterId, Name, Color, Wins);