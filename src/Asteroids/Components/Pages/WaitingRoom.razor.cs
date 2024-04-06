using Asteroids.Shared;

namespace Asteroids;

public partial class WaitingRoom : ILobbyClient
{
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
