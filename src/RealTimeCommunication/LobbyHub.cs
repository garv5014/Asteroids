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

  public Task LobbiesQuery(GetLobbiesMessage message)
  {
    var response = sessionSupervisor.Ask<GetUserSessionResponse>(message);
    _logger.LogInformation("Lobbies query sent to actor");
    return Task.CompletedTask;
  }

  public Task LobbiesPublish(AllLobbiesResponse message)
  {
    return Task.CompletedTask;
  }
}