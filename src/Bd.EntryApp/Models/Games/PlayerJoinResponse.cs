namespace Bd.EntryApp.Models.Games;

public record PlayerJoinResponse(
    bool IsJoined,
    string? Message,
    Guid? CharacterId,
    string? JoinCode,
    List<LobbyPlayer>? Players,
    int? MaxPlayers);