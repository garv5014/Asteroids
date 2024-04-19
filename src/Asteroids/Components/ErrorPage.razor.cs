using Asteroids;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components;

public partial class ErrorPage : IErrorClient
{
    public List<string> Errors { get; set; } = new();
    private string _errorMessage = "An error occurred. Please try again later.";
    private IErrorHub hubProxy = default!;
    private HubConnection connection = default!;

    public Task HandleError(ErrorMessage message)
    {
        ToastService.ShowInfo(message.Message);
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder().WithUrl(SignalREnv.ErrorHubUrl).Build();
        try
        {
            hubProxy = connection.ServerProxy<IErrorHub>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to establish SignalR connection: {ex.Message}");
            throw;
        }
        connection.ClientRegistration<IErrorClient>(this);
        await connection.StartAsync();

        await InvokeAsync(StateHasChanged);
    }
}
