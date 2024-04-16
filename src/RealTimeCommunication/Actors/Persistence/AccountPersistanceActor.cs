using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.Services;

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

    public static Props Props(IServiceProvider serviceProvider)
    {
        return Akka.Actor.Props.Create(() => new AccountPersistanceActor(serviceProvider));
    }
}

internal class AccountStateActor { }

public record StoreAccountInformationMessage(AccountInformation AccountInformation);
