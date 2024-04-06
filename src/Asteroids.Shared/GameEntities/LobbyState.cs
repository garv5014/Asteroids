namespace Asteroids.Shared.GameEntities;

public class LobbyState // Do DDD on this class
{
    public LobbyState(bool isOwner, int playerCount, LobbyStatus currentStatus)
    {
        IsOwner = isOwner;
        PlayerCount = playerCount;
        CurrentStatus = currentStatus;
    }

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
