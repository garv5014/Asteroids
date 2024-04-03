using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using RealTimeCommunication.Actors.Hub;

namespace RealTimeCommunication.Actors.Session;

public class SessionSupervisor : ReceiveActor
{
    // This is the supervisor strategy for the child actors
    // Make a list of session actors
    private readonly List<IActorRef> _sessions = new();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _relayActor;

    public SessionSupervisor()
    {
        _relayActor = Context.ActorOf(
            SessionSupervisorToClientActor.Props("http://localhost:80/ws/actorHub"),
            "HubRelayActor"
        );
        Receive<SimpleMessage>(sm => HandleSimpleMessage(sm));
    }

    private void HandleSimpleMessage(SimpleMessage sm)
    {
        var message = sm.User + " says: " + sm.Message;
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor());
    }
}
