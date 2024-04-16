using Akka.Actor;
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

internal class Program
{
    public static void Main(string[] args)
    {
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
                                // create lobby persistance singleton
                                var lobbySupervisorProxy = system.ActorOf(
                                    ClusterSingletonProxy.Props(
                                        singletonManagerPath: $"/user/{ActorHelper.LobbySupervisorName}",
                                        settings: ClusterSingletonProxySettings
                                            .Create(system)
                                            .WithRole("Lobbies")
                                    ),
                                    name: "lobbySupervisorProxy"
                                );

                                var accountHubRelay = system.ActorOf(
                                    AccountHubRelay.Props(),
                                    ActorHelper.AccountRelayActorName
                                );
                                registry.TryRegister<AccountHubRelay>(accountHubRelay);

                                var lobbyHubRelay = system.ActorOf(
                                    LobbyHubRelay.Props(),
                                    ActorHelper.LobbyRelayActorName
                                );

                                registry.TryRegister<LobbyHubRelay>(lobbyHubRelay);

                                var ss = system.ActorOf(
                                    SessionSupervisor.Props(lobbySupervisorProxy, accountHubRelay),
                                    ActorHelper.SessionSupervisorName
                                );

                                registry.TryRegister<SessionSupervisor>(ss);
                                registry.TryRegister<LobbySupervisor>(lobbyHubRelay);
                            }

                            if (selfMember.HasRole("Lobbies"))
                            {
                                AddSingletons(system, registry);
                            }

                            var deadLetterProps = DependencyResolver
                                .For(system)
                                .Props<DeadLetterActor>();
                            var deadLetterActor = system.ActorOf(
                                deadLetterProps,
                                "deadLetterActor"
                            );
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

        if (actorOptions.ActorRoles.Contains("SignalR"))
        {
            app.MapHub<AccountHub>("/accountHub");
            app.MapHub<LobbyHub>("/lobbyHub");
        }

        app.Run();
    }

    private static IActorRef AddSingletons(ActorSystem system, IActorRegistry actorRegistry)
    {
        // create LobbyHubRelay singleton
        system.ActorOf(
            ClusterSingletonManager.Props(
                singletonProps: LobbyHubRelay.Props(),
                terminationMessage: PoisonPill.Instance,
                settings: ClusterSingletonManagerSettings.Create(system).WithRole("Lobbies")
            ),
            name: ActorHelper.LobbyRelayActorName
        );
        // create lobbies emitter proxy
        var lobbyHubRelay = system.ActorOf(
            ClusterSingletonProxy.Props(
                singletonManagerPath: $"/user/{ActorHelper.LobbyRelayActorName}",
                settings: ClusterSingletonProxySettings.Create(system).WithRole("Lobbies")
            ),
            name: "lobbyHubRelayProxy"
        );

        // create lobby supervisor singleton
        system.ActorOf(
            ClusterSingletonManager.Props(
                singletonProps: LobbySupervisor.Props(lobbyHubRelay: lobbyHubRelay),
                terminationMessage: PoisonPill.Instance,
                settings: ClusterSingletonManagerSettings.Create(system).WithRole("Lobbies")
            ),
            name: ActorHelper.LobbySupervisorName
        );
        // create lobby supervisor proxy
        var lobbySupervisorProxy = system.ActorOf(
            ClusterSingletonProxy.Props(
                singletonManagerPath: $"/user/{ActorHelper.LobbySupervisorName}",
                settings: ClusterSingletonProxySettings.Create(system).WithRole("Lobbies")
            ),
            name: "lobbySupervisorProxy"
        );
        actorRegistry.TryRegister<LobbySupervisor>(lobbySupervisorProxy);

        return lobbySupervisorProxy;
    }
}
