using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Asteroids.Shared;

namespace RealTimeCommunication;

public class LobbySupervisor : ReceiveActor
{
    // return all the lobbies lobbies stored as a Dictionary of lobbies
    // Create a lobby actor
    // Forward messages to the lobby actor
    // Join Lobby passed to lobby actor

    private Dictionary<int, IActorRef> idToActorRef = new Dictionary<int, IActorRef>();
    private Dictionary<string, IActorRef> nameToActorRef = new Dictionary<string, IActorRef>();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int lobbyId = 0;

    private IActorRef _lobbyRelayActor;

    public LobbySupervisor(IActorRef testRelayActorRef = null)
    {
        Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));

        if (testRelayActorRef != null)
        {
            _lobbyRelayActor = testRelayActorRef;
        }
        else
        {
            _lobbyRelayActor = Context
                .ActorSelection($"/user/{ActorHelper.LobbyRelayActorName}")
                .ResolveOne(TimeSpan.FromSeconds(3))
                .Result;
        }
        Receive<GetLobbiesMessage>(msg => GetLobbies(msg));
        Receive<GetLobbyStateMessage>(msg => GetLobbyState(msg));
        Receive<LobbyStateResponse>(msg => HandleStateResponse(msg));
    }

    private void HandleStateResponse(LobbyStateResponse msg)
    {
        _lobbyRelayActor.Tell(msg);
    }

    private void GetLobbyState(GetLobbyStateMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }
        _log.Info("Getting lobby state from child");
        lobbyActor.Tell(msg);
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        var lobbiesState = new List<GameLobby>();
        foreach (var lobby in idToActorRef)
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
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }

        lobbyActor.Forward(
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
        if (nameToActorRef.ContainsKey(msg.LobbyName))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with name {0} already exists", msg.LobbyName);
            return;
        }
        lobbyId++;

        var lobbyActor = Context.ActorOf(
            LobbyActor.Props(msg.LobbyName, msg.SessionActorPath),
            ActorHelper.SanitizeActorName(msg.LobbyName)
        );

        idToActorRef.Add(lobbyId, lobbyActor);
        nameToActorRef.Add(msg.LobbyName, lobbyActor);

        _log.Info("Lobby created with id {0}", lobbyId);

        Self.Tell(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );

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

    public static Props Props(IActorRef actorRef = null)
    {
        return Akka.Actor.Props.Create(() => new LobbySupervisor(actorRef));
    }
}
