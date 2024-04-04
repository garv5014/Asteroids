using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using RealTimeCommunication.Actors.Hub;

namespace RealTimeCommunication.Actors.Session;

public class SessionSupervisor : ReceiveActor
{
  // This is the supervisor strategy for the child actors
  // Make a list of session actors
  private readonly ILoggingAdapter _log = Context.GetLogger();
  private readonly IActorRef _relayActor;
  public Dictionary<string, IActorRef> UserNameToSession { get; } = new();
  public Dictionary<IActorRef, string> ActorRefToUserName { get; } = new();

  public SessionSupervisor()
  {
    _log.Info("SessionSupervisor created");
    _relayActor = Context.ActorOf(
      PublishToClientActor.Props(),
      "PublishToClientActor"
    );
    Receive<SimpleMessage>(sm => HandleSimpleMessage(sm));
    Receive<LoginMessage>(cam => HandleCreateAccountMessage(cam));
  }

  private void HandleCreateAccountMessage(LoginMessage lm)
  {
    // Verify user doesn't already exist
    if (UserNameToSession.ContainsKey(lm.User))
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
      return;
    }

    // Create the user
    _log.Info("Creating user {0}", lm.User);
    var session = Context.ActorOf(SessionActor.Props(lm.User, lm.ConnectionId), lm.User);
    UserNameToSession[lm.User] = session;
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

  private void HandleSimpleMessage(SimpleMessage sm)
  {
    _log.Info("Received message: {0}", sm.Message);
    _relayActor.Tell(sm);
  }

  public static Props Props()
  {
    return Akka.Actor.Props.Create(() => new SessionSupervisor());
  }
}