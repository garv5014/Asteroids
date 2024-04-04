namespace Asteroids.Shared.Messages;

public record HubMessage(string ConnectionId, string SessionActorPath);
