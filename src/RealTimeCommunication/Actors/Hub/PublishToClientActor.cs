using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication.Actors.Hub;

public class PublishToClientActor : ActorPublisher
{
    private IAccountHub Client;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public PublishToClientActor()
        : base(AccountHub.FullUrl)
    {
        Receive<LoginResponseMessage>(async response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.Message);
                Client = hubConnection.ServerProxy<IAccountHub>();
                await Client.LoginPublish(response);
            });
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info($"{nameof(PublishToClientActor)} started");
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new PublishToClientActor());
    }
}
