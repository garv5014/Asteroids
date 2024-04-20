namespace Asteroids.Shared.Messages;

public record ErrorMessage(string Message, string ConnectionId = "");