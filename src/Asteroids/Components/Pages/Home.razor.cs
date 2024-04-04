using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Home : IAsteroidClientHub
{
    public string message;
    private IAccountHub hubProxy = default!;
    private HubConnection connection = default!;

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder().WithUrl(SignalREnv.AccountHubUrl).Build();
        try
        {
            hubProxy = connection.ServerProxy<IAccountHub>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to establish SignalR connection: {ex.Message}");
            throw;
        }
        _ = connection.ClientRegistration<IAsteroidClientHub>(this);
        await connection.StartAsync();

        await hubProxy.TellActor("chris", "Message");
        StateHasChanged();
    }

    public Task HandleActorMessage(string Message)
    {
        Console.WriteLine("Client Message {0}", Message);
        message = Message;

        return Task.Run(() => StateHasChanged());
    }

    public Task HandleCreateAccountResponse(CreateAccountResponseMessage message)
    {
        this.message = message?.Message ?? "Message is null";
        Console.WriteLine("Client Message Account {0}", message);
        StateHasChanged();
        return Task.CompletedTask;
    }

    public void CreateAccount()
    {
        hubProxy.CreateAccountTell(
            new CreateAccountMessage() { User = "Bill", Password = "Hidden" }
        );
    }

    public Task HandleLoginResponse(LoginResponseMessage message)
    {
        throw new NotImplementedException();
    }
}
