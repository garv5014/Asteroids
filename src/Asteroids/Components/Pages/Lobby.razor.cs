using Asteroids.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Lobby : ILobbyClient
{
    private HubConnection connection;
    private List<GameLobby> lobbies;
    private ILobbyHub hubProxy;
    private string newLobbyName = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder()
            .WithUrl(SignalREnv.LobbyHubUrl) // Make sure SignalREnv provides the correct URL for your LobbyHub
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

        await RequestLobbies();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RequestLobbies()
    {
        try
        {
            // Replace "GetLobbies" with the actual method name you're using in your Hub to request lobbies
            var actorPath = await LocalStorage.GetItemAsync<string>("actorPath");
            await hubProxy.LobbiesQuery(
                new GetLobbiesMessage(SessionActorPath: actorPath, ConnectionId: null)
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when requesting lobbies: {ex.Message}");
            // Optionally, handle exceptions (e.g., show a message to the user)
        }
    }

    public async Task HandleLobbiesResponse(AllLobbiesResponse message)
    {
        lobbies = message.Lobbies;
        await InvokeAsync(StateHasChanged); // Refresh UI with the received lobbies
    }

    private async Task JoinLobby(string lobbyName)
    {
        try
        {
            var actorPath = await LocalStorage.GetItemAsync<string>("actorPath");
            await hubProxy.JoinLobbyCommand(
                new JoinLobbyMessage(
                    SessionActorPath: actorPath,
                    ConnectionId: null,
                    LobbyName: lobbyName
                )
            );
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to join lobby: {ex.Message}");
            // Optionally, handle exceptions (e.g., show a message to the user)
        }
    }

    private async Task CreateLobby()
    {
        try
        {
            var actorPath = await LocalStorage.GetItemAsync<string>("actorPath");
            await hubProxy.CreateLobbyCommand(
                new CreateLobbyMessage(
                    SessionActorPath: actorPath,
                    LobbyName: newLobbyName,
                    ConnectionId: null
                )
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when creating lobby: {ex.Message}");
            // Optionally, handle exceptions (e.g., show a message to the user)
        }
    }

    public async Task HandleJoinLobbyResponse(JoinLobbyResponse message)
    {
        ToastService.ShowSuccess("Joined lobby");
        NavManager.NavigateTo($"/waitingroom/{message.LobbyName}");
        // on sucess navigate to waiting room passing in lobby Id
        await InvokeAsync(StateHasChanged);
    }

    public async Task HandleCreateLobbyResponse(CreateLobbyResponse message)
    {
        await InvokeAsync(OnInitializedAsync);
    }

    public Task HandleLobbyStateResponse(LobbyStateResponse message)
    {
        return Task.CompletedTask;
    }

    public Task HandleRefreshConnectionId()
    {
        return Task.CompletedTask;
    }
}
