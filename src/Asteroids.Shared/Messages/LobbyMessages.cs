using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public record GameLobby(string Name, string Id, int PlayerCount);

public record GetLobbiesMessage(string SessionActorPath, string ConnectionId)
    : HubMessage(ConnectionId, SessionActorPath);

public record CreateLobbyMessage(string SessionActorPath, string LobbyName, string ConnectionId)
    : HubMessage(ConnectionId, SessionActorPath);

public record JoinLobbyMessage(string SessionActorPath, string ConnectionId, int LobbyId)
    : HubMessage(ConnectionId, SessionActorPath);

public record JoinLobbyResponse(string ConnectionId, string SessionActorPath, int LobbyId)
    : HubMessage(ConnectionId, SessionActorPath);

public record CreateLobbyResponse(string ConnectionId, string SessionActorPath, string LobbyId)
    : HubMessage(ConnectionId, SessionActorPath);

public record AllLobbiesResponse(string ConnectionId, string SessionActorPath, GameLobby[] Lobbies)
    : HubMessage(ConnectionId, SessionActorPath);
