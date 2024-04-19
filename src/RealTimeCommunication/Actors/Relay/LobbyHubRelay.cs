using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using RealTimeCommunication.Actors.Hub;

namespace RealTimeCommunication;

public class LobbyHubRelay : ActorPublisher
{
    private ILobbyHub Client;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public LobbyHubRelay()
        : base(LobbyHub.FullUrl)
    {
        Receive<AllLobbiesResponse>(response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending Lobby response to client: {0}", response.ConnectionId);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.LobbiesPublish(response);
            });
        });

        Receive<JoinLobbyResponse>(response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.ConnectionId);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.JoinLobbyPublish(response);
            });
        });

        Receive<CreateLobbyResponse>(response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.ConnectionId);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.CreateLobbyPublish(response);
            });
        });

        Receive<LobbyStateResponse>(response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.ConnectionId);
                _log.Info("Projectiles: {0}", response.CurrentState.Projectiles.Count);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.LobbyStatePublish(response);
            });
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info($"{nameof(LobbyHubRelay)} started");
    }

    protected override void PostStop()
    {
        base.PostStop();
        _log.Info($"{nameof(LobbyHubRelay)} Stopped ");
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new LobbyHubRelay());
    }
}
