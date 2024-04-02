using Akka.Actor;

namespace RealTimeCommunication.Actors;

public class SessionSupervisor : ReceiveActor
{
    // This is the supervisor strategy for the child actors
    // Make a list of session actors
    private readonly List<IActorRef> _sessions = new();

    public SessionSupervisor() { }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor());
    }
}
