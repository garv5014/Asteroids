using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor, IWithTimers
{
    private string LobbyName { get; init; }
    private int NumberOfPlayers
    {
        get => _sessionsToUpdate.Count;
    }

    private LobbyStatus LobbyStatus { get; set; }

    private Game GameState { get; set; }

    private string LobbyOwner { get; init; }
    public ITimerScheduler Timers { get; set; }

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private List<IActorRef> _sessionsToUpdate = [];
    private readonly Random _random = new();

    public LobbyActor(string name, string owner)
    {
        LobbyName = name;
        LobbyOwner = owner;
        GameState = new Game(600, 600);
        _log.Info("Lobby created with name: {0} and owner {1}", LobbyName, LobbyOwner);
        LobbyStatus = LobbyStatus.WaitingForPlayers;
        Receive<JoinLobbyMessage>(JoinLobby);
        Receive<GetLobbiesMessage>(GetLobbies);
        Receive<GetLobbyStateMessage>(GetLobbyState);
        Receive<UpdateLobbyMessage>(UpdateLobby);
        Receive<UpdateShipMessage>(UpdateShip);
        Receive<GameLoopMessage>(GameLoop);
        Receive<SpawnAsteroidMessage>(SpawnAsteroids);
    }

    private void GameLoop(GameLoopMessage message)
    {
        if (LobbyStatus.Equals(LobbyStatus.InGame))
        {
            _log.Info("Game loop tick");
            GameState.Tick();
            UpdateClients();
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        if (GameState.GetShips().Count == 0)
        {
            _log.Info("Game over");
            LobbyStatus = LobbyStatus.GameOver;
            Timers.Cancel("gameLoop");
            Timers.Cancel("spawnAsteroid");
        }
    }

    private void UpdateClients()
    {
        _log.Info("Updating clients");

        foreach (var session in _sessionsToUpdate)
        {
            _log.Info("Sending lobby state to {0}", session.Path);
            session.Tell(
                new LobbyStateResponse(
                    ConnectionId: "",
                    SessionActorPath: session.Path.ToString(),
                    CurrentState: GameState.LobbyStateSnapShot(LobbyStatus)
                )
            );
        }
    }

    private void SpawnAsteroids(SpawnAsteroidMessage obj)
    {
        GameState.SpawnAsteroids();
        ScheduleNextAsteroidSpawn();
    }

    private void ScheduleNextAsteroidSpawn()
    {
        var nextSpawnInMilliseconds = _random.Next(500, 3000); // Random interval between spawns
        Timers.StartSingleTimer(
            "spawnAsteroid",
            new SpawnAsteroidMessage(),
            TimeSpan.FromMilliseconds(nextSpawnInMilliseconds)
        );
    }

    private void UpdateShip(UpdateShipMessage obj)
    {
        if (LobbyStatus != LobbyStatus.InGame)
            return;

        // Update ship movement in game state
        GameState.UpdateShip(obj.SessionActorPath, obj.ShipParams);
    }

    private void UpdateLobby(UpdateLobbyMessage msg)
    {
        if (LobbyOwner == msg.SessionActorPath)
        {
            LobbyStatus = msg.NewStatus;
            _log.Info("Lobby status updated to {0}", msg.NewStatus);
        }
        else
        {
            _log.Info("User {0} is not the owner of the lobby", msg.SessionActorPath);
        }

        if (msg.NewStatus == LobbyStatus.InGame)
        {
            Context.Parent.Tell(
                new LobbyStateResponse(
                    ConnectionId: msg.ConnectionId,
                    SessionActorPath: msg.SessionActorPath,
                    CurrentState: GameState.LobbyStateSnapShot(LobbyStatus)
                )
            );

            Timers.StartPeriodicTimer(
                "gameLoop",
                new GameLoopMessage(),
                TimeSpan.FromMilliseconds(50),
                TimeSpan.FromMilliseconds(100)
            );

            // Start spawning asteroids
            ScheduleNextAsteroidSpawn();
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
                    playerCount: NumberOfPlayers,
                    currentStatus: LobbyStatus,
                    ships: [],
                    asteroids: []
                )
            )
        );
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        _log.Info("Getting lobbies in Lobby Actor: {0}", Self.Path.Name);
        var gl = new GameLobby(LobbyName, 0, NumberOfPlayers);
        Sender.Tell(gl);
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        _sessionsToUpdate.Add(Sender); // should be the session Actor.
        GameState.AddShip(
            msg.SessionActorPath,
            new Ship(xCoordinate: 0, yCoordinate: 0, rotation: 0)
        );
        _log.Info("Lobby Joined by {0}", Sender.Path.Name);
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

internal record GameLoopMessage { }

internal record SpawnAsteroidMessage { }
