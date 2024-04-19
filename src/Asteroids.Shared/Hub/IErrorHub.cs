using Asteroids.Shared.Messages;

namespace Asteroids.Shared;

public interface IErrorHub 
{ 
    Task ErrorPublish(ErrorMessage message);
}
