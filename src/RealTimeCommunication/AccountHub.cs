using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class AccountHub : Hub<IAsteroidClientHub>, IAccountHub
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

    public Task TellActor(string user, string message)
    {
        var sm = new SimpleMessage { Message = message, User = user };
        sessionSupervisor.Tell(sm);
        _logger.LogInformation("Message sent to actor: {0}", message);
        return Task.CompletedTask;
    }

    public Task TellClient(string message)
    {
        Clients.All.HandleActorMessage(message);
        return Task.CompletedTask;
    }

    public Task LoginCommand(LoginMessage message)
    {
        var loginMessage = new LoginMessage(message.User, message.Password, Context.ConnectionId, message.SessionActorPath);
        sessionSupervisor.Tell(loginMessage);
        _logger.LogInformation("Create account message sent to actor: {0}", message.User);
        return Task.CompletedTask;
    }

    public Task LoginPublish(LoginResponseMessage message)
    {
        Clients.Client(message.ConnectionId).HandleLoginResponse(message);
        _logger.LogInformation("Login response sent to client: {0}", message.ConnectionId);
        return Task.CompletedTask;
    }
}
