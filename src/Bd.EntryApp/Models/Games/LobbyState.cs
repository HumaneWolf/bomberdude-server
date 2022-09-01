namespace Bd.EntryApp.Models.Games;

public record LobbyState(
    string State,
    List<LobbyPlayer> Players);