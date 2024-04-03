namespace Asteroids.Shared;

public interface IAsteroidClientHub
{
    Task ReceiveActorMessage(string Message);
}
