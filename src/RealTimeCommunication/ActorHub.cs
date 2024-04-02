using Microsoft.AspNetCore.SignalR;

namespace RealTimeCommunication;

public class ActorHub : Hub<IAsteriodClient>, IActorHub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.RecieveActorMessage(message);
    }

    public Task TellActor(string user, string message)
    {
        throw new NotImplementedException();
    }
}
