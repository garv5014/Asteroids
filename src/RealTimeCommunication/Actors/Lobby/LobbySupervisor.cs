using Akka.Actor;
using Akka.Event;
using Asteroids.Shared;
using Asteroids.Shared.Messages;

namespace RealTimeCommunication;

public record GetLobbyMessage(int LobbyId);

public record GetLobbyResponse(IActorRef LobbyActorRef);

public class LobbySupervisor : ReceiveActor
{
    // return all the lobbies lobbies stored as a Dictionary of lobbies
    // Create a lobby actor
    // Forward messages to the lobby actor
    // Join Lobby passed to lobby actor

    private Dictionary<int, IActorRef> idToActorRef = new Dictionary<int, IActorRef>();
    private Dictionary<string, IActorRef> nameToActorRef = new Dictionary<string, IActorRef>();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private int lobbyId = 0;
    private IActorRef _lobbyRelayActor;

    private ILogger<LobbySupervisor> _logger;

    private IActorRef _errorHubActor;

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
            var lobbyId = idToActorRef.FirstOrDefault(x => x.Value == lobbyActor).Key;
            var actor = Context.ActorOf(
                LobbyActor.Props(lobbyName, ""),
                ActorHelper.SanitizeActorName(lobbyName)
            );
            idToActorRef[lobbyId] = actor;
            nameToActorRef[lobbyName] = actor;
            _log.Info("Lobby {0} with id {1} terminated", lobbyName, lobbyId);
        });

        var scope = serviceProvider.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<LobbySupervisor>>();
        _lobbyRelayActor = lobbyRelayActorRef;
        _errorHubActor = errorHubActorRef;
    }

    private void HandleGetLobbyMessage(GetLobbyMessage msg)
    {
        if (!idToActorRef.TryGetValue(msg.LobbyId, out var lobbyActor))
        {
            _log.Info("Lobby with id {0} does not exist", msg.LobbyId);
            return;
        }

        Sender.Tell(new GetLobbyResponse(LobbyActorRef: lobbyActor));
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

        Sender.Tell(new GetLobbyResponse(LobbyActorRef: lobbyActor));
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
        lobbyActor.Tell(msg);
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
                .Value.Ask<GameLobby>(
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

        lobbyActor.Forward(
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

        idToActorRef.Add(lobbyId, lobbyActor);
        nameToActorRef.Add(msg.LobbyName, lobbyActor);
        Context.Watch(lobbyActor);

        _log.Info("Lobby created with id {0}", lobbyId);

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
        IServiceProvider serviceProvider
    )
    {
        return Akka.Actor.Props.Create(
            () => new LobbySupervisor(lobbyHubRelay, errorHubRelay, serviceProvider)
        );
    }
}
