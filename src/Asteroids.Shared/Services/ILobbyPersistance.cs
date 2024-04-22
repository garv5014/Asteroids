using Akka.Actor;
using Asteroids.Shared.GameEntities;

namespace Asteroids.Shared.Services;

public interface ILobbyPersistence
{
    Task StoreGameInformationAsync(LobbySnapShot lobbyInformation);
    Task<LobbySnapShot> GetGameInformationAsync(string lobbyName);
}

public class LobbySnapShot
{
    public string LobbyName { get; set; }
    public int NumberOfPlayers { get; set; }
    public LobbyStatus LobbyStatus { get; set; }
    public Game GameState { get; set; }
    public string LobbyOwner { get; set; }
    public List<IActorRef> SessionsToUpdate { get; set; }

    public LobbySnapShot(
        string lobbyName,
        int numberOfPlayers,
        LobbyStatus lobbyStatus,
        Game gameState,
        string lobbyOwner,
        List<IActorRef> sessionsToUpdate
    )
    {
        LobbyName = lobbyName;
        NumberOfPlayers = numberOfPlayers;
        LobbyStatus = lobbyStatus;
        GameState = gameState;
        LobbyOwner = lobbyOwner;
        SessionsToUpdate = sessionsToUpdate;
    }
}

public record StoreLobbyInformationMessage(LobbySnapShot LobbySnapShot);
