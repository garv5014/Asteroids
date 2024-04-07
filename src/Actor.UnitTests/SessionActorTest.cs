using Akka.Actor;
using Akka.TestKit.Xunit2;
using Asteroids.Shared.Messages;
using FluentAssertions;
using RealTimeCommunication;
using RealTimeCommunication.Actors.Hub;
using RealTimeCommunication.Actors.Session;

namespace Actor.UnitTests;

public class SessionActorTest : TestKit
{
    // SessionSupervisor can receive a LoginMessage and create a new session actor successfully
    // SessionSupervisor can be created successfully
    [Fact]
    public void session_supervisor_can_be_created_successfully()
    {
        // Arrange
        var testProbe = CreateTestProbe();

        // Act
        var sessionSupervisor = Sys.ActorOf(SessionSupervisor.Props(testProbe.Ref));

        // Assert
        sessionSupervisor.Should().NotBeNull();
    }

    // SessionSupervisor can receive a LoginMessage and create a new SessionActor
    [Fact]
    public void SessionSupervisor_CanReceiveLoginMessageAndCreateNewSessionActor()
    {
        // Arrange
        var probe = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(SessionSupervisor.Props(probe.Ref));

        // Act
        sessionSupervisor.Tell(new LoginMessage("user1", "connection1", "", ""), probe.Ref);

        // Assert
        probe.ExpectMsg<LoginResponseMessage>();
    }

    // SessionSupervisor can receive a GetUserSessionMessage and return the correct session actor successfully
    [Fact]
    public async Task SessionSupervisor_CanReceiveGetUserSessionMessageAndReturnCorrectSessionActor()
    {
        // Arrange
        var probe = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(
            SessionSupervisor.Props(probe.Ref),
            ActorHelper.SessionSupervisorName
        );
        Sys.ActorOf(LobbySupervisor.Props(probe.Ref), ActorHelper.LobbySupervisorName);
        var loginMessage = new LoginMessage("user1", "connection1", "", "");
        sessionSupervisor.Tell(loginMessage, probe.Ref);

        await Task.Delay(100);

        probe.ExpectMsg<LoginResponseMessage>();

        // Act
        var getUserSessionMessage = new GetUserSessionMessage(probe.Ref.Path.ToString());
        sessionSupervisor.Tell(getUserSessionMessage, probe.Ref);
        await Task.Delay(100);
        // Assert
        probe.ExpectMsg<GetUserSessionResponse>();
    }

    // SessionSupervisor can send a LoginResponseMessage to the AccountRelayActor when a new SessionActor is created
    [Fact]
    public void SessionSupervisor_CanSendLoginResponseMessage()
    {
        // Arrange
        var probe = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(SessionSupervisor.Props(probe.Ref));
        var loginMessage = new LoginMessage("user1", "connection1", "", "");

        // Act
        sessionSupervisor.Tell(loginMessage, probe.Ref);

        // Assert
        probe.ExpectMsg<LoginResponseMessage>();
    }
}
