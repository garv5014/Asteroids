using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR;

namespace RealTimeCommunication;

public class ErrorHub : Hub<IErrorClient>, IErrorHub
{
    private ILogger<LobbyHub> _logger;

    public static string UrlPath = "/errorHub";
    public static string FullUrl = $"http://realtime:8080{UrlPath}";

    public ErrorHub(ILogger<LobbyHub> logger)
    {
        _logger = logger;
    }

    public Task ErrorPublish(ErrorMessage message)
    {
        _logger.LogInformation("Sending error message to client: {0}", message.Message);
        return Clients.All.HandleError(message);
    }
}
