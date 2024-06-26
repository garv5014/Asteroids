﻿using System.Timers;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Timer = System.Timers.Timer;

namespace Asteroids.Components.Pages;

public partial class WaitingRoom : ILobbyClient
{
    [Parameter]
    public string LobbyName { get; set; }
    private HubConnection connection;
    private string SessionActorPath { get; set; }
    private ILobbyHub hubProxy;
    private GameSnapShot gameState;
    private Timer timer;
    private HashSet<string> pressedKeys = new HashSet<string>();
    private string shipColor = "white";
    private string projectileColor = "white";

    protected override async Task OnInitializedAsync()
    {
        connection = new HubConnectionBuilder()
            .WithUrl(signalREnv.LobbyHubUrl) // Make sure SignalREnv provides the correct URL for your LobbyHub
            .Build();

        try
        {
            hubProxy = connection.ServerProxy<ILobbyHub>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to establish SignalR connection LobbyHub: {ex.Message}");
            throw;
        }
        connection.ClientRegistration<ILobbyClient>(this);
        await connection.StartAsync();
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetLobbyState();
        }

        SessionActorPath = await localStorage.GetItemAsync<string>("actorPath");
        await localStorage.SetItemAsync("shipColor", shipColor);
        await localStorage.SetItemAsync("projectileColor", projectileColor);
        await InvokeAsync(StateHasChanged);
    }

    private async Task StartGame()
    {
        // Implement this method to start the game
        ToastService.ShowSuccess("Game started!");

        await hubProxy.UpdateLobbyStateCommand(
            new UpdateLobbyMessage(
                SessionActorPath: await localStorage.GetItemAsync<string>("actorPath"),
                ConnectionId: string.Empty,
                LobbyName: LobbyName,
                NewStatus: LobbyStatus.InGame
            )
        );
        await InvokeAsync(StateHasChanged);
    }

    private async Task GetLobbyState()
    {
        try
        {
            // Replace "GetLobbyState" with the actual method name you're using in your Hub to request lobby state
            var actorPath = await localStorage.GetItemAsync<string>("actorPath");
            await hubProxy.LobbyStateQuery(
                new GetLobbyStateMessage(
                    SessionActorPath: actorPath,
                    ConnectionId: string.Empty,
                    LobbyName: LobbyName
                )
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception when requesting lobby state: {ex.Message}");
            // Optionally, handle exceptions (e.g., show a message to the user)
        }
        await InvokeAsync(StateHasChanged);
    }

    private void SetTimer()
    {
        timer = new Timer(100);
        timer.Elapsed += PublishClientState;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async void PublishClientState(object? sender, ElapsedEventArgs e)
    {
        var thrust = pressedKeys.Contains("w");
        var left = pressedKeys.Contains("a") && !pressedKeys.Contains("d");
        var right = pressedKeys.Contains("d") && !pressedKeys.Contains("a");
        var shoot = pressedKeys.Contains(" ");

        await hubProxy.UpdateShipCommand(
            new UpdateShipMessage(
                ConnectionId: string.Empty,
                SessionActorPath: SessionActorPath,
                ShipParams: new UpdateShipParams(thrust, left, right, shoot),
                LobbyName: LobbyName
            )
        );
    }

    private void HandleKeyPress(HashSet<string> pressedKeySet)
    {
        pressedKeys = pressedKeySet;
    }

    public Task HandleCreateLobbyResponse(CreateLobbyResponse message)
    {
        return Task.CompletedTask;
    }

    public Task HandleJoinLobbyResponse(JoinLobbyResponse message)
    {
        return Task.CompletedTask;
    }

    public Task HandleLobbiesResponse(AllLobbiesResponse message)
    {
        return Task.CompletedTask;
    }

    public async Task HandleLobbyStateResponse(LobbyStateResponse message)
    {
        if (message.CurrentState.CurrentStatus == LobbyStatus.InGame && timer == null)
        {
            SetTimer();
        }
        else if (message.CurrentState.CurrentStatus != LobbyStatus.InGame && timer != null)
        {
            timer.Stop();
        }
        gameState = message.CurrentState;
        // Console.WriteLine($"Received lobby state: {message.ConnectionId}");
        await InvokeAsync(StateHasChanged);
    }

    public async Task HandleRefreshConnectionId()
    {
        await hubProxy.RefreshConnectionIdCommand(
            new RefreshConnectionIdMessage(
                ConnectionId: string.Empty,
                SessionActorPath: SessionActorPath
            )
        );
    }

    private async Task KillLobby()
    {
        await hubProxy.KillLobbyCommand(
            new KillLobbyMessage(
                SessionActorPath: SessionActorPath,
                ConnectionId: string.Empty,
                LobbyName: LobbyName
            )
        );
    }

    private async Task Reload()
    {
        await OnInitializedAsync();
        Console.WriteLine("Reloaded {0}", SessionActorPath);
        await hubProxy.RefreshConnectionIdCommand(
            new RefreshConnectionIdMessage(
                ConnectionId: string.Empty,
                SessionActorPath: SessionActorPath
            )
        );
    }

    private async void OnShipColorChanged(string color)
    {
        shipColor = color;
        await localStorage.SetItemAsync("shipColor", color);
    }

    private async void OnBulletColorChanged(string color)
    {
        projectileColor = color;
        await localStorage.SetItemAsync("projectileColor", color);
    }
}
