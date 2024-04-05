﻿using Akka.Actor;
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
        Receive<AllLobbiesResponse>(async response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.ConnectionId);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.LobbiesPublish(response);
            });
        });

        Receive<JoinLobbyResponse>(async response =>
        {
            ExecuteAndPipeToSelf(async () =>
            {
                _log.Info("Sending response to client: {0}", response.ConnectionId);
                Client = hubConnection.ServerProxy<ILobbyHub>();
                await Client.JoinLobbyPublish(response);
            });
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info($"{nameof(AccountHubRelay)} started");
    }

    public static Props Props()
    {
        return Akka.Actor.Props.Create(() => new AccountHubRelay());
    }
}