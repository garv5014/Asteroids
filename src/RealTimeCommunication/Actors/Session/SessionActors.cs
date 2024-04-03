using Akka.Actor;

namespace RealTimeCommunication.Actors.Session;

public class SessionActors : ReceiveActor
{
    public SessionActors() { }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionActors());
    }
}
