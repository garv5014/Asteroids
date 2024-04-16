using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;

namespace RealTimeCommunication.Actors.Session;

public record GetUserSessionMessage(string ActorPath);

public record GetUserSessionResponse(IActorRef ActorRef);

public class SessionSupervisor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _accountRelayActor;
    private readonly IActorRef _accountPersistenceActor;
    private readonly IActorRef? testProbe;
    private Dictionary<string, IActorRef> _sessions = new();

    public SessionSupervisor(IActorRef? testProbe = null)
    {
        _log.Info("SessionSupervisor created");

        if (testProbe != null)
        {
            _accountRelayActor = testProbe;
            this.testProbe = testProbe;
        }
        else
        {
            _accountRelayActor = Context
                .ActorSelection($"/user/{ActorHelper.AccountRelayActorName}")
                .ResolveOne(TimeSpan.FromSeconds(3))
                .Result;
        }

        _accountPersistenceActor = Context.ActorOf(
            AccountPersistanceActor.Props(),
            ActorHelper.AccountPersistanceActorName
        );
        
        Receive<LoginMessage>(cam => CreateAccountMessage(cam));
        Receive<GetUserSessionMessage>(gusm => GetUserSessionMessage(gusm));
    }

    private void CreateAccountMessage(LoginMessage lm)
    {
        _log.Info("User {0} already exists", lm.User);
        // Create the user
        _log.Info("Creating user {0}", lm.User);

        IActorRef session;
        var accountId = Guid.NewGuid();

        if (testProbe != null)
        {
            session = testProbe;
        }
        else
        {
            session = Context.ActorOf(SessionActor.Props(lm.User), accountId.ToString());
        }

        _sessions.Add(session.Path.ToString(), session);
        _accountPersistenceActor.Tell(
            new StoreAccountInformationMessage(
                new AccountInformation(password: lm.Password, userName: lm.User, userId: accountId)
            )
        );
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

    public static Props Props(IActorRef? testProbe = null)
    {
        return Akka.Actor.Props.Create(() => new SessionSupervisor(testProbe));
    }
}
