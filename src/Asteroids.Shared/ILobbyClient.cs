namespace Asteroids.Shared;

public interface ILobbyClient
{
  Task HandleLobbiesResponse(AllLobbiesResponse message);
}