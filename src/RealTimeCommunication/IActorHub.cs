namespace RealTimeCommunication;

public interface IActorHub
{
    Task TellActor(string user, string message);
}
