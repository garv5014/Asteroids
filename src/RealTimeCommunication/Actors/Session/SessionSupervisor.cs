using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication.Actors.Session;

public record GetUserSessionMessage(string ActorPath);

public record GetUserSessionResponse(IActorRef ActorRef);

public class SessionSupervisor : ReceiveActor
{
    // This is the supervisor strategy for the child actors
    // Make a list of session actors
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _accountRelayActor;

    private Dictionary<string, IActorRef> _sessions = new();

    public SessionSupervisor()
    {
        _log.Info("SessionSupervisor created");
        _accountRelayActor = Context
            .ActorSelection($"/user/{ActorHelper.AccountRelayActorName}")
            .ResolveOne(TimeSpan.FromSeconds(3))
            .Result;
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
        _accountRelayActor.Tell(
            new LoginResponseMessage(
                lm.ConnectionId,
                false,
                "Failed To log in",
                lm.SessionActorPath
            )
        );
        // Create the user
        _log.Info("Creating user {0}", lm.User);
        var session = Context.ActorOf(
            SessionActor.Props(lm.User, lm.ConnectionId),
            Guid.NewGuid().ToString()
        );
        _sessions.Add(session.Path.ToString(), session);
        _accountRelayActor.Tell(
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
        _log.Info("Getting user session for {0}", gusm.ActorPath);
        var actor = _sessions[gusm.ActorPath];
        Sender.Tell(new GetUserSessionResponse(actor));
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor());
    }
}
