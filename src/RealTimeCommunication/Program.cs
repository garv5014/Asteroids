using Akka.Hosting;
using Observability;
using RealTimeCommunication;
using RealTimeCommunication.Actors.Hub;
using RealTimeCommunication.Actors.Session;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.AddObservability();

builder.Services.AddAkka(
    "AsteroidsSystem",
    configurationBuilder =>
    {
        configurationBuilder.WithActors(
            (system, registry) =>
            {
                var ss = system.ActorOf(
                    SessionSupervisor.Props(),
                    ActorHelper.SessionSupervisorName
                );
                registry.TryRegister<SessionSupervisor>(ss);
                var ls = system.ActorOf(LobbySupervisor.Props(), ActorHelper.LobbySupervisorName);
                registry.TryRegister<SessionSupervisor>(ls);
                registry.TryRegister<AccountHubRelay>(
                    system.ActorOf(AccountHubRelay.Props(), ActorHelper.AccountRelayActorName)
                );
                registry.TryRegister<LobbyHubRelay>(
                    system.ActorOf(LobbyHubRelay.Props(), ActorHelper.LobbyRelayActorName)
                );
            }
        );
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<AccountHub>("/accountHub");
app.MapHub<LobbyHub>("/lobbyHub");
app.Run();
