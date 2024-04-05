using Akka.Actor;
using Akka.Event;

namespace RealTimeCommunication;

public class LobbyActor : ReceiveActor
{
    // Lobby actor is in charge of creating and managing lobbies
    // respond to messages from the lobby supervisor after creation.
    // Add user to lobby'

    private string lobbyName { get; init; }
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public LobbyActor(string name) { }

    protected override void PreStart()
    {
        _log.Info("LobbyActor created");
    }

    protected override void PostStop()
    {
        _log.Info("LobbyActor stopped");
    }

    public static Props Props(string LobbyName)
    {
        return Akka.Actor.Props.Create(() => new LobbyActor(LobbyName));
    }
}
