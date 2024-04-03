using Akka.Actor;
using Akka.Hosting;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class ActorHub : Hub<IAsteroidClientHub>, IActorHub
{
    private readonly IActorRef sessionSupervisor;
    private readonly ILogger<Hub> _logger;

    public ActorHub(ILogger<Hub> logger, ActorRegistry actorRegistry)
    {
        sessionSupervisor = actorRegistry.Get<SessionSupervisor>();
        _logger = logger;
    }

    public Task TellActor(string user, string message)
    {
        var sm = new SimpleMessage { Message = message, User = user };
        sessionSupervisor.Tell(sm);
        return Task.CompletedTask;
    }

    public Task TellClient(string message)
    {
        Clients.All.ReceiveActorMessage(message);
        return Task.CompletedTask;
    }
}
