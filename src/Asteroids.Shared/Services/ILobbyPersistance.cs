using Akka.Actor;
using Asteroids.Shared.GameEntities;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

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

public class LobbyPersistanceService : ILobbyPersistence
{
    private readonly string lobbyKey = "lobbies";
    private readonly IGatewayClient gatewayService;

    public LobbyPersistanceService(IGatewayClient gatewayService)
    {
        this.gatewayService = gatewayService;
    }

    public Task<LobbySnapShot> GetGameInformationAsync(string lobbyName)
    {
        throw new NotImplementedException();
    }

    public async Task StoreGameInformationAsync(LobbySnapShot lobbyInformation)
    {
        VersionedValue<string> stored = await gatewayService.StrongGet(lobbyKey);
        var oldValue = JsonHelper.Deserialize<Dictionary<string, LobbySnapShot>>(stored.Value);
        var NewValue = new Dictionary<string, LobbySnapShot>(oldValue);
        NewValue[lobbyInformation.LobbyName] = lobbyInformation;
        var NewSerializedValue = JsonHelper.Serialize(oldValue);
        if (oldValue == null)
        {
            var response = await gatewayService.CompareAndSwap(
                new CompareAndSwapRequest
                {
                    Key = lobbyKey,
                    NewValue = NewSerializedValue,
                    OldValue = null
                }
            );
        }
        else
        {
            var response = await gatewayService.CompareAndSwap(
                new CompareAndSwapRequest
                {
                    Key = lobbyKey,
                    NewValue = NewSerializedValue,
                    OldValue = stored.Value
                }
            );
        }
    }
}
