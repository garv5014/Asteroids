using Asteroids.Client;
using Microsoft.AspNetCore.SignalR.Client;
using RealTimeCommunication;

namespace Asteroids.Components.Pages;

public partial class Home : IAsteroidClient
{
    public string message = string.Empty;
    private IActorHub hubProxy = default!;
    private HubConnection connection = default!;

    protected override async Task OnInitializedAsync()
    {
        message = "Hello, world!";
        connection = new HubConnectionBuilder().WithUrl(SignalREnv.ActorHubUrl).Build();
        hubProxy = connection.ServerProxy<IActorHub>();
        _ = connection.ClientRegistration<IAsteroidClient>(this);
        await connection.StartAsync();

        await hubProxy.TellActor("chris", "Message");
        StateHasChanged();
    }

    public Task ReceiveActorMessage(string Message)
    {
        Console.WriteLine(Message);
        message = Message;
        StateHasChanged();
        return Task.CompletedTask;
    }
}
