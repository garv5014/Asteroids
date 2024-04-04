using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IAsteroidClientHub
{
    Task HandleActorMessage(string Message);
    Task HandleCreateAccountResponse(CreateAccountResponseMessage message);

    Task HandleLoginResponse(LoginResponseMessage message);
}
