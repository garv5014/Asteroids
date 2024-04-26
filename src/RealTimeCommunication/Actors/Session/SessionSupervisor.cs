﻿using Akka.Actor;
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
    string UserName
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
        _log.Info("Creating user {0}", lm.UserName);

        IActorRef session;
        var accountId = Guid.NewGuid();

        session = Context.ActorOf(
            SessionActor.Props(lm.UserName, _lobbySupervisor),
            accountId.ToString()
        );

        _sessionIdToActor.Add(accountId.ToString(), session);

        _userNameToAccountId.Add(lm.UserName, accountId.ToString());

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
                new AccountInformation(lm.UserName, "", accountId)
            )
        );
    }

    private void HandleExistingAccount(LoginConfirmedMessage lm)
    {
        _log.Info("Logging in user {0}", lm.UserName);

        if (_userNameToAccountId.ContainsKey(lm.UserName))
        {
            var accountId = _userNameToAccountId[lm.UserName];
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

        _accountPersistenceActor.Tell(
            new LoginMessage(
                lm.User,
                lm.Password,
                lm.ConnectionId,
                (lm.SessionActorPath == string.Empty || lm.SessionActorPath == null)
                    ? "re"
                    : lm.SessionActorPath
            )
        );
    }

    public void GetUserSessionMessage(GetUserSessionMessage gusm)
    {
        var sessionName = ActorHelper.GetActorNameFromPath(gusm.ActorPath);
        _log.Info("Getting user session for {0}", sessionName);
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
