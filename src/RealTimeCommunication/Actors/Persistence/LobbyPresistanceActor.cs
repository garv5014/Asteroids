using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.Services;

namespace RealTimeCommunication;

public class LobbyPersistanceActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly ILobbyPersistence _persistenceService;

    public LobbyPersistanceActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        var s = serviceProvider.CreateScope();
        _persistenceService = s.ServiceProvider.GetRequiredService<ILobbyPersistence>();
        Receive<StoreLobbyInformationMessage>(msg => StoreLobbyInformation(msg));
    }

    private void StoreLobbyInformation(StoreLobbyInformationMessage msg)
    {
        throw new NotImplementedException();
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

public class LobbyInformation
{
    public LobbySnapShot GameState { get; set; }

    public LobbyInformation(LobbySnapShot gameSnapShot)
    {
        GameState = gameSnapShot;
    }
}
