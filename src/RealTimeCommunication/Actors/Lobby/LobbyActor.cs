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
        Receives();
    }

    private void Receives()
    {
        Receive<JoinLobbyMessage>(JoinLobby);
        Receive<GetLobbiesMessage>(GetLobbies);
        Receive<GetLobbyStateMessage>(GetLobbyState);
        Receive<UpdateLobbyMessage>(UpdateLobby);
        Receive<UpdateShipMessage>(UpdateShip);
        Receive<GameLoopMessage>(GameLoop);
        Receive<SpawnAsteroidMessage>(SpawnAsteroids);
        Receive<ErrorMessage>(msg => Context.Parent.Tell(msg));
        Receive<KillLobbyMessage>(StopActor);
    }

    private void StopActor(KillLobbyMessage message)
    {
        _log.Info("Killing lobby actor");
        Context.Parent.Tell(new ErrorMessage("Lobby has been killed", message.ConnectionId));
        Context.Stop(Self);
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
            UpdateClients();
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
                    CurrentState: GameState.ToGameSnapShot(
                        LobbyStatus,
                        session.Path.ToString() == LobbyOwner
                    )
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
                    CurrentState: GameState.ToGameSnapShot(
                        LobbyStatus,
                        isOwner: msg.SessionActorPath == LobbyOwner
                    )
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
        try
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
                    CurrentState: new GameSnapShot(
                        isOwner: LobbyOwner == msg.SessionActorPath,
                        playerCount: NumberOfPlayers,
                        currentStatus: LobbyStatus,
                        ships: [],
                        asteroids: [],
                        projectiles: [],
                        boardWidth: GameState.BoardWidth,
                        boardHeight: GameState.BoardHeight
                    )
                )
            );
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error getting lobby state in Lobby Actor: {0}", Self.Path.Name);
            Context.Parent.Tell(new ErrorMessage("Couldn't get lobby state"));
        }
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        _log.Info("Getting lobbies in Lobby Actor: {0}", Self.Path.Name);
        var gl = new GameLobby(LobbyName, 0, NumberOfPlayers);
        Sender.Tell(gl);
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        try
        {
            _sessionsToUpdate.Add(Sender); // should be the session Actor.
            var ranX = _random.Next(0, GameState.BoardWidth);
            var ranY = _random.Next(0, GameState.BoardHeight);
            GameState.AddShip(
                msg.SessionActorPath,
                new Ship(xCoordinate: ranX, yCoordinate: ranY, rotation: 0)
            );
            _log.Info("Lobby Joined by {0}", Sender.Path.Name);
        }
        catch (Exception e)
        {
            _log.Error(e, "Error joining lobby for {0}", Sender.Path.Name);
            Context.Parent.Tell(new ErrorMessage("Couldn't join lobby"));
        }
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
