using Akka.Actor;
using Akka.Event;
using Asteroids.Shared.Services;

namespace RealTimeCommunication;

public class SessionStorageActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly UserPersistanceService _persistenceService;

    public SessionStorageActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        _persistenceService = serviceProvider.GetRequiredService<UserPersistanceService>();
        Receive<CompareAndSwapMessage<string>>(msg => CompareAndSwap(msg));
    }

    private void CompareAndSwap(CompareAndSwapMessage<string> msg)
    {
        throw new NotImplementedException();
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("RaftActor started");
    }

    protected override void PostStop()
    {
        base.PostStop();
        _log.Info("RaftActor stopped");
    }

    public static Props Props(IServiceProvider serviceProvider)
    {
        return Akka.Actor.Props.Create(() => new SessionStorageActor(serviceProvider));
    }
}

public record CompareAndSwapMessage<T>(string Key, T Value);
