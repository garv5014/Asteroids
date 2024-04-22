using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public record GameLobby(string Name, int PlayerCount);

public record GetLobbiesMessage(string SessionActorPath, string ConnectionId)
    : HubMessage(ConnectionId, SessionActorPath);

public record CreateLobbyMessage(string SessionActorPath, string LobbyName, string ConnectionId)
    : HubMessage(ConnectionId, SessionActorPath);

public record JoinLobbyMessage(string SessionActorPath, string ConnectionId, string LobbyName)
    : HubMessage(ConnectionId, SessionActorPath);

public record JoinLobbyResponse(string ConnectionId, string SessionActorPath, string LobbyName)
    : HubMessage(ConnectionId, SessionActorPath);

public record KillLobbyMessage(string SessionActorPath, string ConnectionId, string LobbyName)
    : HubMessage(ConnectionId, SessionActorPath);

public record CreateLobbyResponse(string ConnectionId, string SessionActorPath, string LobbyName)
    : HubMessage(ConnectionId, SessionActorPath);

public record AllLobbiesResponse(
    string ConnectionId,
    string SessionActorPath,
    List<GameLobby> Lobbies
) : HubMessage(ConnectionId, SessionActorPath);

public record GetLobbyStateMessage(string SessionActorPath, string ConnectionId, string LobbyName)
    : HubMessage(ConnectionId, SessionActorPath);

// lobby state response Params: isOwner(bool) number of players(int) current state(lobby state enum).
public record LobbyStateResponse(
    string ConnectionId,
    string SessionActorPath,
    GameSnapShot CurrentState
) : HubMessage(ConnectionId, SessionActorPath);

public record UpdateLobbyMessage(
    string ConnectionId,
    string SessionActorPath,
    string LobbyName,
    LobbyStatus NewStatus
) : HubMessage(ConnectionId, SessionActorPath);

// update lobby state response params: updated state
public record UpdateLobbyStateResponse(
    string ConnectionId,
    string SessionActorPath,
    LobbyStatus CurrentStatus
) : HubMessage(ConnectionId, SessionActorPath);

public record RefreshConnectionIdMessage(string ConnectionId, string SessionActorPath)
    : HubMessage(ConnectionId, SessionActorPath);
