using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Microsoft.AspNetCore.Components;

namespace Asteroids.Components.Pages;

public partial class PlayArea : ILobbyClient
{
  [Parameter]
  public int LobbyId { get; set; }

  public List<Ship> Ships { get; set; } = [];
  public List<Asteroid> Asteroids { get; set; } = [];
  
  public Task HandleLobbiesResponse(AllLobbiesResponse message)
  {
    throw new NotImplementedException();
  }

  public Task HandleJoinLobbyResponse(JoinLobbyResponse message)
  {
    throw new NotImplementedException();
  }

  public Task HandleCreateLobbyResponse(CreateLobbyResponse message)
  {
    throw new NotImplementedException();
  }

  public Task HandleLobbyStateResponse(LobbyStateResponse message)
  {
    throw new NotImplementedException();
  }
}