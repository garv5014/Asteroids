using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.Cluster.Tools.Singleton;
using Akka.DependencyInjection;
using Akka.Event;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Observability;
using RealTimeCommunication;
using RealTimeCommunication.Actors;
using RealTimeCommunication.Actors.Hub;
using RealTimeCommunication.Actors.Session;
using RealTimeCommunication.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
var raftConnection = new RaftConnectionOptions();
var actorOptions = new ActorOptions();
builder.Configuration.GetRequiredSection(nameof(ActorOptions)).Bind(actorOptions);

// builder.Configuration.GetRequiredSection(nameof(RaftConnectionOptions)).Bind(raftConnection);
// builder.Services.AddSingleton(raftConnection);
// builder.Services.AddHttpClient(
//     "Raft",
//     client =>
//         client.BaseAddress = new Uri(
//             builder.Configuration.GetSection(nameof(RaftConnectionOptions))["GatewayUrl"]
//                 ?? throw new InvalidOperationException("GatewayUrl address not found.")
//         )
// );

// builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Raft"));
// builder.Services.AddHttpClient<IGatewayClient, GatewayService>();
// builder.Services.AddScoped<IUserPersistence, UserPersistanceService>();

builder.AddObservability();

builder.Services.AddAkka(
    ActorHelper.ProjectName,
    configBuilder =>
    {
        configBuilder
            .WithRemoting(actorOptions.ActorHostName, 0)
            .WithClustering(
                new ClusterOptions()
                {
                    Roles = actorOptions.ActorRoles.Split(
                        ",",
                        StringSplitOptions.RemoveEmptyEntries
                    ),
                    SeedNodes = actorOptions.ActorSeeds.Split(
                        ",",
                        StringSplitOptions.RemoveEmptyEntries
                    ),
                }
            )
            .ConfigureLoggers(
                (setup) =>
                {
                    setup.AddLoggerFactory();
                }
            )
            .WithActors(
                (system, registry) =>
                {
                    var selfMember = Cluster.Get(system).SelfMember;

                    if (selfMember.HasRole("SignalR"))
                    {
                        var lobbySupervisorProxy = system.ActorOf(
                            ClusterSingletonProxy.Props(
                                singletonManagerPath: $"/user/{ActorHelper.LobbySupervisorName}",
                                settings: ClusterSingletonProxySettings
                                    .Create(system)
                                    .WithRole("Lobbies")
                            ),
                            name: "lobbySupervisorProxy"
                        );

                        registry.TryRegister<AccountHubRelay>(
                            system.ActorOf(
                                AccountHubRelay.Props(),
                                ActorHelper.AccountRelayActorName
                            )
                        );
                        registry.TryRegister<LobbyHubRelay>(
                            system.ActorOf(LobbyHubRelay.Props(), ActorHelper.LobbyRelayActorName)
                        );
                        var ss = system.ActorOf(
                            SessionSupervisor.Props(lobbySupervisorProxy),
                            ActorHelper.SessionSupervisorName
                        );
                        registry.TryRegister<SessionSupervisor>(ss);
                        registry.TryRegister<LobbySupervisor>(lobbySupervisorProxy);
                    }

                    if (selfMember.HasRole("Lobbies"))
                    {
                        // Setup Singletons for the Lobbies role
                    }

                    var deadLetterProps = DependencyResolver.For(system).Props<DeadLetterActor>();
                    var deadLetterActor = system.ActorOf(deadLetterProps, "deadLetterActor");
                    system.EventStream.Subscribe(deadLetterActor, typeof(DeadLetter));
                    system.EventStream.Subscribe(deadLetterActor, typeof(UnhandledMessage));
                    system.EventStream.Subscribe(deadLetterActor, typeof(AllDeadLetters));
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

app.UseHttpsRedirection();

app.MapHub<AccountHub>("/accountHub");
app.MapHub<LobbyHub>("/lobbyHub");

app.Run();
