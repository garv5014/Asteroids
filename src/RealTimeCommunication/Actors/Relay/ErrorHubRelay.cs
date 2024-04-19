using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication.Actors.Hub;

public class ErrorHubRelay : ActorPublisher
{
    private IErrorHub Client;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public ErrorHubRelay()
        : base(ErrorHub.FullUrl)
    {
        Receive<ErrorMessage>(async response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending Error response to client: {0}", response.Message);
                Client = hubConnection.ServerProxy<IErrorHub>();
                await Client.ErrorPublish(response);
            });
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info($"{nameof(ErrorHubRelay)} started");
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new ErrorHubRelay());
    }
}
