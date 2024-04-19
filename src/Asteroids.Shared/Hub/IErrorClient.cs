using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IErrorClient
{
    Task HandleError(ErrorMessage message);
}
