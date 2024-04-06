namespace Asteroids.Shared.GameEntities;

public class LobbyState
{
    public bool IsOwner { get; set; }
    public int PlayerCount { get; set; }
    public LobbyStatus CurrentStatus { get; set; }
}

public enum LobbyStatus
{
    WaitingForPlayers,
    InGame,
    GameOver
}
