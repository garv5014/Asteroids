using System;
using System.Threading.Tasks;
using Asteroids.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Lobby : ILobbyClient
{
    private HubConnection connection;
    private GameLobby[] lobbies;
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

    private async Task JoinLobby(string lobbyId) { }

    private async Task CreateLobby() { }

    public Task HandleJoinLobbyResponse(JoinLobbyResponse message)
    {
        throw new NotImplementedException();
    }

    public Task HandleCreateLobbyResponse(CreateLobbyResponse message)
    {
        throw new NotImplementedException();
    }
}
