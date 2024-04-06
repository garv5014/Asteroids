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

    private IActorRef LobbyOwner { get; set; }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private List<IActorRef> SessionsToUpdate = new List<IActorRef>();

    public LobbyActor(string name)
    {
        lobbyName = name;
        lobbyStatus = LobbyStatus.WaitingForPlayers;
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
        Receive<GetLobbiesMessage>(msg => GetLobbies(msg));
        Receive<GetLobbyStateMessage>(msg => GetLobbyState(msg));
        // Receive<StartGameMessage>(msg => StartGame(msg));
        // Receive<EndGameMessage>(msg => EndGame(msg));
    }

    private void GetLobbyState(GetLobbyStateMessage msg)
    {
        _log.Info("Getting lobby state in Lobby Actor: {0}", Self.Path.Name);
        Context.Parent.Tell(
            new LobbyStateResponse(
                ConnectionId: msg.ConnectionId,
                SessionActorPath: msg.SessionActorPath,
                CurrentState: new LobbyState(
                    isOwner: LobbyOwner == Sender,
                    playerCount: numberOfPlayers,
                    currentStatus: lobbyStatus
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
        LobbyOwner = Sender;
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

    public static Props Props(string LobbyName)
    {
        return Akka.Actor.Props.Create(() => new LobbyActor(LobbyName));
    }
}
