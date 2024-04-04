using Akka.Actor;
using Akka.Event;
namespace RealTimeCommunication;

public class LobbySupervisor : ReceiveActor
{
  // return all the lobbies lobbies stored as a list of lobbies
  // Create a lobby actor
  // Forward messages to the lobby actor
  

  private readonly ILoggingAdapter _log = Context.GetLogger();

  public LobbySupervisor()
  {
    // 
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
