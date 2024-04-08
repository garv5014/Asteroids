using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class LobbyHub : Hub<ILobbyClient>, ILobbyHub
{
    private ILogger<LobbyHub> _logger;
    private IActorRef sessionSupervisor;
    public static string UrlPath = "/lobbyHub";
    public static string FullUrl = $"http://realtime:8080{UrlPath}";

    public LobbyHub(ILogger<LobbyHub> logger, ActorRegistry actorRegistry)
    {
        _logger = logger;
        sessionSupervisor = actorRegistry.Get<SessionSupervisor>();
    }

    public async Task LobbiesQuery(GetLobbiesMessage message)
    {
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        _logger.LogInformation("Lobbies query sent to actor");
        sessionActorRef.Tell(message);
        // More needed here
    }

    public Task LobbiesPublish(AllLobbiesResponse message)
    {
        _logger.LogInformation("Sending lobby response to client: {0}", message.ConnectionId);
        return Clients.All.HandleLobbiesResponse(message);
    }

    public async Task CreateLobbyCommand(CreateLobbyMessage message)
    {
        _logger.LogInformation("Create lobby command received");
        var clc = new CreateLobbyMessage(
            SessionActorPath: message.SessionActorPath,
            LobbyName: message.LobbyName,
            ConnectionId: Context.ConnectionId
        );
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        sessionActorRef.Tell(clc);
    }

    public async Task JoinLobbyCommand(JoinLobbyMessage message)
    {
        _logger.LogInformation("Join lobby command received");
        var jlc = new JoinLobbyMessage(
            SessionActorPath: message.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyId: message.LobbyId
        );
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        sessionActorRef.Tell(jlc);
    }

    private async Task<IActorRef> GetSessionActor(string sessionActorPath)
    {
        var gusr = await sessionSupervisor.Ask<GetUserSessionResponse>(
            new GetUserSessionMessage(ActorPath: sessionActorPath)
        );
        return gusr.ActorRef;
    }

    public async Task JoinLobbyPublish(JoinLobbyResponse response)
    {
        _logger.LogInformation("Sending response to client: {0}", response.ConnectionId);
        await Clients.Client(response.ConnectionId).HandleJoinLobbyResponse(response);
    }

    public async Task CreateLobbyPublish(CreateLobbyResponse response)
    {
        _logger.LogInformation("Sending response to client: {0}", response.ConnectionId);
        await Clients.All.HandleCreateLobbyResponse(response);
    }

    public async Task LobbyStateQuery(GetLobbyStateMessage message)
    {
        _logger.LogInformation("Lobby state query received");
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        var lsm = new GetLobbyStateMessage(
            SessionActorPath: message.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyId: message.LobbyId
        );
        sessionActorRef.Tell(lsm);
    }

    public Task LobbyStatePublish(LobbyStateResponse response)
    {
        _logger.LogInformation(
            "Sending {0} response to client : {1} Is owner is {2}",
            nameof(LobbyStateResponse),
            response.ConnectionId,
            response.CurrentState.IsOwner
        );
        return Clients.Client(response.ConnectionId).HandleLobbyStateResponse(response);
    }

    public Task UpdateLobbyStateCommand(UpdateLobbyMessage message)
    {
        throw new NotImplementedException();
    }
}
