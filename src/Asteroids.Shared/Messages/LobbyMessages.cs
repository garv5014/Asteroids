using Asteroids.Shared.Messages;

namespace Asteroids.Shared;
public record GameLobby(string Name, string Id, int PlayerCount);
public record GetLobbiesMessage(string SessionActorPath) : HubMessage(null, SessionActorPath);
public record AllLobbiesResponse(string ConnectionId, string SessionActorPath, GameLobby[] Lobbies) : HubMessage(ConnectionId, SessionActorPath);
