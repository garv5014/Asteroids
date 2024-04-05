using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IAccountHub
{
    // For messages client -> hub -> actor use Tell
    // for messages hub -> client use Publish
    Task LoginCommand(LoginMessage message);
    Task LoginPublish(LoginResponseMessage message);
}
