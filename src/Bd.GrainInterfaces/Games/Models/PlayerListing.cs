namespace Bd.GrainInterfaces.Games.Models;

public record PlayerListing(
    Guid CharacterId,
    string Name,
    string Color,
    bool IsAlive,
    int Health,
    double PositionX,
    double PositionY,
    int Wins);