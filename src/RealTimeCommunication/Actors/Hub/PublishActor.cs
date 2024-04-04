using Akka.Actor;
using Akka.Event;
using Microsoft.AspNetCore.SignalR.Client;

namespace RealTimeCommunication.Actors.Hub;

public class PublishActor : ReceiveActor
{
    private readonly string hubUrl;
    internal HubConnection hubConnection;
    protected ILoggingAdapter Log { get; } = Context.GetLogger();

    public PublishActor(string hubUrl)
    {
        this.hubUrl = hubUrl;
    }

    protected override void PreStart()
    {
        base.PreStart();
#pragma warning disable CS4014
        EstablishConnection().Wait();
#pragma warning restore CS4014
    }

    private async Task EstablishConnection()
    {
        hubConnection = new HubConnectionBuilder().WithUrl(hubUrl).Build();

        try
        {
            await hubConnection.StartAsync();
            Log.Info("SignalR connection established.");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to establish SignalR connection: {ex.Message}");
        }

        hubConnection.Closed += async (error) =>
        {
            Log.Warning("Connection closed. Trying to reconnect...");
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await EstablishConnection();
        };
    }

    private async Task EnsureConnectedAndExecute(Func<Task> action)
    {
        if (hubConnection.State != HubConnectionState.Connected)
        {
            await EstablishConnection();
        }
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log.Error($"Error executing action: {ex.Message}");
        }
    }

    internal void ExecuteAndPipeToSelf(Func<Task> action)
    {
        EnsureConnectedAndExecute(async () =>
            {
                await action();
            })
            .PipeTo(Self);
    }

    protected override void PostStop()
    {
        base.PostStop();
        hubConnection?.DisposeAsync();
    }

    public static Props Props(string hubUrl)
    {
        return Akka.Actor.Props.Create(() => new PublishActor(hubUrl));
    }
}
