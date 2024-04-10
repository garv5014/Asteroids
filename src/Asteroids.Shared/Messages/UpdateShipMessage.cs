using Asteroids.Shared.GameEntities;

namespace Asteroids.Shared.Messages;

public record UpdateShipMessage(string ConnectionId, string SessionActorPath, UpdateShipParams ShipParams, int LobbyId)
  : HubMessage(ConnectionId, SessionActorPath);