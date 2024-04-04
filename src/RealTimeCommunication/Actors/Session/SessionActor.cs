using Akka.Actor;
using Akka.Event;

namespace RealTimeCommunication.Actors.Session;

// In charge of talking to the lobby(game) on behalf of the user
public class SessionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string username;
    private readonly string connectionId;

    public SessionActor(string username, string connectionId)
    {
        this.username = username;
        this.connectionId = connectionId;
    }

    public static Props Props(string username, string connectionId)
    {
        return Akka.Actor.Props.Create<SessionActor>(username, connectionId);
    }
}
