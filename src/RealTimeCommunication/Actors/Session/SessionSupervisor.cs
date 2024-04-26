using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;

namespace RealTimeCommunication.Actors.Session;

public enum LoginStatus
{
    NewAccount,
    ExistingAccount,
    InvalidAccount
}

public record GetUserSessionMessage(string ActorPath, string ConnectionId = "")
    : HubMessage(SessionActorPath: ActorPath, ConnectionId: ConnectionId);

public record GetUserSessionResponse(IActorRef ActorRef);

public record LoginConfirmedMessage(
    string ActorPath,
    string ConnectionId,
    LoginStatus Status,
    string User
) : HubMessage(SessionActorPath: ActorPath, ConnectionId: ConnectionId);

public class SessionSupervisor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _accountRelayActor;
    private readonly IActorRef _accountPersistenceActor;
    private readonly IActorRef _lobbySupervisor;
    private Dictionary<string, string> _userNameToAccountId = new();
    private Dictionary<string, IActorRef> _sessionNameToActor = new();

    public SessionSupervisor(
        IActorRef lobbySupervisor,
        IActorRef accountRelayHub,
        IActorRef accountPersistenceActor
    )
    {
        _log.Info("SessionSupervisor created");

        _accountRelayActor = accountRelayHub;
        Receive<LoginMessage>(LoginMessage);
        Receive<GetUserSessionMessage>(GetUserSessionMessage);
        Receive<LoginConfirmedMessage>(ConfirmLogin);
        _accountPersistenceActor = accountPersistenceActor;
        _lobbySupervisor = lobbySupervisor;
    }

    private void ConfirmLogin(LoginConfirmedMessage lm)
    {
        _log.Info("Creating user {0}", lm.User);

        IActorRef session;
        var accountId = Guid.NewGuid();

        session = Context.ActorOf(
            SessionActor.Props(lm.User, _lobbySupervisor),
            accountId.ToString()
        );

        _sessionNameToActor.Add(accountId.ToString(), session);

        _userNameToAccountId.Add(lm.User, accountId.ToString());

        _accountRelayActor.Tell(
            new LoginResponseMessage(
                lm.ConnectionId,
                true,
                "Successfully logged in",
                session.Path.ToString()
            )
        );
    }

    private void LoginMessage(LoginMessage lm)
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

        _accountPersistenceActor.Tell(
            new LoginMessage(
                lm.User,
                lm.Password,
                lm.ConnectionId,
                lm.SessionActorPath
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
