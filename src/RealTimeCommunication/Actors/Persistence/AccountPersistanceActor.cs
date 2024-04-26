using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class AccountPersistanceActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IUserPersistence _persistenceService;

    public AccountPersistanceActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        var s = serviceProvider.CreateScope();
        _persistenceService = s.ServiceProvider.GetRequiredService<IUserPersistence>();
        Receive<StoreAccountInformationMessage>(msg => StoreAccountInformation(msg));
        Receive<LoginMessage>(msg => Login(msg));
        Receive<LoginToConfirmMessage>(ConfirmLogin);
    }

    private void ConfirmLogin(LoginToConfirmMessage msg)
    {
        var user = msg.AccountInformation;
        if (user == null)
        {
            _log.Info("User {0} not found", msg.User);
            Sender.Tell(
                new LoginConfirmedMessage(
                    ActorPath: msg.SessionActorPath,
                    ConnectionId: msg.ConnectionId,
                    UserName: msg.User,
                    Status: LoginStatus.NewAccount
                )
            );
            var acctInfo = new AccountInformation(
                msg.User,
                msg.Password,
                new Guid(ActorHelper.GetActorNameFromPath(msg.SessionActorPath))
            );
            Self.Tell(new StoreAccountInformationMessage(acctInfo));
            return;
        }

        _log.Info("User {0} found", msg.User);
        Sender.Tell(
            new LoginConfirmedMessage(
                ActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                UserName: msg.User,
                Status: LoginStatus.ExistingAccount
            )
        );
    }

    private async Task Login(LoginMessage msg)
    {
        _log.Info("Logging in {0}", msg.User);

        await GetAccountInformation(msg).PipeTo(Self, Sender);

        async Task<LoginToConfirmMessage> GetAccountInformation(LoginMessage msg)
        {
            var parsable = Guid.TryParse(
                ActorHelper.GetActorNameFromPath(msg.SessionActorPath),
                out var userId
            );
            var aI = await _persistenceService.GetUserInformationAsync(
                parsable ? userId : Guid.Empty
            );

            return new LoginToConfirmMessage(
                AccountInformation: aI,
                User: msg.User,
                Password: msg.Password,
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId
            );
        }
    }

    private async Task StoreAccountInformation(StoreAccountInformationMessage msg)
    {
        _log.Info("Storing account information for {0}", msg.AccountInformation.UserName);
        await _persistenceService.StoreUserInformationAsync(msg.AccountInformation);
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("StorageActor started");
    }

    protected override void PostStop()
    {
        base.PostStop();
        _log.Info("StorageActor stopped");
    }

    public static Props Props()
    {
        var spExtension = DependencyResolver.For(Context.System);
        return spExtension.Props<AccountPersistanceActor>();
    }

    public static Props Props(ActorSystem system)
    {
        var spExtension = DependencyResolver.For(system);
        return spExtension.Props<AccountPersistanceActor>();
    }

    public static Props Props(IServiceProvider serviceProvider)
    {
        return Akka.Actor.Props.Create(() => new AccountPersistanceActor(serviceProvider));
    }
}

public record StoreAccountInformationMessage(AccountInformation AccountInformation);

public record LoginToConfirmMessage(
    AccountInformation? AccountInformation,
    string User,
    string Password,
    string SessionActorPath,
    string ConnectionId
);
