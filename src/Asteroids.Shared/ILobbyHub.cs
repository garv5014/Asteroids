namespace Asteroids.Shared;

public interface ILobbyHub
{
  Task LobbiesQuery(GetLobbiesMessage message);
  Task LobbiesPublish(AllLobbiesResponse message);
}