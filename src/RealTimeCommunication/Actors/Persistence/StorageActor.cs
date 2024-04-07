using Akka.Actor;
using Akka.Event;

namespace RealTimeCommunication;

public class RaftActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly HttpClient raftClient;

    public RaftActor(HttpClient raftGatewayClient)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        Receive<CompareAndSwapMessage<string>>(msg => CompareAndSwap(msg));
        this.raftClient = raftGatewayClient;
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

    public static Props Props(HttpClient raftGatewayClient)
    {
        return Akka.Actor.Props.Create(() => new RaftActor(raftGatewayClient));
    }
}

public record CompareAndSwapMessage<T>(string Key, T Value);
