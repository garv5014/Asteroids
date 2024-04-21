using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Observability;

public static class DiagnosticsConfig
{
    public static string LobbyService = "Asteroids.Actors";
    public static Meter LobbyMeter = new(LobbyService);
    public static Counter<int> TotalLobbies = LobbyMeter.CreateCounter<int>("total.lobbies");
    public static Counter<int> PlayingLobbies = LobbyMeter.CreateCounter<int>("total.playing.lobbies");
    public static ActivitySource Source = new(LobbyService);
}
