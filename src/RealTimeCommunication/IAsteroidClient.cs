namespace RealTimeCommunication;

public interface IAsteroidClient
{
    Task ReceiveActorMessage(string Message);
}
