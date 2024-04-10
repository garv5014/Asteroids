namespace Asteroids.Shared.GameEntities;

public class LobbyState(
    bool isOwner,
    int playerCount,
    LobbyStatus currentStatus,
    List<Ship> ships,
    List<Asteroid> asteroids
)
{
    public bool IsOwner { get; set; } = isOwner;
    public int PlayerCount { get; set; } = playerCount;
    public LobbyStatus CurrentStatus { get; set; } = currentStatus;

    public List<Ship> Ships { get; set; } = ships;

    public List<Asteroid> Asteroids { get; set; } = asteroids;

    public int boardWidth { get; init; } = 600;
    public int boardHeight { get; init; } = 600;
}

public enum LobbyStatus
{
    WaitingForPlayers,
    InGame,
    GameOver
}
