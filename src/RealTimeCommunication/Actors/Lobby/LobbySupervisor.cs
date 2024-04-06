using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
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

    private IActorRef _lobbyRelayActor;

    public LobbySupervisor()
    {
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
        _lobbyRelayActor = Context
            .ActorSelection($"/user/{ActorHelper.LobbyRelayActorName}")
            .ResolveOne(TimeSpan.FromSeconds(3))
            .Result;
        Receive<GetLobbiesMessage>(msg => GetLobbies(msg));
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        var lobbiesState = new List<GameLobby>();
        foreach (var lobby in lobbies)
        {
            var gl = lobby
                .Value.Ask<GameLobby>(
                    new GetLobbiesMessage(
                        SessionActorPath: msg.SessionActorPath,
                        ConnectionId: msg.ConnectionId
                    )
                )
                .Result;
            var glId = new GameLobby(gl.Name, lobby.Key, gl.PlayerCount);
            _log.Info("Lobby: {0}", glId);
            lobbiesState.Add(glId);
        }
        _log.Info("There are {0} lobbies", lobbiesState.Count);
        _lobbyRelayActor.Tell(
            new AllLobbiesResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                Lobbies: lobbiesState ?? new List<GameLobby>()
            )
        );
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        Sender.Forward(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );
        _lobbyRelayActor.Tell(
            new JoinLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        lobbyId++;
        var lobbyActor = Context.ActorOf(
            LobbyActor.Props(msg.LobbyName),
            ActorHelper.SanitizeActorName(msg.LobbyName)
        );
        lobbies.Add(lobbyId, lobbyActor);
        _log.Info("Lobby created with id {0}", lobbyId);

        _lobbyRelayActor.Tell(
            new CreateLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName,
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
