using Asteroids.Shared.Messages;

namespace Asteroids.Shared;
public record Lobby(string Name, string Id, int PlayerCount);
public record GetLobbiesMessage(string SessionActorPath) : HubMessage(null, SessionActorPath);
public record AllLobbiesResponse(string ConnectionId, string SessionActorPath, Lobby[] Lobbies) : HubMessage(ConnectionId, SessionActorPath);