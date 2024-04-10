using System.Timers;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Timer = System.Timers.Timer;

namespace Asteroids.Components.Pages;

public partial class WaitingRoom : ILobbyClient
{
    [Parameter]
    public int LobbyId { get; set; }
    private HubConnection connection;
    private ILobbyHub hubProxy;
    private LobbyState lobbyState;
    private Timer timer;
    private HashSet<string> pressedKeys = new HashSet<string>();

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder()
            .WithUrl(signalREnv.LobbyHubUrl) // Make sure SignalREnv provides the correct URL for your LobbyHub
            .Build();

        try
        {
            hubProxy = connection.ServerProxy<ILobbyHub>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to establish SignalR connection LobbyHub: {ex.Message}");
            throw;
        }
        connection.ClientRegistration<ILobbyClient>(this);
        await connection.StartAsync();

        await GetLobbyState();
        await InvokeAsync(StateHasChanged);
    }

    private async Task StartGame()
    {
        // Implement this method to start the game
        ToastService.ShowSuccess("Game started!");

        await hubProxy.UpdateLobbyStateCommand(
            new UpdateLobbyMessage(
                SessionActorPath: await localStorage.GetItemAsync<string>("actorPath"),
                ConnectionId: string.Empty,
                LobbyId: LobbyId,
                NewStatus: LobbyStatus.InGame
            )
        );

        SetTimer();

        await InvokeAsync(StateHasChanged);
    }

    private async Task GetLobbyState()
    {
        try
        {
            // Replace "GetLobbyState" with the actual method name you're using in your Hub to request lobby state
            var actorPath = await localStorage.GetItemAsync<string>("actorPath");
            await hubProxy.LobbyStateQuery(
                new GetLobbyStateMessage(
                    SessionActorPath: actorPath,
                    ConnectionId: string.Empty,
                    LobbyId: LobbyId
                )
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when requesting lobby state: {ex.Message}");
            // Optionally, handle exceptions (e.g., show a message to the user)
        }
        await InvokeAsync(StateHasChanged);
    }

    private void SetTimer()
    {
        timer = new Timer(100);
        timer.Elapsed += PublishClientState;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async void PublishClientState(object? sender, ElapsedEventArgs e)
    {
        var thrust = pressedKeys.Contains("w");
        var left = pressedKeys.Contains("a") && !pressedKeys.Contains("d");
        var right = pressedKeys.Contains("d") && !pressedKeys.Contains("a");

        Console.WriteLine($"Sending ship state: thrust: {thrust}, left: {left}, right: {right}");
        var path = await localStorage.GetItemAsync<string>("actorPath");
        await hubProxy.UpdateShipCommand(
            new UpdateShipMessage(
                ConnectionId: string.Empty,
                SessionActorPath: path,
                ShipParams: new UpdateShipParams(thrust, left, right),
                LobbyId: LobbyId
            )
        );
    }

    private void HandleKeyPress(HashSet<string> pressedKeySet)
    {
        Console.WriteLine($"Pressed keys: {string.Join(", ", pressedKeySet)}");
        pressedKeys = pressedKeySet;
    }

    public Task HandleCreateLobbyResponse(CreateLobbyResponse message)
    {
        return Task.CompletedTask;
    }

    public Task HandleJoinLobbyResponse(JoinLobbyResponse message)
    {
        return Task.CompletedTask;
    }

    public Task HandleLobbiesResponse(AllLobbiesResponse message)
    {
        return Task.CompletedTask;
    }

    public async Task HandleLobbyStateResponse(LobbyStateResponse message)
    {
        lobbyState = message.CurrentState;
        Console.WriteLine($"Received lobby state: {message.CurrentState.Ships.Count}");
        await InvokeAsync(StateHasChanged);
    }
}
