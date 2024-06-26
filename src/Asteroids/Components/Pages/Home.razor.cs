﻿using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Asteroids.Components.Pages;

public partial class Home : IAccountClient
{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
    private IAccountHub hubProxy = default!;
    private HubConnection connection = default!;

    private string sessionActorPath = string.Empty;

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
        connection.ClientRegistration<IAccountClient>(this);
        await connection.StartAsync();

        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await Task.CompletedTask;
        sessionActorPath = await LocalStorage.GetItemAsync<string>("actorPath");
    }

    public async Task HandleLoginResponse(LoginResponseMessage message)
    {
        if (message?.Success ?? false)
        {
            ToastService.ShowSuccess("Login Success");
            await LocalStorage.SetItemAsync("actorPath", message.SessionActorPath);
            Console.WriteLine("Client Message Account {0}", message.Message);
            NavManager.NavigateTo("/lobby");
            return;
        }
        else
        {
            ToastService.ShowError("Login Failed: " + message?.Message);
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Login()
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ToastService.ShowError("Username and Password are required");
            return;
        }
        hubProxy.LoginCommand(new LoginMessage(username, password, "", sessionActorPath));
    }
}
