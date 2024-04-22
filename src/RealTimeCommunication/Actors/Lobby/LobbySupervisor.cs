﻿using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;
using Observability;

namespace RealTimeCommunication;

public record GetLobbyMessage(int LobbyId);

public record GetLobbyResponse(IActorRef LobbyActorRef);

public class LobbySupervisor : ReceiveActor
{
    private Dictionary<int, (IActorRef, LobbySnapShot?)> idToActorRef =
        new Dictionary<int, (IActorRef, LobbySnapShot?)>();
    private Dictionary<string, (IActorRef, LobbySnapShot?)> nameToActorRef =
        new Dictionary<string, (IActorRef, LobbySnapShot?)>();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int lobbyId = 0;
    private IActorRef _lobbyRelayActor;

    private ILogger<LobbySupervisor> _logger;

    private IActorRef _errorHubActor;
    private IActorRef _gamePersistanceActor;

    public LobbySupervisor(
        IActorRef lobbyRelayActorRef,
        IActorRef errorHubActorRef,
        IServiceProvider serviceProvider
    )
    {
        Receive<CreateLobbyMessage>(CreateLobby);
        Receive<JoinLobbyMessage>(JoinLobby);
        Receive<UpdateLobbyMessage>(UpdateLobby);
        Receive<GetLobbiesMessage>(GetLobbies);
        Receive<GetLobbyStateMessage>(GetLobbyState);
        Receive<LobbyStateResponse>(HandleStateResponse);
        Receive<GetLobbyMessage>(HandleGetLobbyMessage);
        Receive<ErrorMessage>(msg => _errorHubActor.Tell(msg));
        Receive<Terminated>(msg =>
        {
            var lobbyActor = msg.ActorRef;
            var lobbyName = lobbyActor.Path.Name;
            var lobbyId = idToActorRef.FirstOrDefault(x => x.Value.Item1 == lobbyActor).Key;
            var actor = Context.ActorOf(
                LobbyActor.Props(lobbyName, ""),
                ActorHelper.SanitizeActorName(lobbyName)
            );
            // idToActorRef[lobbyId] = actor;
            // nameToActorRef[lobbyName] = actor;
            _log.Info("Lobby {0} with id {1} terminated", lobbyName, lobbyId);
            DiagnosticsConfig.TotalLobbies.Add(-1);
        });

        var scope = serviceProvider.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<LobbySupervisor>>();
        _lobbyRelayActor = lobbyRelayActorRef;
        _errorHubActor = errorHubActorRef;
        _gamePersistanceActor = Context.ActorOf(LobbyPersistanceActor.Props(serviceProvider));
    }

    private void HandleGetLobbyMessage(GetLobbyMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }

        Sender.Tell(new GetLobbyResponse(LobbyActorRef: lobbyActor.Item1));
    }

    private void UpdateLobby(UpdateLobbyMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            Self.Tell(new ErrorMessage("Lobby does not exist", msg.ConnectionId));
            return;
        }

        Sender.Tell(new GetLobbyResponse(LobbyActorRef: lobbyActor.Item1));
    }

    private void HandleStateResponse(LobbyStateResponse msg)
    {
        _lobbyRelayActor.Tell(msg);
    }

    private void GetLobbyState(GetLobbyStateMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }

        _log.Info("Getting lobby state from child");
        lobbyActor.Item1.Tell(msg);
    }

    private void GetLobbies(GetLobbiesMessage msg)
    {
        _log.Info($"{nameof(LobbySupervisor)}Getting lobbies in ");
        GettingLobbyStates(msg).PipeTo(_lobbyRelayActor);
    }

    private Task<AllLobbiesResponse> GettingLobbyStates(GetLobbiesMessage msg)
    {
        var lobbiesState = new List<GameLobby>();

        foreach (var lobby in idToActorRef)
        {
            var gl = lobby
                .Value.Item1.Ask<GameLobby>(
                    new GetLobbiesMessage(
                        SessionActorPath: msg.SessionActorPath,
                        ConnectionId: msg.ConnectionId
                    )
                )
                .Result;
            var glId = new GameLobby(gl.Name, lobby.Key, gl.PlayerCount);
            _log.Info("Lobby: {0}", glId);
            lobbiesState.Add(glId);
        }

        return Task.FromResult(
            new AllLobbiesResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                Lobbies: lobbiesState ?? new List<GameLobby>()
            )
        );
    }

    private void JoinLobby(JoinLobbyMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }

        lobbyActor.Item1.Forward(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );

        _lobbyRelayActor.Tell(
            new JoinLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );
    }

    private void CreateLobby(CreateLobbyMessage msg)
    {
        if (nameToActorRef.ContainsKey(msg.LobbyName))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with name {0} already exists", msg.LobbyName);
            Self.Tell(new ErrorMessage("Lobby already exists", msg.ConnectionId));
            return;
        }

        lobbyId++;

        var lobbyActor = Context.ActorOf(
            LobbyActor.Props(msg.LobbyName, msg.SessionActorPath),
            ActorHelper.SanitizeActorName(msg.LobbyName)
        );

        idToActorRef.Add(lobbyId, (lobbyActor, null));
        nameToActorRef.Add(msg.LobbyName, (lobbyActor, null));
        Context.Watch(lobbyActor);

        _logger.LogInformation("Lobby created with id {0}", lobbyId);

        Self.Forward(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyId: lobbyId
            )
        );

        _lobbyRelayActor.Tell(
            new CreateLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName,
                LobbyId: lobbyId
            )
        );
        DiagnosticsConfig.TotalLobbies.Add(1);
    }

    protected override void PreStart()
    {
        _log.Info("LobbySupervisor created");
    }

    protected override void PostStop()
    {
        _log.Info("LobbySupervisor stopped");
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 10,
            withinTimeRange: TimeSpan.FromMinutes(1),
            decider: Decider.From(x =>
            {
                switch (x)
                {
                    case Exception _:
                        return Directive.Restart;
                    default:
                        return Directive.Escalate;
                }
            })
        );
    }

    public static Props Props(
        IActorRef lobbyHubRelay,
        IActorRef errorHubRelay,
        ActorSystem actorSystem
    )
    {
        var dR = DependencyResolver.For(actorSystem);
        return dR.Props<LobbySupervisor>(lobbyHubRelay, errorHubRelay);
    }

    public static Props Props(
        IActorRef lobbyHubRelay,
        IActorRef errorHubRelay,
        IServiceProvider serviceProvider
    )
    {
        return Akka.Actor.Props.Create(
            () => new LobbySupervisor(lobbyHubRelay, errorHubRelay, serviceProvider)
        );
    }
}
