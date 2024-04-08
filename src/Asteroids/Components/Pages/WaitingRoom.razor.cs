using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class WaitingRoom : ILobbyClient
{
    [Parameter]
    public int LobbyId { get; set; }
    private HubConnection connection;
    private ILobbyHub hubProxy;

    private LobbyState lobbyState;

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
        
        NavigationManager.NavigateTo($"/playarea/{LobbyId}");
        
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
