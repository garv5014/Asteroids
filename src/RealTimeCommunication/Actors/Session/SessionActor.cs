using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;

namespace RealTimeCommunication.Actors.Session;

// In charge of talking to the lobby(game) on behalf of the user
// Return User Information.
public class SessionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string username;
    private string connectionId;

    private int? lobbyId = null;
    private SessionState state = SessionState.JoinLobby;

    private readonly IActorRef lobbySupervisor;

    public SessionActor(string username)
    {
        this.username = username;
        lobbySupervisor = Context
            .ActorSelection($"/user/{ActorHelper.LobbySupervisorName}")
            .ResolveOne(TimeSpan.FromSeconds(3))
            .Result;
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
        Receive<GetLobbiesMessage>(msg => GetLobbies(msg));
        Receive<GetLobbyStateMessage>(msg => GetLobbyState(msg));
    }

    private void GetLobbyState(GetLobbyStateMessage msg)
    {
        _log.Info("Getting lobby state");
        lobbySupervisor.Forward(msg);
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        lobbySupervisor.Forward(msg);
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        lobbyId = msg.LobbyId;
        state = SessionState.InLobby;
        connectionId = msg.ConnectionId;
        lobbySupervisor.Tell(msg);
    }

    protected override void PreStart()
    {
        _log.Info("SessionActor started");
    }

    protected override void PostStop()
    {
        _log.Info("SessionActor stopped");
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        connectionId = msg.ConnectionId;
        lobbySupervisor.Tell(msg);
    }

    public static Props Props(string username)
    {
        return Akka.Actor.Props.Create<SessionActor>(username);
    }
}

public enum SessionState
{
    JoinLobby,
    InLobby,
}
