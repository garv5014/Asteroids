using Akka.Actor;
using Akka.TestKit.Xunit2;
using Asteroids.Shared.Messages;
using RealTimeCommunication;
using RealTimeCommunication.Actors.Session;

namespace Actor.UnitTests;

public class SessionSupervisorTests : TestKit
{
    [Fact]
    public void TestGetUserSessionMessageHandling()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var accountRelayMock = CreateTestProbe();
        var accountPersistenceMock = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(
            SessionSupervisor.Props(
                lobbySupervisorMock.Ref,
                accountRelayMock.Ref,
                accountPersistenceMock
            )
        );

        var userSessionActor = CreateTestProbe();
        sessionSupervisor.Tell(
            new LoginMessage(
                "user1",
                "connection1",
                "password1",
                userSessionActor.Ref.Path.ToString()
            )
        );
        var actorPath = userSessionActor.Ref.Path.ToString();
        sessionSupervisor.Tell(new GetUserSessionMessage(actorPath));

        lobbySupervisorMock.ExpectMsg<ErrorMessage>(msg => msg.Message == "Session not found");
    }

    [Fact]
    public void TestGetUserSessionMessageWithInvalidActorPath()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var accountRelayMock = CreateTestProbe();
        var accountPersistenceMock = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(
            SessionSupervisor.Props(
                lobbySupervisorMock.Ref,
                accountRelayMock.Ref,
                accountPersistenceMock
            )
        );

        var invalidActorPath = "akka://test/user/nonexistent";
        sessionSupervisor.Tell(new GetUserSessionMessage(invalidActorPath));

        ExpectNoMsg();
        lobbySupervisorMock.ExpectMsg<ErrorMessage>();
    }

    [Fact]
    public void TestCreateAccountMessageHandling()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var accountRelayMock = CreateTestProbe();
        var accountPersistenceMock = CreateTestProbe();
        var sessionSupervisor = Sys.ActorOf(
            SessionSupervisor.Props(
                lobbySupervisorMock.Ref,
                accountRelayMock.Ref,
                accountPersistenceMock
            )
        );

        var loginMessage = new LoginMessage("user1", "connection1", "password1", "");
        sessionSupervisor.Tell(loginMessage);

        accountPersistenceMock.ExpectMsg<LoginMessage>(msg => msg.User == "user1");
    }
}
