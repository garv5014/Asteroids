using Asteroids.Shared;
using Microsoft.AspNetCore.Components;

namespace Asteroids.Components.Pages;

public partial class WaitingRoom : ILobbyClient
{
    //
    [Parameter]
    public int LobbyId { get; set; }

    public Task HandleCreateLobbyResponse(CreateLobbyResponse message)
    {
        throw new NotImplementedException();
    }

    public Task HandleJoinLobbyResponse(JoinLobbyResponse message)
    {
        throw new NotImplementedException();
    }

    public Task HandleLobbiesResponse(AllLobbiesResponse message)
    {
        throw new NotImplementedException();
    }
}
