using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Hosting;
using Akka.Util.Internal;
using Asteroids.Shared;

namespace RealTimeCommunication.Actors.Session;

// In charge of talking to the lobby(game) on behalf of the user
// Return User Information.
public class SessionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string username;
    private readonly string connectionId;

    private int? lobbyId = null;
    private SessionState state = SessionState.JoinLobby;

    private readonly IActorRef lobbySupervisor;

    public SessionActor(string username, string connectionId)
    {
        this.username = username;
        this.connectionId = connectionId;
        lobbySupervisor = (IActorRef)Context.AsInstanceOf<LobbySupervisor>();
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        lobbyId = msg.LobbyId;
        state = SessionState.InLobby;
        lobbySupervisor.Tell(msg);
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        lobbySupervisor.Tell(msg);
    }

    public static Props Props(string username, string connectionId)
    {
        return Akka.Actor.Props.Create<SessionActor>(username, connectionId);
    }
}

public enum SessionState
{
    JoinLobby,
    InLobby,
}
