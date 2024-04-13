namespace Asteroids.Shared;

public interface ILobbyClient
{
    Task HandleLobbiesResponse(AllLobbiesResponse message);
    Task HandleJoinLobbyResponse(JoinLobbyResponse message);
    Task HandleCreateLobbyResponse(CreateLobbyResponse message);
    Task HandleLobbyStateResponse(LobbyStateResponse message);
    Task HandleRefreshConnectionId();
}
