using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.Services;

namespace RealTimeCommunication;

public class LobbyPersistenceActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly ILobbyPersistence _persistenceService;

    public LobbyPersistenceActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        var s = serviceProvider.CreateScope();
        _persistenceService = s.ServiceProvider.GetRequiredService<ILobbyPersistence>();
        Receive<StoreLobbyInformationMessage>(StoreLobbyInformation);
        Receive<GetLobbyInformationMessage>(GetLobbyInformation);
    }

    private async void GetLobbyInformation(GetLobbyInformationMessage msg)
    {
        _log.Info("Getting lobby information for lobbies");
        await GetLobbySupervisorInformationFromRaft().PipeTo(Sender);
    }

    private async Task<RehydrateLobbySupervisorMessage> GetLobbySupervisorInformationFromRaft()
    {
        var t = await _persistenceService.GetGameInformationAsync();
        return new RehydrateLobbySupervisorMessage(t);
    }

    private void StoreLobbyInformation(StoreLobbyInformationMessage msg)
    {
        _log.Info("Storing lobby information for {0}", msg.LobbySnapShot.LobbyName);
        _persistenceService.StoreGameInformationAsync(msg.LobbySnapShot);
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
        return spExtension.Props<LobbyPersistenceActor>();
    }

    public static Props Props(ActorSystem system)
    {
        var spExtension = DependencyResolver.For(system);
        return spExtension.Props<LobbyPersistenceActor>();
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
