using Asteroids.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Home : IAsteroidClient
{
    public string message;
    private IActorHub hubProxy = default!;
    private HubConnection connection = default!;

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder().WithUrl(SignalREnv.ActorHubUrl).Build();
        try
        {
            hubProxy = connection.ServerProxy<IActorHub>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to establish SignalR connection: {ex.Message}");
            throw;
        }
        _ = connection.ClientRegistration<IAsteroidClient>(this);
        await connection.StartAsync();

        await hubProxy.TellActor("chris", "Message");
        StateHasChanged();
    }

    public Task ReceiveActorMessage(string Message)
    {
        Console.WriteLine(Message);
        message = Message;

        return Task.Run(() => StateHasChanged());
    }
}
