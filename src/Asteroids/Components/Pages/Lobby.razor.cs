using Asteroids.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

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
    await connection.StartAsync();

    await RequestLobbies();
  }

  private async Task RequestLobbies()
  {
    try
    {
      // Replace "GetLobbies" with the actual method name you're using in your Hub to request lobbies
      await connection.InvokeAsync("GetLobbies");
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

  private async Task JoinLobby(string lobbyId)
  {
    try
    {
      // Replace "JoinLobby" with your actual Hub method for joining a lobby
      await connection.InvokeAsync("JoinLobby", lobbyId);
      // Optionally, navigate to the lobby-specific page or perform other actions upon joining
      NavManager.NavigateTo($"/lobby/join/{lobbyId}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Exception when joining lobby: {ex.Message}");
      // Optionally, handle exceptions (e.g., show a message to the user)
    }
  }

  private async Task CreateLobby()
  {
    if (!string.IsNullOrWhiteSpace(newLobbyName))
    {
      try
      {
        // Replace "CreateLobby" with your actual Hub method for creating a new lobby
        await connection.InvokeAsync("CreateLobby", newLobbyName);

        newLobbyName = string.Empty; // Reset input field
        await RequestLobbies(); // Refresh the lobby list
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Exception when creating a new lobby: {ex.Message}");
        // Optionally, handle exceptions (e.g., show a message to the user)
      }
    }
  }
}
