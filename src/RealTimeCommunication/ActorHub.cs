using Asteroids.Shared;
using Microsoft.AspNetCore.SignalR;

namespace RealTimeCommunication;

public class ActorHub : Hub<IAsteroidClient>, IActorHub
{
    public async Task TellActor(string user, string message)
    {
        await Clients.All.ReceiveActorMessage(message);
    }
}
