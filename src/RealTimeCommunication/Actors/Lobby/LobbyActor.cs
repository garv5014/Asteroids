using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor
{
    // Lobby actor is in charge of creating and managing lobbies
    // respond to messages from the lobby supervisor after creation.
    // Add user to lobby'

    private string lobbyName { get; init; }

    private int numberOfPlayers
    {
        get => SessionsToUpdate.Count;
    }

    private LobbyStatus lobbyStatus { get; set; }

    private string LobbyOwner { get; init; }

    private LobbyState lobbyState;

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private List<IActorRef> SessionsToUpdate = new List<IActorRef>();
    private Dictionary<string, Ship> Ships = new Dictionary<string, Ship>();

    public LobbyActor(string name, string owner)
    {
        lobbyName = name;
        LobbyOwner = owner;
        _log.Info("Lobby created with name: {0} and owner {1}", lobbyName, LobbyOwner);
        lobbyStatus = LobbyStatus.WaitingForPlayers;
        Receive<JoinLobbyMessage>(JoinLobby);
        Receive<GetLobbiesMessage>(GetLobbies);
        Receive<GetLobbyStateMessage>(GetLobbyState);
        Receive<UpdateLobbyMessage>(UpdateLobby);
    }

    private void UpdateLobby(UpdateLobbyMessage msg)
    {
        if (LobbyOwner == msg.SessionActorPath)
        {
            lobbyStatus = msg.NewStatus;
            _log.Info("Lobby status updated to {0}", msg.NewStatus);
        }
        else
        {
            _log.Info("User {0} is not the owner of the lobby", msg.SessionActorPath);
        }

        if (msg.NewStatus == LobbyStatus.InGame)
        {
            lobbyState = new LobbyState(
                isOwner: LobbyOwner == msg.SessionActorPath,
                playerCount: numberOfPlayers,
                currentStatus: lobbyStatus,
                ships: [new Ship(id: 0, xCoordinate: 10, yCoordinate: 10, rotation: 45)],
                asteroids: []
            );

            Context.Parent.Tell(
                new LobbyStateResponse(
                    ConnectionId: msg.ConnectionId,
                    SessionActorPath: msg.SessionActorPath,
                    CurrentState: lobbyState
                )
            );
        }
    }

    private void GetLobbyState(GetLobbyStateMessage msg)
    {
        _log.Info(
            "Getting lobby state in Lobby Actor: {0} for user actor {1} here is the owner {2}",
            Self.Path.Name,
            msg.SessionActorPath,
            LobbyOwner
        );
        Context.Parent.Tell(
            new LobbyStateResponse(
                ConnectionId: msg.ConnectionId,
                SessionActorPath: msg.SessionActorPath,
                CurrentState: new LobbyState(
                    isOwner: LobbyOwner == msg.SessionActorPath,
                    playerCount: numberOfPlayers,
                    currentStatus: lobbyStatus,
                    ships: [],
                    asteroids: []
                )
            )
        );
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        _log.Info("Getting lobbies in Lobby Actor: {0}", Self.Path.Name);
        var gl = new GameLobby(lobbyName, 0, numberOfPlayers);
        Sender.Tell(gl);
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        SessionsToUpdate.Add(Sender); // should be the session Actor.
        _log.Info("Lobby created");
    }

    protected override void PreStart()
    {
        _log.Info("LobbyActor created");
    }

    protected override void PostStop()
    {
        _log.Info("LobbyActor stopped");
    }

    public static Props Props(string LobbyName, string LobbyOwner)
    {
        return Akka.Actor.Props.Create(() => new LobbyActor(LobbyName, LobbyOwner));
    }
}
