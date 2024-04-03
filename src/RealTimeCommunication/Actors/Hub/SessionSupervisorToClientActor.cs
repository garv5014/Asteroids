using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication.Actors.Hub;

public class SessionSupervisorToClientActor : HubRelayActor
{
    private IActorHub Client;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public SessionSupervisorToClientActor(string hubUrl)
        : base(hubUrl)
    {
        Receive<SimpleMessage>(async client =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending message to client: {0}", client.Message);
                Client = hubConnection.ServerProxy<IActorHub>();
                await Client.TellClient($"{client.Message} {client.User}");
            });
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("SessionSupervisorToClientActor started");
    }

    public static Props Props(string hubUrl)
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisorToClientActor(hubUrl));
    }
}
