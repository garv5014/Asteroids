using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using RealTimeCommunication.Actors.Hub;

namespace RealTimeCommunication.Actors.Session;

public class SessionSupervisor : ReceiveActor
{
    // This is the supervisor strategy for the child actors
    // Make a list of session actors
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _relayActor;

    public SessionSupervisor()
    {
        _log.Info("SessionSupervisor created");
        _relayActor = Context.ActorOf(
            PublishToClientActor.Props("http://nginx:80/ws/actorHub"),
            "SessionSupervisorToClientActor"
        );
        Receive<SimpleMessage>(sm => HandleSimpleMessage(sm));
    }

    private void HandleSimpleMessage(SimpleMessage sm)
    {
        _log.Info("Received message: {0}", sm.Message);
        _relayActor.Tell(sm);
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor());
    }
}
