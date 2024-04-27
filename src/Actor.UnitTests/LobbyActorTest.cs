using System;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.TestKit.Xunit2;
using Asteroids.Shared;
using Asteroids.Shared.GameEntities;
using Asteroids.Shared.Messages;
using RealTimeCommunication;
using Xunit;

namespace Actor.UnitTests;

public class LobbyActorTests : TestKit
{
    [Fact]
    public void Test_JoinLobby()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var joinLobbyMessage = new JoinLobbyMessage("sessionPath", lobbyName, "");
        lobbyActor.Tell(joinLobbyMessage);
        ExpectNoMsg();
    }

    [Fact]
    public void Test_JoinLobby_1()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var joinLobbyMessage = new JoinLobbyMessage("sessionPath", lobbyName, "");
        lobbyActor.Tell(joinLobbyMessage);
        ExpectNoMsg();
        var getLobbiesMessage = new GetLobbiesMessage("sessionPath", "");
        lobbyActor.Tell(getLobbiesMessage);
        ExpectMsg(new GameLobby("existingLobby", 1, LobbyStatus.WaitingForPlayers));
    }

    [Fact]
    public void Test_JoinLobby_2()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var joinLobbyMessage = new JoinLobbyMessage("sessionPath", lobbyName, "");
        lobbyActor.Tell(joinLobbyMessage);
        lobbyActor.Tell(new JoinLobbyMessage("sessionPaths", lobbyName, ""));
        ExpectNoMsg();
        var getLobbiesMessage = new GetLobbiesMessage("sessionPath", "");
        lobbyActor.Tell(getLobbiesMessage);
        ExpectMsg(new GameLobby("existingLobby", 1, LobbyStatus.WaitingForPlayers));
    }

    [Fact]
    public void Test_JoinLobby_Same_Session()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var joinLobbyMessage = new JoinLobbyMessage("sessionPath", lobbyName, "");
        lobbyActor.Tell(joinLobbyMessage);
        lobbyActor.Tell(joinLobbyMessage);
        lobbyActor.Tell(joinLobbyMessage);
        lobbyActor.Tell(joinLobbyMessage);
        lobbyActor.Tell(joinLobbyMessage);
        ExpectNoMsg();
        var getLobbiesMessage = new GetLobbiesMessage("sessionPath", "");
        lobbyActor.Tell(getLobbiesMessage);
        ExpectMsg(new GameLobby("existingLobby", 1, LobbyStatus.WaitingForPlayers));
    }

    [Fact]
    public void Test_GetLobbies()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var getLobbiesMessage = new GetLobbiesMessage("sessionPath", "");
        lobbyActor.Tell(getLobbiesMessage);
        ExpectMsg(new GameLobby("existingLobby", 0, LobbyStatus.WaitingForPlayers));
    }

    [Fact]
    public void Test_GetLobbyState()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var getLobbyStateMessage = new GetLobbyStateMessage(lobbyName, "", "");
        lobbyActor.Tell(getLobbyStateMessage);
        ExpectNoMsg();
    }

    [Fact]
    public void Test_GetLobbyState_To_Parent()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var Parent = CreateTestProbe();

        var lobbyActor = Parent.ChildActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var getLobbyStateMessage = new GetLobbyStateMessage(lobbyName, "", "");
        lobbyActor.Tell(getLobbyStateMessage);
        ExpectNoMsg();
        Parent.ExpectMsg<LobbyStateResponse>();
    }

    [Fact]
    public void Test_UpdateLobby()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var updateLobbyMessage = new UpdateLobbyMessage("", "", "", LobbyStatus.InGame);
        lobbyActor.Tell(updateLobbyMessage);
        ExpectNoMsg();
    }

    [Fact]
    public void Test_UpdateLobby_SendToParent()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";

        var Parent = CreateTestProbe();
        var lobbyActor = Parent.ChildActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));

        var updateLobbyMessage = new UpdateLobbyMessage("", "", "", LobbyStatus.InGame);
        lobbyActor.Tell(updateLobbyMessage);
        ExpectNoMsg();
        Parent.ExpectMsg<LobbyStateResponse>();
    }

    [Fact]
    public void Test_UpdateLobby_To_Parent()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var Parent = CreateTestProbe();

        var lobbyActor = Parent.ChildActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var updateLobbyMessage = new UpdateLobbyMessage("", "", "", LobbyStatus.InGame);
        lobbyActor.Tell(updateLobbyMessage);
        ExpectNoMsg();
        Parent.ExpectMsg<LobbyStateResponse>();
    }

    [Fact]
    public void Test_UpdateShip()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var updateShipMessage = new UpdateShipMessage(
            "",
            "",
            new UpdateShipParams(true, true, true, true),
            ""
        );
        lobbyActor.Tell(updateShipMessage);
        ExpectNoMsg();
    }

    [Fact]
    public void Test_GameLoop()
    {
        var lobbyName = "existingLobby";
        var owner = "owner";
        var lobbyActor = Sys.ActorOf(Props.Create(() => new LobbyActor(lobbyName, owner)));
        var gameLoopMessage = new GameLoopMessage();
        lobbyActor.Tell(gameLoopMessage);
        ExpectNoMsg();
    }
}
