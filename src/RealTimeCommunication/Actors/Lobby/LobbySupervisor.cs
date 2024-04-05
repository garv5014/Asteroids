using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
namespace RealTimeCommunication;

public class LobbySupervisor : ReceiveActor
{
  // return all the lobbies lobbies stored as a Dictionary of lobbies
  // Create a lobby actor
  // Forward messages to the lobby actor
  // Join Lobby passed to lobby actor

  private readonly ILoggingAdapter _log = Context.GetLogger();

  public LobbySupervisor()
  {
    Receive<CreateLobbyMessage>(msg => CreateLobby(msg));
  }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        var lobbyActor = Context.ActorOf(LobbyActor.Props(msg.LobbyName), msg.LobbyName);
        lobbyActor.Forward(msg);
    }

    protected override void PreStart()
  {
    _log.Info("LobbySupervisor created");
  }

  protected override void PostStop()
  {
    _log.Info("LobbySupervisor stopped");
  }
  public static Props Props()
  {
    return Akka.Actor.Props.Create(() => new LobbySupervisor());
  }
}
