using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Hosting;
using Asteroids.Shared;

namespace RealTimeCommunication.Actors.Session;

// In charge of talking to the lobby(game) on behalf of the user
// Return User Information.
public class SessionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string username;
    private readonly string connectionId;

    private readonly IActorRef lobbySupervisor;

    public SessionActor(string username, string connectionId, ActorRegistry actorRegistry)
    {
        this.username = username;
        this.connectionId = connectionId;
        lobbySupervisor = (IActorRef)Context.ActorSelection(ActorHelper.LobbySupervisorName);
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        throw new NotImplementedException();
    }

    public static Props Props(string username, string connectionId)
    {
        return Akka.Actor.Props.Create<SessionActor>(username, connectionId);
    }
}
