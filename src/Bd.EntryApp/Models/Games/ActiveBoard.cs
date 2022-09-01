namespace Bd.EntryApp.Models.Games;

public class ActiveBoard
{
    public LobbyPlayer? Winner { get; set; }
    
    public List<List<string>> Map { get; set; }
    public List<ActiveBomb> Bombs { get; set; }
    public List<GamePlayer> Players { get; set; }

    public ActiveBoard(LobbyPlayer? winner, List<List<string>> map, List<ActiveBomb> bombs, List<GamePlayer> players)
    {
        Winner = winner;
        Map = map;
        Bombs = bombs;
        Players = players;
    }
}