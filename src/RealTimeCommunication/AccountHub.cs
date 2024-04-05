using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class AccountHub : Hub<IAccountClient>, IAccountHub
{
    private readonly IActorRef sessionSupervisor;
    private readonly ILogger<AccountHub> _logger;
    public static string UrlPath = "/ws/accountHub";
    public static string FullUrl = $"http://nginx:80{UrlPath}";

    public AccountHub(ILogger<AccountHub> logger, ActorRegistry actorRegistry)
    {
        sessionSupervisor = actorRegistry.Get<SessionSupervisor>();
        _logger = logger;
    }

    public Task LoginCommand(LoginMessage message)
    {
        var loginMessage = new LoginMessage(
            message.User,
            message.Password,
            Context.ConnectionId,
            message.SessionActorPath
        );
        sessionSupervisor.Tell(loginMessage);
        _logger.LogInformation("Create account message sent to actor: {0}", message.User);
        return Task.CompletedTask;
    }

    public async Task LoginPublish(LoginResponseMessage message)
    {
        await Clients.Client(message.ConnectionId).HandleLoginResponse(message);
        _logger.LogInformation("Login response sent to client: {0}", message.ConnectionId);
    }
}
