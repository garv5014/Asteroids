using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using RealTimeCommunication.Actors.Hub;

namespace RealTimeCommunication.Actors.Session;

public record GetUserSessionMessage(string ActorPath);
public record GetUserSessionResponse(IActorRef ActorRef);

public class SessionSupervisor : ReceiveActor
{
  // This is the supervisor strategy for the child actors
  // Make a list of session actors
  private readonly ILoggingAdapter _log = Context.GetLogger();
  private readonly IActorRef _relayActor;

  public SessionSupervisor()
  {
    _log.Info("SessionSupervisor created");
    _relayActor = Context.ActorOf(
      PublishToClientActor.Props(),
      "PublishToClientActor"
    );
    Receive<LoginMessage>(cam => HandleCreateAccountMessage(cam));
    Receive<GetUserSessionMessage>(gusm => HandleGetUserSessionMessage(gusm));
  }

  private void HandleCreateAccountMessage(LoginMessage lm)
  {
    _log.Info("User {0} already exists", lm.User);
    _relayActor.Tell(
      new LoginResponseMessage
      (
        lm.ConnectionId,
        false,
        "Failed To log in",
        lm.SessionActorPath
      )
    );

    // Create the user
    _log.Info("Creating user {0}", lm.User);
    var session = Context.ActorOf(SessionActor.Props(lm.User, lm.ConnectionId), lm.User);
    _relayActor.Tell(
      new LoginResponseMessage
      (
        lm.ConnectionId,
        true,
        "Successfully logged in",
        session.Path.ToString()
      )
    );
  }

  public void HandleGetUserSessionMessage(GetUserSessionMessage gusm)
  { 
    var actor = Context.ActorSelection(gusm.ActorPath).ResolveOne(TimeSpan.FromSeconds(1)).Result;
    Sender.Tell(new GetUserSessionResponse(actor));
  }

  public static Props Props()
  {
    return Akka.Actor.Props.Create(() => new SessionSupervisor());
  }
}