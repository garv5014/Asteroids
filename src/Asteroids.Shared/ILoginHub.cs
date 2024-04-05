using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IAccountClient
{
    Task HandleLoginResponse(LoginResponseMessage message);
}
