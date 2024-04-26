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
    AccountInformation AccountInformation
) : HubMessage(SessionActorPath: ActorPath, ConnectionId: ConnectionId);

public class SessionSupervisor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _accountRelayActor;
    private readonly IActorRef _accountPersistenceActor;
    private readonly IActorRef _lobbySupervisor;
    private Dictionary<string, string> _userNameToAccountId = new();
    private Dictionary<string, IActorRef> _sessionIdToActor = new();

    public SessionSupervisor(
        IActorRef lobbySupervisor,
        IActorRef accountRelayHub,
        IActorRef accountPersistenceActor
    )
    {
        _log.Info("SessionSupervisor created");

        _accountRelayActor = accountRelayHub;
        Receive<LoginMessage>(Login);
        Receive<GetUserSessionMessage>(GetUserSessionMessage);
        Receive<LoginConfirmedMessage>(ConfirmLogin);
        _accountPersistenceActor = accountPersistenceActor;
        _lobbySupervisor = lobbySupervisor;
    }

    private void ConfirmLogin(LoginConfirmedMessage lm)
    {
        // wrong password and or username
        if (lm.Status == LoginStatus.InvalidAccount)
        {
            _accountRelayActor.Tell(
                new LoginResponseMessage(lm.ConnectionId, false, "Invalid username or password", "")
            );
            return;
        }

        // existing account
        if (lm.Status == LoginStatus.NewAccount)
        {
            HandleNewAccount(lm);
        }

        // existing account
        if (lm.Status == LoginStatus.ExistingAccount)
        {
            HandleExistingAccount(lm);
        }
    }

    private void HandleNewAccount(LoginConfirmedMessage lm)
    {
        _log.Info("Creating user {0}", lm.AccountInformation.UserName);

        IActorRef session;
        var accountId = Guid.NewGuid();

        session = Context.ActorOf(
            SessionActor.Props(lm.AccountInformation.UserName, _lobbySupervisor),
            accountId.ToString()
        );

        _sessionIdToActor.Add(accountId.ToString(), session);

        _userNameToAccountId.Add(lm.AccountInformation.UserName, accountId.ToString());

        _accountRelayActor.Tell(
            new LoginResponseMessage(
                lm.ConnectionId,
                true,
                "Successfully logged in",
                session.Path.ToString()
            )
        );

        _accountPersistenceActor.Tell(
            new StoreAccountInformationMessage(
                AccountInformation: new AccountInformation(
                    userName: lm.AccountInformation.UserName,
                    password: lm.AccountInformation.Password,
                    userId: accountId,
                    salt: lm.AccountInformation.Salt
                )
            )
        );
    }

    private void HandleExistingAccount(LoginConfirmedMessage lm)
    {
        _log.Info("Logging in user {0}", lm.AccountInformation.UserName);

        if (_userNameToAccountId.ContainsKey(lm.AccountInformation.UserName))
        {
            var accountId = _userNameToAccountId[lm.AccountInformation.UserName];
            var session = _sessionIdToActor[accountId];
            _accountRelayActor.Tell(
                new LoginResponseMessage(
                    lm.ConnectionId,
                    true,
                    "Successfully logged in",
                    session.Path.ToString()
                )
            );
        }
        else
        {
            _accountRelayActor.Tell(
                new LoginResponseMessage(lm.ConnectionId, false, "User not found", "")
            );
        }
    }

    private void Login(LoginMessage lm)
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

        var potentialActorId = _userNameToAccountId.ContainsKey(lm.User)
            ? _userNameToAccountId[lm.User]
            : Guid.Empty.ToString();

        var session = _sessionIdToActor.ContainsKey(potentialActorId)
            ? _sessionIdToActor[potentialActorId]
            : null;

        _accountPersistenceActor.Tell(
            new LoginMessage(lm.User, lm.Password, lm.ConnectionId, session?.Path.ToString() ?? "")
        );
    }

    public void GetUserSessionMessage(GetUserSessionMessage gusm)
    {
        var sessionName = ActorHelper.GetActorNameFromPath(gusm.ActorPath);
        // _log.Info("Getting user session for {0}", sessionName);
        if (_sessionIdToActor.ContainsKey(sessionName))
        {
            var actor = _sessionIdToActor[sessionName];
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
