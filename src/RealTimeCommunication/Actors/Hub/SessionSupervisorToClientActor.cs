using Akka.Actor;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication.Actors.Hub;

public class SessionSupervisorToClientActor : HubRelayActor
{
    private IAsteroidClient Client;

    public SessionSupervisorToClientActor(string hubUrl)
        : base(hubUrl)
    {
        Receive<SimpleMessage>(client =>
        {
            Client = hubConnection.ServerProxy<IAsteroidClient>();
            Client.ReceiveActorMessage(client.Message + " " + client.User);
        });
    }

    public static Props Props(string hubUrl)
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisorToClientActor(hubUrl));
    }
}
