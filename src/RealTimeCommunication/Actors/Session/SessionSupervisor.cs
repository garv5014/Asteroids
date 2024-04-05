using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
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
    private readonly IActorRef _AccountRelayActor;

    public SessionSupervisor()
    {
        _log.Info("SessionSupervisor created");
        _AccountRelayActor = Context.ActorOf(
            AccountHubRelay.Props(),
            ActorHelper.AccountRelayActorName
        );
        Receive<LoginMessage>(cam => CreateAccountMessage(cam));
        Receive<GetUserSessionMessage>(gusm => GetUserSessionMessage(gusm));
        Receive<JoinLobbyResponse>(jlr => JoinLobbyResponse(jlr));
    }

    private void JoinLobbyResponse(JoinLobbyResponse jlr)
    {
        throw new NotImplementedException();
    }

    private void CreateAccountMessage(LoginMessage lm)
    {
        _log.Info("User {0} already exists", lm.User);
        _AccountRelayActor.Tell(
            new LoginResponseMessage(
                lm.ConnectionId,
                false,
                "Failed To log in",
                lm.SessionActorPath
            )
        );

        // Create the user
        _log.Info("Creating user {0}", lm.User);
        var session = Context.ActorOf(SessionActor.Props(lm.User, lm.ConnectionId), lm.User);
        _AccountRelayActor.Tell(
            new LoginResponseMessage(
                lm.ConnectionId,
                true,
                "Successfully logged in",
                session.Path.ToString()
            )
        );
    }

    public void GetUserSessionMessage(GetUserSessionMessage gusm)
    {
        var actor = Context
            .ActorSelection(gusm.ActorPath)
            .ResolveOne(TimeSpan.FromSeconds(1))
            .Result;
        Sender.Tell(new GetUserSessionResponse(actor));
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor());
    }
}
