namespace Actor.UnitTests;

using System.Collections.Generic;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using Asteroids.Shared;
using Asteroids.Shared.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RealTimeCommunication;
using Xunit;

public class LobbySupervisorTests : TestKit
{
    [Fact]
    public void test_create_lobby_duplicate_name()
    {
        var lobbyName = "existingLobby";

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILogger<LobbySupervisor>>(
            new Mock<ILogger<LobbySupervisor>>().Object
        );
        var mockServiceProvider = serviceCollection.BuildServiceProvider();

        var lobbyRelayActor = CreateTestProbe();
        var errorHubActor = CreateTestProbe();
        var lobbyPersistenceActor = CreateTestProbe();
        var lobbySupervisor = Sys.ActorOf(
            LobbySupervisor.Props(
                lobbyRelayActor.Ref,
                errorHubActor.Ref,
                lobbyPersistenceActor.Ref,
                mockServiceProvider
            )
        );

        lobbySupervisor.Tell(new CreateLobbyMessage(lobbyName, "sessionPath", ""));
        lobbySupervisor.Tell(new CreateLobbyMessage(lobbyName, "sessionPath", ""));

        errorHubActor.ExpectMsg<ErrorMessage>(msg => msg.Message == "Lobby already exists");
    }

    [Fact]
    public async void test_handle_get_lobby_message_valid_lobby()
    {
        var lobbyName = "validLobby";
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILogger<LobbySupervisor>>(
            new Mock<ILogger<LobbySupervisor>>().Object
        );
        var mockServiceProvider = serviceCollection.BuildServiceProvider();

        var senderActor = CreateTestProbe();
        var lobbyRelayActor = CreateTestProbe();
        var errorHubActor = CreateTestProbe();
        var lobbyPersistenceActor = CreateTestProbe();
        var lobbySupervisor = Sys.ActorOf(
            LobbySupervisor.Props(
                lobbyRelayActor.Ref,
                errorHubActor.Ref,
                lobbyPersistenceActor.Ref,
                mockServiceProvider
            )
        );

        lobbySupervisor.Tell(
            new CreateLobbyMessage(
                LobbyName: lobbyName,
                SessionActorPath: "sessionPath",
                ConnectionId: ""
            )
        );
        lobbySupervisor.Tell(new GetLobbyMessage(lobbyName), senderActor);

        Thread.Sleep(1000);

        senderActor.ExpectMsg<GetLobbyResponse>(response =>
            response.LobbyActorRef.Path.ToString().Contains(lobbyName)
        );
    }

    [Fact]
    public void test_handle_get_lobby_message_invalid_lobby()
    {
        var lobbyName = "nonExistentLobby";
        var mockServiceProvider = new Mock<IServiceProvider>();
        var lobbyRelayActor = CreateTestProbe();
        var errorHubActor = CreateTestProbe();
        var lobbyPersistenceActor = CreateTestProbe();
        var lobbySupervisor = Sys.ActorOf(
            LobbySupervisor.Props(
                lobbyRelayActor.Ref,
                errorHubActor.Ref,
                lobbyPersistenceActor.Ref,
                mockServiceProvider.Object
            )
        );

        lobbySupervisor.Tell(new GetLobbyMessage(lobbyName));

        ExpectNoMsg();
    }
}
