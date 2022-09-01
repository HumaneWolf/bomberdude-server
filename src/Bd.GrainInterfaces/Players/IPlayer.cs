using Bd.GrainInterfaces.Games;
using Orleans;

namespace Bd.GrainInterfaces.Players;

public interface IPlayer : IGrainWithStringKey
{
    Task SetCharacter(IPlayerCharacter? character);
    Task<IPlayerCharacter?> GetCharacter();

    Task SetName(string name);
    Task<string> GetName();
    Task HandleLeave();
}