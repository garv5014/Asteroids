using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;

namespace RealTimeCommunication.Actors.Session;

// In charge of talking to the lobby(game) on behalf of the user
// Return User Information.

public record RefreshConnectionId(string ConnectionId);

public class SessionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string username;
    private string connectionId;

    private int? lobbyId = null;
    private SessionState state = SessionState.JoinLobby;

    private readonly IActorRef lobbySupervisor;

    public SessionActor(string username, IActorRef lobbySupervisor)
    {
        this.username = username;
        this.lobbySupervisor = lobbySupervisor;
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
        Receive<GetLobbiesMessage>(msg => GetLobbies(msg));
        Receive<GetLobbyStateMessage>(msg => GetLobbyState(msg));
        Receive<RefreshConnectionId>(msg => ConnectionIdRefresh(msg));
        Receive<LobbyStateResponse>(msg => PassToLobbySupervisor(msg));
    }

    private void PassToLobbySupervisor(LobbyStateResponse msg)
    {
        _log.Info("Passing lobby state to LobbySupervisor for {0}", Self.Path.Name);
        var mes = new LobbyStateResponse(this.connectionId, msg.SessionActorPath, msg.CurrentState);
        lobbySupervisor.Tell(mes);
    }

    private void ConnectionIdRefresh(RefreshConnectionId msg)
    {
        _log.Info("ConnectionId Refreshed for {0}", Self.Path.Name);
        this.connectionId = msg.ConnectionId;
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

    public static Props Props(string username, IActorRef LobbySupervisor)
    {
        return Akka.Actor.Props.Create<SessionActor>(username, LobbySupervisor);
    }
}

public enum SessionState
{
    JoinLobby,
    InLobby,
}
