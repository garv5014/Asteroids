namespace Asteroids.Shared;

public interface IAsteroidClient
{
    Task ReceiveActorMessage(string Message);
}
