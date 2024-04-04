namespace Asteroids.Shared.Messages;

public record LoginMessage(string User, string Password, string ConnectionId, string SessionActorPath) : HubMessage(ConnectionId, SessionActorPath);

public record LoginResponseMessage(string ConnectionId, bool Success, string Message, string SessionActorPath) : HubMessage(ConnectionId, SessionActorPath);
