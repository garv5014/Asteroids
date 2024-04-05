using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Home : IAccountClient
{
    public string message;
    public string username { get; set; } = "";
    public string password { get; set; } = "";
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
        _ = connection.ClientRegistration<IAccountClient>(this);
        await connection.StartAsync();

        StateHasChanged();
    }

    public async Task HandleActorMessage(string Message)
    {
        Console.WriteLine("Client Message {0}", Message);
        message = Message;

        await InvokeAsync(StateHasChanged);
    }

    public async Task HandleLoginResponse(LoginResponseMessage message)
    {
        this.message = message?.Message ?? "Message is null";
        if (message?.Success ?? false)
        {
            ToastService.ShowSuccess("Login Success");
        }
        else
        {
            ToastService.ShowError("Login Failed");
            return;
        }

        await LocalStorage.SetItemAsync("actorPath", message.SessionActorPath);

        Console.WriteLine("Client Message Account {0}", message.Message);
        await InvokeAsync(StateHasChanged);
    }

    public void Login()
    {
        hubProxy.LoginCommand(
            new LoginMessage(username, password, connection.ConnectionId, null)
        );
    }
}
