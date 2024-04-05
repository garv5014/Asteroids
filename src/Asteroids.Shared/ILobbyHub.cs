namespace Asteroids.Shared;

public interface ILobbyHub
{
  Task LobbiesQuery(GetLobbiesMessage message);
  Task LobbiesPublish(AllLobbiesResponse message);
  Task CreateLobbyCommand(CreateLobbyMessage message);
  Task JoinLobbyCommand(JoinLobbyMessage message);
}