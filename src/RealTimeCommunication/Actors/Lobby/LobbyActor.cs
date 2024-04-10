using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor, IWithTimers
{
  // Lobby actor is in charge of creating and managing lobbies
  // respond to messages from the lobby supervisor after creation.
  // Add user to lobby'

  private string LobbyName { get; init; }

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
      UpdateClients();
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

    // Example spawning logic at the top edge, you can extend this logic for other edges
    var spawnX = _random.Next(0, _lobbyState.boardWidth);
    var spawnY = 0;

    // Random heading downwards
    var heading = _random.Next(160, 200); // Adjust the range based on how you define angles

    // Create and add the asteroid
    var asteroid = new Asteroid
    (
      xCoordinate: spawnX,
      yCoordinate: spawnY,
      rotation: heading,
      size: 20,
      velocityX: 5, // Adjust speed
      velocityY: 5
      // velocityX: Math.Cos(heading * Math.PI / 180.0) * 0.5, // Adjust speed
      // velocityY: Math.Sin(heading * Math.PI / 180.0) * 0.5
    );

    _lobbyState.Asteroids.Add(asteroid);

    // Schedule next spawn
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
    // Update asteroid positions and check for despawning
    _log.Info("Moving asteroids");
    
    var asteroids = new List<Asteroid>();
    foreach(var asteroid in _lobbyState.Asteroids)
    {
      if (asteroid.YCoordinate > _lobbyState.boardHeight || asteroid.YCoordinate < 0 ||
          asteroid.XCoordinate > _lobbyState.boardWidth || asteroid.XCoordinate < 0)
      {
        _lobbyState.Asteroids.Remove(asteroid);
      }
      else
      {
        asteroid.XCoordinate += (int)asteroid.VelocityX;
        asteroid.YCoordinate += (int)asteroid.VelocityY;
        asteroids.Add(asteroid);
      }

      

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
        ship.Rotation += 5; // Adjust as needed
      }
      else if (ship.ShipMovement.IsRotatingLeft)
      {
        ship.Rotation -= 5; // Adjust as needed
      }

      ship.Rotation = (ship.Rotation + 360) % 360; // Normalize rotation

      // Handle thrust
      if (ship.ShipMovement.IsThrusting)
      {
        double radians = Math.PI * ship.Rotation / 180.0;
        ship.VelocityX += Math.Cos(radians) * 0.1; // Adjust thrust power
        ship.VelocityY += Math.Sin(radians) * 0.1; // Adjust thrust power
      }

      // Apply friction/drag to slow down the ship over time
      ship.VelocityX *= 1; // Adjust friction level
      ship.VelocityY *= 1; // Adjust friction level

      // Update ship position based on velocity
      ship.XCoordinate += (int)ship.VelocityX;
      ship.YCoordinate += (int)ship.VelocityY;

      // Screen wrapping
      ship.XCoordinate = (ship.XCoordinate + _lobbyState.boardWidth) % _lobbyState.boardWidth;
      ship.YCoordinate = (ship.YCoordinate + _lobbyState.boardHeight) % _lobbyState.boardHeight;
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

internal record GameLoopMessage
{
}

internal record SpawnAsteroidMessage
{
}