using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IAccountClient
{
    Task HandleActorMessage(string Message);
    Task HandleLoginResponse(LoginResponseMessage message);
}
