using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IAccountHub
{
    // For messages client -> hub -> actor use Tell
    // for messages hub -> client use Publish
    Task TellActor(string user, string message);
    Task TellClient(string message);
    Task CreateAccountTell(CreateAccountMessage message);
    Task LoginTell(LoginMessage message);
    Task LoginResponsePublish(LoginResponseMessage message);
    Task CreateAccountResponsePublish(CreateAccountResponseMessage message);
}
