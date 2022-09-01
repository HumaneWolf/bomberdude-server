namespace Bd.GrainInterfaces.Games.Models;

public record ActiveBomb(
    Guid Id,
    int X,
    int Y,
    int Power);