using Akka.Actor;
using Akka.TestKit.Xunit2;
using Asteroids.Shared;
using Moq;
using RealTimeCommunication;

namespace Actor.UnitTests;

public class LobbySupervisorTest : TestKit
{
    // CreateLobbyMessage fails if the lobby name already exists
    [Fact]
    public void TestCreateLobbyMessageWithExistingName()
    {
        // Arrange
        var lobbyRelayActorRef = CreateTestProbe();
        var errorHubActorRef = CreateTestProbe();
        var serviceProvider = new Mock<IServiceProvider>();
        var lobbySupervisor = Sys.ActorOf(
            LobbySupervisor.Props(
                lobbyRelayActorRef.Ref,
                errorHubActorRef.Ref,
                serviceProvider.Object
            )
        );

        // Act
        var createLobbyMessage1 = new CreateLobbyMessage("lobby1", "", "");
        lobbySupervisor.Tell(createLobbyMessage1);

        var createLobbyMessage2 = new CreateLobbyMessage("lobby1", "", "");
        lobbySupervisor.Tell(createLobbyMessage2);

        // Assert
        ExpectNoMsg(); // Verify that no messages are received by the test probe

        // Verify that only one lobby actor is created and added to the dictionaries
        var lobbyActorRef = lobbySupervisor.Ask<IActorRef>(new GetLobbyMessage("lobby1")).Result;
        Assert.NotNull(lobbyActorRef);

        var lobbyActorRef2 = lobbySupervisor.Ask<IActorRef>(new GetLobbyMessage("lobby2")).Result;
        Assert.Null(lobbyActorRef2);
    }
}
