using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;

namespace RealTimeCommunication;

// public record CreateLobbyMessageWithId(
//     string SessionActorPath,
//     string ConnectionId,
//     string LobbyName,
//     int LobbyId
// )
//     : CreateLobbyMessage(
//         SessionActorPath: SessionActorPath,
//         LobbyName: LobbyName,
//         ConnectionId: ConnectionId
//     );

public class LobbySupervisor : ReceiveActor
{
    // return all the lobbies lobbies stored as a Dictionary of lobbies
    // Create a lobby actor
    // Forward messages to the lobby actor
    // Join Lobby passed to lobby actor

    private Dictionary<int, IActorRef> lobbies = new Dictionary<int, IActorRef>();

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int lobbyId = 0;

    public LobbySupervisor()
    {
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        var lobbyActor = Context.ActorOf(LobbyActor.Props(msg.LobbyName), msg.LobbyName);
        lobbies.Add(lobbyId, lobbyActor);
        lobbyId++;
        lobbyActor.Forward(
            new CreateLobbyMessage(msg.SessionActorPath, msg.ConnectionId, msg.LobbyName)
        );
        Sender.Tell(
            new CreateLobbyResponse(
                ConnectionId: msg.ConnectionId,
                SessionActorPath: msg.SessionActorPath,
                lobbyId
            )
        );
        Sender.Tell(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );
    }

    protected override void PreStart()
    {
        _log.Info("LobbySupervisor created");
    }

    protected override void PostStop()
    {
        _log.Info("LobbySupervisor stopped");
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new LobbySupervisor());
    }
}
