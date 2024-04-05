using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor
{
    // Lobby actor is in charge of creating and managing lobbies
    // respond to messages from the lobby supervisor after creation.
    // Add user to lobby'

    private string lobbyName { get; init; }
    private int numberOfPlayers { get; init; }
    private LobbyState lobbyState { get; set; }

    private IActorRef LobbyOwner { get; set; }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private List<IActorRef> SessionsToUpdate = new List<IActorRef>();

    public LobbyActor(string name)
    {
        lobbyName = name;
        lobbyState = LobbyState.WaitingForPlayers;
        Receive<CreateLobbyMessageWithId>(msg => CreateLobby(msg));
        Receive<JoinLobbyMessage>(msg => JoinLobby(msg));
        // Receive<StartGameMessage>(msg => StartGame(msg));
        // Receive<EndGameMessage>(msg => EndGame(msg));
    }

    private void CreateLobby(CreateLobbyMessageWithId msg)
    {
        SessionsToUpdate.Add(Sender); // should be the session Actor.
        LobbyOwner = Sender;
        _log.Info("Lobby created");
        Sender.Tell(
            new JoinLobbyMessage(
                ConnectionId: msg.ConnectionId,
                LobbyId: msg.LobbyId,
                SessionActorPath: msg.SessionActorPath
            )
        );
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        throw new NotImplementedException();
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

public enum LobbyState
{
    WaitingForPlayers,
    InGame,
    GameOver
}
