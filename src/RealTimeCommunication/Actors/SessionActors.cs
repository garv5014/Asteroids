using Akka.Actor;

namespace RealTimeCommunication.Actors;

public class SessionActors : ReceiveActor
{
    public SessionActors() { }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionActors());
    }
}
