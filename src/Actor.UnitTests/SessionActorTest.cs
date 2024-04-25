using Akka.Actor;
using Akka.TestKit.Xunit2;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Moq;
using RealTimeCommunication.Actors.Session;
using Xunit;

namespace Actor.UnitTests;

public class SessionActorTests : TestKit
{
    [Fact]
    public void TestHandleCreateLobbyMessage()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));
        var createLobbyMessage = new CreateLobbyMessage("", "connection123", "");

        sessionActor.Tell(createLobbyMessage);

        lobbySupervisorMock.ExpectMsg<CreateLobbyMessage>();
    }

    [Fact]
    public void TestHandleRefreshConnectionId()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));
        var refreshConnectionId = new RefreshConnectionId("newConnection123");

        sessionActor.Tell(refreshConnectionId);

        sessionActor.Tell(new GetLobbyStateMessage("", "", ""));
        lobbySupervisorMock.ExpectMsg<GetLobbyStateMessage>();
    }

    [Fact]
    public void TestPostStopBehavior()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));

        Watch(sessionActor);
        Sys.Stop(sessionActor);
        ExpectTerminated(sessionActor);
    }

    [Fact]
    public void test_join_lobby()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));
        var joinLobbyMessage = new JoinLobbyMessage(
            LobbyName: "lobby1",
            SessionActorPath: "",
            ConnectionId: ""
        );

        sessionActor.Tell(joinLobbyMessage);

        lobbySupervisorMock.ExpectMsg<JoinLobbyMessage>();
    }

    [Fact]
    public void test_get_lobby()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));
        var joinLobbyMessage = new GetLobbiesMessage(SessionActorPath: "", ConnectionId: "");

        sessionActor.Tell(joinLobbyMessage);

        lobbySupervisorMock.ExpectMsg<GetLobbiesMessage>();
    }

    [Fact]
    public void Test_PassTo_Lobby()
    {
        var lobbySupervisorMock = CreateTestProbe();
        var sessionActor = Sys.ActorOf(SessionActor.Props("user1", lobbySupervisorMock.Ref));
        var joinLobbyMessage = new LobbyStateResponse(
            SessionActorPath: "",
            ConnectionId: "",
            CurrentState: new GameSnapShot(
                isOwner: true,
                playerCount: 1,
                currentStatus: LobbyStatus.InGame,
                ships: new List<Ship>(),
                asteroids: new List<Asteroid>(),
                projectiles: new List<Projectile>(),
                boardWidth: 100,
                boardHeight: 100
            )
        );

        sessionActor.Tell(joinLobbyMessage);

        lobbySupervisorMock.ExpectMsg<LobbyStateResponse>();
    }
}
