using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;
using Observability;

namespace RealTimeCommunication;

public record GetLobbyMessage(string LobbyName);

public record GetLobbyResponse(IActorRef LobbyActorRef);

public class LobbySupervisor : ReceiveActor
{
    private Dictionary<string, (IActorRef, LobbySnapShot?)> nameToActorRef =
        new Dictionary<string, (IActorRef, LobbySnapShot?)>();

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private IActorRef _lobbyRelayActor;

    private ILogger<LobbySupervisor> _logger;

    private IActorRef _errorHubActor;

    private IActorRef _lobbyPersistanceActor;

    public LobbySupervisor(
        IActorRef lobbyRelayActorRef,
        IActorRef errorHubActorRef,
        IActorRef lobbyPersistanceActorRef,
        IServiceProvider serviceProvider
    )
    {
        Receives();

        var scope = serviceProvider.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<LobbySupervisor>>();
        _lobbyRelayActor = lobbyRelayActorRef;
        _errorHubActor = errorHubActorRef;
        _lobbyPersistanceActor = lobbyPersistanceActorRef;
    }

    private void Receives()
    {
        Receive<CreateLobbyMessage>(CreateLobby);
        Receive<JoinLobbyMessage>(JoinLobby);
        Receive<UpdateLobbyMessage>(UpdateLobby);
        Receive<GetLobbiesMessage>(GetLobbies);
        Receive<GetLobbyStateMessage>(GetLobbyState);
        Receive<LobbyStateResponse>(HandleStateResponse);
        Receive<GetLobbyMessage>(HandleGetLobbyMessage);
        Receive<ErrorMessage>(msg => _errorHubActor.Tell(msg));
        Receive<StoreLobbyInformationMessage>(SaveAndSendState);
        Receive<RehydrateLobbySupervisorMessage>(RehydrateSupervisor);
        Receive(
            (Action<Terminated>)(
                msg =>
                {
                    HandleTermination(msg);
                }
            )
        );
    }

    private void HandleTermination(Terminated msg)
    {
        var lobbyActor = msg.ActorRef;
        var lobbyName = lobbyActor.Path.Name;
        var lobby = nameToActorRef[lobbyName];
        var actor = Context.ActorOf(
            LobbyActor.Props(lobbyName, lobby.Item2.LobbyOwner),
            ActorHelper.SanitizeActorName(lobbyName)
        );
        Context.Watch(actor);
        nameToActorRef[lobbyName] = (actor, lobby.Item2);
        actor.Tell(new RehydrateLobbyMessage(lobby.Item2));
        _log.Info("Lobby {0}", lobbyName);
        DiagnosticsConfig.TotalLobbies.Add(-1);
    }

    private void RehydrateSupervisor(RehydrateLobbySupervisorMessage message)
    {
        foreach (var lobby in message.Lobbies)
        {
            var actor = Context.ActorOf(
                LobbyActor.Props(lobby.Value.LobbyName, lobby.Value.LobbyOwner),
                ActorHelper.SanitizeActorName(lobby.Value.LobbyName)
            );
            Context.Watch(actor);
            nameToActorRef[lobby.Value.LobbyName] = (actor, lobby.Value);
            actor.Tell(new RehydrateLobbyMessage(lobby.Value));
            _log.Info("Lobby {0}", lobby.Value.LobbyName);
            DiagnosticsConfig.TotalLobbies.Add(1);
        }
    }

    private void SaveAndSendState(StoreLobbyInformationMessage msg)
    {
        _log.Info("Saving lobby state");
        _lobbyPersistanceActor.Tell(msg);
        nameToActorRef[msg.LobbySnapShot.LobbyName] = (
            nameToActorRef[msg.LobbySnapShot.LobbyName].Item1,
            msg.LobbySnapShot
        );
    }

    private void HandleGetLobbyMessage(GetLobbyMessage msg)
    {
        if (!nameToActorRef.TryGetValue(msg.LobbyName, out var lobbyActor))
        {
            _log.Info("Lobby with id {0} does not exist", msg.LobbyName);
            return;
        }

        Sender.Tell(new GetLobbyResponse(LobbyActorRef: lobbyActor.Item1));
    }

    private void UpdateLobby(UpdateLobbyMessage msg)
    {
        if (!nameToActorRef.TryGetValue(msg.LobbyName, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyName);
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
        if (!nameToActorRef.TryGetValue(msg.LobbyName, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyName);
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

        foreach (var lobby in nameToActorRef)
        {
            var gl = lobby
                .Value.Item1.Ask<GameLobby>(
                    new GetLobbiesMessage(
                        SessionActorPath: msg.SessionActorPath,
                        ConnectionId: msg.ConnectionId
                    )
                )
                .Result;
            var glId = new GameLobby(gl.Name, gl.PlayerCount);
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
        if (!nameToActorRef.TryGetValue(msg.LobbyName, out var lobbyActor))
        {
            // Eventually needs to send action failed message
            _log.Info("Lobby with id {0} does not exist", msg.LobbyName);
            return;
        }

        lobbyActor.Item1.Forward(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName
            )
        );

        _lobbyRelayActor.Tell(
            new JoinLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName
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

        var lobbyActor = Context.ActorOf(
            LobbyActor.Props(msg.LobbyName, msg.SessionActorPath),
            ActorHelper.SanitizeActorName(msg.LobbyName)
        );

        nameToActorRef.Add(msg.LobbyName, (lobbyActor, null));
        Context.Watch(lobbyActor);

        _logger.LogInformation("Lobby created with name {0}", msg.LobbyName);

        Self.Forward(
            new JoinLobbyMessage(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName
            )
        );

        _lobbyRelayActor.Tell(
            new CreateLobbyResponse(
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                LobbyName: msg.LobbyName
            )
        );
        DiagnosticsConfig.TotalLobbies.Add(1);
    }

    protected override void PreStart()
    {
        _lobbyPersistanceActor.Tell(new GetLobbyInformationMessage());
        _log.Info("LobbySupervisor created");
    }

    protected override void PostStop()
    {
        _log.Info("LobbySupervisor stopped");
    }

    // protected override SupervisorStrategy SupervisorStrategy()
    // {
    //     return new OneForOneStrategy(
    //         maxNrOfRetries: 10,
    //         withinTimeRange: TimeSpan.FromMinutes(1),
    //         decider: Decider.From(x =>
    //         {
    //             switch (x)
    //             {
    //                 case Exception _:
    //                     return Directive.Restart;
    //                 default:
    //                     return Directive.Escalate;
    //             }
    //         })
    //     );
    // }

    public static Props Props(
        IActorRef lobbyHubRelay,
        IActorRef errorHubRelay,
        IActorRef lobbyPersistanceActor,
        ActorSystem actorSystem
    )
    {
        var dR = DependencyResolver.For(actorSystem);
        return dR.Props<LobbySupervisor>(lobbyHubRelay, errorHubRelay, lobbyPersistanceActor);
    }

    public static Props Props(
        IActorRef lobbyHubRelay,
        IActorRef errorHubRelay,
        IActorRef lobbyPersistanceActor,
        IServiceProvider serviceProvider
    )
    {
        return Akka.Actor.Props.Create(
            () =>
                new LobbySupervisor(
                    lobbyHubRelay,
                    errorHubRelay,
                    lobbyPersistanceActor,
                    serviceProvider
                )
        );
    }
}

public record RehydrateLobbyMessage(LobbySnapShot LobbySnapShot);

public record RehydrateLobbySupervisorMessage(Dictionary<string, LobbySnapShot> Lobbies);
