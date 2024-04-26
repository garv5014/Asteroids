using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;

namespace RealTimeCommunication.Actors.Session;

public record GetUserSessionMessage(string ActorPath, string? ConnectionId);

public record GetUserSessionResponse(IActorRef ActorRef);

public class SessionSupervisor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _accountRelayActor;
    private readonly IActorRef _accountPersistenceActor;
    private readonly IActorRef _lobbySupervisor;
    private Dictionary<string, IActorRef> _sessionNameToActor = new();

    public SessionSupervisor(
        IActorRef lobbySupervisor,
        IActorRef accountRelayHub,
        IActorRef accountPersistenceActor
    )
    {
        _log.Info("SessionSupervisor created");

        _accountRelayActor = accountRelayHub;
        Receive<LoginMessage>(CreateAccountMessage);
        Receive<GetUserSessionMessage>(GetUserSessionMessage);
        _accountPersistenceActor = accountPersistenceActor;
        _lobbySupervisor = lobbySupervisor;
    }

    private void CreateAccountMessage(LoginMessage lm)
    {
        if (lm.User == null || lm.Password == null)
        {
            _accountRelayActor.Tell(
                new LoginResponseMessage(
                    lm.ConnectionId,
                    false,
                    "Username or password cannot be empty",
                    ""
                )
            );
            return;
        }

        if (
            _sessionNameToActor.ContainsKey(
                ActorHelper.GetActorNameFromPath(lm?.SessionActorPath ?? "")
            )
        )
        {
            _accountRelayActor.Tell(
                new LoginResponseMessage(
                    lm.ConnectionId,
                    false,
                    "Please check your username and password",
                    ""
                )
            );
            return;
        }
        _log.Info("Creating user {0}", lm.User);

        IActorRef session;
        var accountId = Guid.NewGuid();

        session = Context.ActorOf(
            SessionActor.Props(lm.User, _lobbySupervisor),
            accountId.ToString()
        );

        _sessionNameToActor.Add(accountId.ToString(), session);
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
        var sessionName = ActorHelper.GetActorNameFromPath(gusm.ActorPath);
        _log.Info("Getting user session for {0}", sessionName);
        if (_sessionNameToActor.ContainsKey(sessionName))
        {
            var actor = _sessionNameToActor[sessionName];
            Sender.Tell(new GetUserSessionResponse(actor));
            return;
        }
        else
        {
            _lobbySupervisor.Tell(new ErrorMessage("Session not found", gusm?.ConnectionId ?? ""));
        }
    }

    public static Props Props(
        IActorRef LobbySupervisor,
        IActorRef accountRelay,
        IActorRef accountPersistenceActor
    )
    {
        return Akka.Actor.Props.Create(
            () => new SessionSupervisor(LobbySupervisor, accountRelay, accountPersistenceActor)
        );
    }
}
