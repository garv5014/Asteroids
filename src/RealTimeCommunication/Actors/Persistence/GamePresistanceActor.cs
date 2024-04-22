using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Services;

namespace RealTimeCommunication;

public class GamePersistanceActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IUserPersistence _persistenceService;

    public GamePersistanceActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        var s = serviceProvider.CreateScope();
        _persistenceService = s.ServiceProvider.GetRequiredService<IUserPersistence>();
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

public class LobbyInformation
{
    public LobbySnapShot GameState { get; set; }

    public LobbyInformation(LobbySnapShot gameSnapShot)
    {
        GameState = gameSnapShot;
    }
}

public class LobbySnapShot
{
    public string LobbyName { get; set; }
    public int NumberOfPlayers { get; set; }
    public LobbyStatus LobbyStatus { get; set; }
    public Game GameState { get; set; }
    public string LobbyOwner { get; set; }
    public List<IActorRef> SessionsToUpdate { get; set; }

    public LobbySnapShot(
        string lobbyName,
        int numberOfPlayers,
        LobbyStatus lobbyStatus,
        Game gameState,
        string lobbyOwner,
        List<IActorRef> sessionsToUpdate
    )
    {
        LobbyName = lobbyName;
        NumberOfPlayers = numberOfPlayers;
        LobbyStatus = lobbyStatus;
        GameState = gameState;
        LobbyOwner = lobbyOwner;
        SessionsToUpdate = sessionsToUpdate;
    }
}

public record StoreGameInformationMessage(LobbySnapShot LobbySnapShot);
