using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor, IWithTimers
{
    private string LobbyName { get; init; }
    public List<((int xEdge, int yEdge), (int headingMin, int headingMax))> edges { get; set; } =
        new List<((int xEdge, int yEdge), (int headingMin, int headingMax))>
        {
            ((595, 0), (0, 180)), // top
            ((0, 595), (180, 360)), // left
            ((595, 595), (90, 270)), // bottom
            ((595, 0), (270, 430)) // right
        };
    private int NumberOfPlayers
    {
        get => _sessionsToUpdate.Count;
    }

    private LobbyStatus LobbyStatus { get; set; }

    private string LobbyOwner { get; init; }
    public ITimerScheduler Timers { get; set; }

    private LobbyState _lobbyState;

    private readonly ILoggingAdapter _log = Context.GetLogger();

    private List<IActorRef> _sessionsToUpdate = [];
    private Dictionary<string, Ship> _ships = new();
    private readonly Random _random = new();

    public LobbyActor(string name, string owner)
    {
        LobbyName = name;
        LobbyOwner = owner;
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
            MoveShips();
            MoveAsteroids();
            CheckCollisions();
            UpdateClients();
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        if (_ships.Count == 0)
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
        List<Ship> newShips = [];
        foreach (var ship in _ships)
        {
            newShips.Add(ship.Value);
        }

        _lobbyState.Ships = newShips;
        foreach (var session in _sessionsToUpdate)
        {
            _log.Info("Sending lobby state to {0}", session.Path);
            session.Tell(
                new LobbyStateResponse(
                    ConnectionId: "",
                    SessionActorPath: session.Path.ToString(),
                    CurrentState: _lobbyState
                )
            );
        }
    }

    private void SpawnAsteroids(SpawnAsteroidMessage obj)
    {
        _log.Info("Spawning asteroids");

        var randomEdge = _random.Next(0, edges.Count);
        var edge = edges[randomEdge];
        var spawnX = _random.Next(edge.Item1.xEdge);
        var spawnY = randomEdge == 2 ? edge.Item1.yEdge : _random.Next(edge.Item1.yEdge);

        var heading = _random.Next(edge.Item2.headingMin, edge.Item2.headingMax);
        double headingRadians = Math.PI * heading / 180.0;
        double speedFactor = 4; // Adjust this value to control asteroid speed

        var asteroid = new Asteroid(
            xCoordinate: spawnX,
            yCoordinate: spawnY,
            rotation: heading,
            size: 20,
            velocityX: Math.Cos(headingRadians) * speedFactor,
            velocityY: Math.Sin(headingRadians) * speedFactor
        );

        _lobbyState.Asteroids.Add(asteroid);
        ScheduleNextAsteroidSpawn();
    }

    private void ScheduleNextAsteroidSpawn()
    {
        var nextSpawnInMilliseconds = _random.Next(1000, 5000); // Random interval between spawns
        Timers.StartSingleTimer(
            "spawnAsteroid",
            new SpawnAsteroidMessage(),
            TimeSpan.FromMilliseconds(nextSpawnInMilliseconds)
        );
    }

    // Call this method in the constructor or appropriate place to start the asteroid spawning

    private void MoveAsteroids()
    {
        _log.Info("Moving asteroids");

        var asteroidsToRemove = new List<Asteroid>();
        foreach (var asteroid in _lobbyState.Asteroids)
        {
            if (
                asteroid.YCoordinate > _lobbyState.boardHeight
                || asteroid.YCoordinate < 0
                || asteroid.XCoordinate > _lobbyState.boardWidth
                || asteroid.XCoordinate < 0
            )
            {
                asteroidsToRemove.Add(asteroid);
            }
            else
            {
                asteroid.XCoordinate += (int)asteroid.VelocityX;
                asteroid.YCoordinate += (int)asteroid.VelocityY;
            }
        }

        // Now remove the asteroids that need to be removed.
        foreach (var asteroid in asteroidsToRemove)
        {
            _lobbyState.Asteroids.Remove(asteroid);
        }
    }

    private void MoveShips()
    {
        _log.Info("Moving ships");
        foreach (var shipEntry in _ships)
        {
            _log.Info("Moving ship {0}", shipEntry.Key);
            var ship = shipEntry.Value;

            // Handle rotation
            if (ship.ShipMovement.IsRotatingRight)
            {
                ship.Rotation -= 5; // Adjust as needed
            }
            else if (ship.ShipMovement.IsRotatingLeft)
            {
                ship.Rotation += 5; // Adjust as needed
            }

            ship.Rotation = (ship.Rotation + 360) % 360; // Normalize rotation

            // Handle thrust
            if (ship.ShipMovement.IsThrusting)
            {
                double radians = Math.PI * ship.Rotation / 180.0;
                ship.VelocityX += Math.Cos(radians) * 0.1; // Adjust thrust power
                ship.VelocityY += Math.Sin(radians) * 0.1; // Adjust thrust power
            }

            // Update ship position based on velocity
            ship.XCoordinate += (int)ship.VelocityX;
            ship.YCoordinate += (int)ship.VelocityY;

            // Screen wrapping
            ship.XCoordinate = (ship.XCoordinate + _lobbyState.boardWidth) % _lobbyState.boardWidth;
            ship.YCoordinate =
                (ship.YCoordinate + _lobbyState.boardHeight) % _lobbyState.boardHeight;
        }
    }

    private void CheckCollisions()
    {
        var asteroidsToRemove = new List<Asteroid>();
        foreach (var asteroid in _lobbyState.Asteroids)
        {
            foreach (var shipEntry in _ships)
            {
                var ship = shipEntry.Value;
                // Calculate distance between ship and asteroid
                double distance = Math.Sqrt(
                    Math.Pow(asteroid.XCoordinate - ship.XCoordinate, 2)
                        + Math.Pow(asteroid.YCoordinate - ship.YCoordinate, 2)
                );

                // Check if distance is less than sum of their radii (size / 2 for simplicity)
                if (distance < (asteroid.Size / 2) + (ship.Size / 2))
                {
                    ship.Health -= 10;
                    asteroidsToRemove.Add(asteroid);

                    // Optional: Add logic to handle what happens when health reaches 0
                    if (ship.Health <= 0)
                    {
                        // Ship destroyed
                        _ships.Remove(shipEntry.Key);
                    }

                    // Optional: Remove asteroid or split into smaller asteroids
                }
            }
        }
        foreach (var asteroid in asteroidsToRemove)
        {
            _lobbyState.Asteroids.Remove(asteroid);
        }
    }

    private void UpdateShip(UpdateShipMessage obj)
    {
        if (LobbyStatus != LobbyStatus.InGame)
            return;
        if (_ships.TryGetValue(obj.SessionActorPath, out var ship))
        {
            _log.Info("Updating ship for {0} in LobbyActor", obj.SessionActorPath);
            ship.ShipMovement = obj.ShipParams;
        }
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
            _lobbyState = new LobbyState(
                isOwner: LobbyOwner == msg.SessionActorPath,
                playerCount: NumberOfPlayers,
                currentStatus: LobbyStatus,
                ships: _ships.Values.ToList(),
                asteroids: []
            );

            Context.Parent.Tell(
                new LobbyStateResponse(
                    ConnectionId: msg.ConnectionId,
                    SessionActorPath: msg.SessionActorPath,
                    CurrentState: _lobbyState
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
        _ships[msg.SessionActorPath] = new Ship(xCoordinate: 0, yCoordinate: 0, rotation: 0);
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
