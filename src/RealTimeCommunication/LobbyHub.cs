using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;
using ILogger = Serilog.ILogger;

namespace RealTimeCommunication;

public class LobbyHub : Hub<ILobbyClient>, ILobbyHub
{
    private ILogger<LobbyHub> _logger;
    private IActorRef sessionSupervisor;
    public static string UrlPath = "/ws/lobbyHub";
    public static string FullUrl = $"http://nginx:80{UrlPath}";

    public LobbyHub(ILogger<LobbyHub> logger, ActorRegistry actorRegistry)
    {
        _logger = logger;
        sessionSupervisor = actorRegistry.Get<SessionSupervisor>();
    }

    public async Task LobbiesQuery(GetLobbiesMessage message)
    {
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        _logger.LogInformation("Lobbies query sent to actor");
        // More needed here
    }

    public Task LobbiesPublish(AllLobbiesResponse message)
    {
        return Task.CompletedTask;
    }

    public async Task CreateLobbyCommand(CreateLobbyMessage message)
    {
        var clc = new CreateLobbyMessage(
            SessionActorPath: message.SessionActorPath,
            LobbyName: message.LobbyName,
            ConnectionId: Context.ConnectionId
        );
        var sessionActorRef = await GetSessionActor(message.SessionActorPath);
        sessionActorRef.Tell(clc);
    }

    public Task JoinLobbyCommand(JoinLobbyMessage message)
    {
        throw new NotImplementedException();
    }

    private async Task<IActorRef> GetSessionActor(string sessionActorPath)
    {
        var gusr = await sessionSupervisor.Ask<GetUserSessionResponse>(
            new GetUserSessionMessage(ActorPath: sessionActorPath)
        );
        return gusr.ActorRef;
    }

    public Task JoinLobbyPublish(JoinLobbyResponse response)
    {
        throw new NotImplementedException();
    }

    public Task CreateLobbyPublish(CreateLobbyResponse response)
    {
        throw new NotImplementedException();
    }
}
