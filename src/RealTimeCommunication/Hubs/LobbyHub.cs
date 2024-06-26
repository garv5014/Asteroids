using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class LobbyHub : Hub<ILobbyClient>, ILobbyHub
{
    private ILogger<LobbyHub> _logger;
    private IActorRef sessionSupervisor;
    private IActorRef lobbySupervisor;
    public static string UrlPath = "/lobbyHub";
    public static string FullUrl = $"http://realtime:8080{UrlPath}";

    public LobbyHub(ILogger<LobbyHub> logger, ActorRegistry actorRegistry)
    {
        _logger = logger;
        sessionSupervisor = actorRegistry.Get<SessionSupervisor>();
        lobbySupervisor = actorRegistry.Get<LobbySupervisor>();
    }

    public async Task LobbiesQuery(GetLobbiesMessage message)
    {
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        // _logger.LogInformation("Lobbies query sent to actor");
        sessionActorRef.Tell(message);
        // More needed here
    }

    public Task LobbiesPublish(AllLobbiesResponse message)
    {
        // _logger.LogInformation("Sending lobby response to client: {0}", message.ConnectionId);
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

    public async Task JoinLobbyCommand(JoinLobbyMessage msg)
    {
        _logger.LogInformation("Join lobby command received");
        var jlc = new JoinLobbyMessage(
            SessionActorPath: msg.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyName: msg.LobbyName
        );
        var sessionActorRef = await GetSessionActor(msg.SessionActorPath);
        sessionActorRef.Tell(jlc);
    }

    private async Task<IActorRef> GetSessionActor(string sessionActorPath)
    {
        var gusr = await sessionSupervisor.Ask<GetUserSessionResponse>(
            new GetUserSessionMessage(ActorPath: sessionActorPath, Context.ConnectionId)
        );
        _logger.LogInformation("Session actor ref received {0}", gusr.ActorRef.Path.ToString());
        return gusr.ActorRef;
    }

    public async Task JoinLobbyPublish(JoinLobbyResponse response)
    {
        _logger.LogInformation("Sending response to client: {0}", response.ConnectionId);
        await Clients.Client(response.ConnectionId).HandleJoinLobbyResponse(response);
    }

    public async Task CreateLobbyPublish(CreateLobbyResponse response)
    {
        _logger.LogInformation("CreateLobbyResponse {0}", response.ConnectionId);
        await Clients.All.HandleCreateLobbyResponse(response);
    }

    public async Task LobbyStateQuery(GetLobbyStateMessage message)
    {
        _logger.LogInformation("Lobby state query received");
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        var lsm = new GetLobbyStateMessage(
            SessionActorPath: message.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyName: message.LobbyName
        );
        sessionActorRef.Tell(lsm);
    }

    public Task LobbyStatePublish(LobbyStateResponse response)
    {
        // _logger.LogInformation(
        //     "Sending {0} response to client : {1} Is owner is {2}",
        //     nameof(LobbyStateResponse),
        //     response.ConnectionId,
        //     response.CurrentState.IsOwner
        // );
        Clients.All.HandleRefreshConnectionId();
        return Clients.Client(response.ConnectionId).HandleLobbyStateResponse(response);
    }

    public async Task UpdateLobbyStateCommand(UpdateLobbyMessage message)
    {
        _logger.LogInformation("Update lobby state command received");

        var mes = new UpdateLobbyMessage(
            SessionActorPath: message.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyName: message.LobbyName,
            NewStatus: message.NewStatus
        );

        var lobbyActorRef = await GetLobbyByName(message.LobbyName);
        lobbyActorRef.Tell(mes);
    }

    public async Task UpdateShipCommand(UpdateShipMessage message)
    {
        // _logger.LogInformation("Update ship command received");

        var lobbyActorRef = await GetLobbyByName(message.LobbyName);
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);

        sessionActorRef.Tell(new RefreshConnectionId(ConnectionId: Context.ConnectionId));
        var mes = new UpdateShipMessage(
            ConnectionId: Context.ConnectionId,
            SessionActorPath: message.SessionActorPath,
            ShipParams: message.ShipParams,
            LobbyName: message.LobbyName
        );

        lobbyActorRef.Tell(mes);
    }

    public async Task RefreshConnectionIdCommand(RefreshConnectionIdMessage message)
    {
        // _logger.LogInformation("Refresh connection id command received");
        var mes = new RefreshConnectionId(ConnectionId: Context.ConnectionId);
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        sessionActorRef.Tell(mes);
    }

    public async Task KillLobbyCommand(KillLobbyMessage message)
    {
        _logger.LogInformation("Kill lobby command received");
        var lobby = await GetLobbyByName(message.LobbyName);
        var newMsg = new KillLobbyMessage(
            SessionActorPath: message.SessionActorPath,
            ConnectionId: Context.ConnectionId,
            LobbyName: message.LobbyName
        );

        lobby.Tell(newMsg);
    }

    private async Task<IActorRef> GetLobbyByName(string lobbyName)
    {
        // _logger.LogInformation("Getting lobby name {0}", lobbyName);
        var res = await lobbySupervisor.Ask<GetLobbyResponse>(
            new GetLobbyMessage(LobbyName: lobbyName)
        );
        return res.LobbyActorRef;
    }
}
