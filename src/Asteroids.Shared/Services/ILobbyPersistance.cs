using Akka.Actor;
using Asteroids.Shared.GameEntities;
using Microsoft.Extensions.Logging;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Asteroids.Shared.Services;

public interface ILobbyPersistence
{
    Task StoreGameInformationAsync(LobbySnapShot lobbyInformation);
    Task<Dictionary<string, LobbySnapShot>> GetGameInformationAsync();
}

public class LobbySnapShot
{
    public string LobbyName { get; set; }
    public int NumberOfPlayers { get; set; }
    public LobbyStatus LobbyStatus { get; set; }
    public Game GameState { get; set; }
    public string LobbyOwner { get; set; }
    public List<string> SessionsToUpdate { get; set; }

    public LobbySnapShot(
        string lobbyName,
        int numberOfPlayers,
        LobbyStatus lobbyStatus,
        Game gameState,
        string lobbyOwner,
        List<string> sessionsToUpdate
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

public record GetLobbyInformationMessage();

public class LobbyPersistanceService : ILobbyPersistence
{
    private readonly string lobbyKey = "lobbies";
    private readonly IGatewayClient _gatewayService;
    private readonly ILogger<LobbyPersistanceService> _logger;

    public LobbyPersistanceService(
        IGatewayClient gatewayService,
        ILogger<LobbyPersistanceService> logger
    )
    {
        _gatewayService = gatewayService;
        _logger = logger;
    }

    // lobbies are stored in a dictionary with the key being the lobby name
    public async Task StoreGameInformationAsync(LobbySnapShot lobbyInformation)
    {
        _logger.LogInformation("Storing lobby information for {0}", lobbyInformation.LobbyName);
        VersionedValue<string> stored = await _gatewayService.StrongGet(lobbyKey);
        var oldValue = JsonHelper.Deserialize<Dictionary<string, LobbySnapShot>>(stored.Value);
        var NewValue = new Dictionary<string, LobbySnapShot>(oldValue);
        NewValue[lobbyInformation.LobbyName] = lobbyInformation;
        var NewSerializedValue = JsonHelper.Serialize(NewValue);
        if (oldValue == null)
        {
            var response = await _gatewayService.CompareAndSwap(
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
            var response = await _gatewayService.CompareAndSwap(
                new CompareAndSwapRequest
                {
                    Key = lobbyKey,
                    NewValue = NewSerializedValue,
                    OldValue = stored.Value
                }
            );
        }
    }

    public async Task<Dictionary<string, LobbySnapShot>> GetGameInformationAsync()
    {
        var lobbies = await _gatewayService.StrongGet(lobbyKey);
        return JsonHelper.Deserialize<Dictionary<string, LobbySnapShot>>(lobbies.Value);
    }
}
