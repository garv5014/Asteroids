namespace Asteroids.Shared;

public interface ILobbyHub
{
    Task LobbiesQuery(GetLobbiesMessage message);
    Task LobbiesPublish(AllLobbiesResponse message);
    Task CreateLobbyCommand(CreateLobbyMessage message);
    Task JoinLobbyCommand(JoinLobbyMessage message);
    Task JoinLobbyPublish(JoinLobbyResponse response);
    Task CreateLobbyPublish(CreateLobbyResponse response);
    Task LobbyStateQuery(GetLobbyStateMessage message);
    Task LobbyStatePublish(LobbyStateResponse response);
    Task UpdateLobbyStateCommand(UpdateLobbyMessage message);
}
