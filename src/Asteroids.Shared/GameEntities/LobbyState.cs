namespace Asteroids.Shared.GameEntities;

public class GameSnapShot(
    bool isOwner,
    int playerCount,
    LobbyStatus currentStatus,
    List<Ship> ships,
    List<Asteroid> asteroids,
    List<Projectile> projectiles,
    int boardWidth,
    int boardHeight
)
{
    public bool IsOwner { get; init; } = isOwner;
    public int PlayerCount { get; init; } = playerCount;
    public LobbyStatus CurrentStatus { get; init; } = currentStatus;
    public List<Ship> Ships { get; init; } = ships;
    public List<Asteroid> Asteroids { get; init; } = asteroids;
    public List<Projectile> Projectiles { get; init; } = projectiles;

    public int boardWidth { get; init; } = boardWidth;
    public int boardHeight { get; init; } = boardHeight;
}

public enum LobbyStatus
{
    WaitingForPlayers,
    InGame,
    GameOver
}
