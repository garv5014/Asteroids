using System.Timers;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;
using Blazored.Toast.Services;
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
    private Ship localPlayer;
    private UpdateShipParams shipParams;

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
        timer = new Timer(10);
        timer.Elapsed += PublishClientState;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async void PublishClientState(object? sender, ElapsedEventArgs e)
    {
        await hubProxy.UpdateShipCommand(
            new UpdateShipMessage(
                ConnectionId: string.Empty,
                SessionActorPath: string.Empty,
                ShipParams: new UpdateShipParams(false, false, false)
            )
        );
    }
    
    public void HandleKeyPress(UpdateShipParams newParams)
    {
        shipParams = newParams;
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
        await InvokeAsync(StateHasChanged);
    }
}
