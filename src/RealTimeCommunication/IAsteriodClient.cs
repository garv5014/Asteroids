namespace RealTimeCommunication;

public interface IAsteriodClient
{
    Task RecieveActorMessage(string Message);
}
