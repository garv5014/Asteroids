using System.Text;

namespace RealTimeCommunication;

public static class ActorHelper
{
    public static string GetActorNameFromPath(string path)
    {
        var actorName = path.Split('/').Last();
        return actorName;
    }

    public static string SanitizeActorName(string input) //chat gpt
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty; // Default name for completely empty or whitespace names
        }

        var allowedSpecialCharacters = new HashSet<char>("-_.*$+:@&=,!~';()");
        var sanitized = new StringBuilder();

        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || allowedSpecialCharacters.Contains(c))
            {
                sanitized.Append(c);
            }
            else
            {
                sanitized.Append('_'); // Replace illegal characters with underscore
            }
        }

        var sanitizedResult = sanitized.ToString();

        // Ensure the name does not start with '$'
        if (sanitizedResult.StartsWith("$"))
        {
            sanitizedResult = "_" + sanitizedResult.Substring(1);
        }

        return sanitizedResult;
    }

    public static string ProjectName = "Asteroids";

    public static string LobbySupervisorName = "lobbySupervisor";

    public static string SessionSupervisorName = "sessionSupervisor";

    public static string AccountRelayActorName = "accountRelayActor";

    public static string LobbyRelayActorName = "lobbyRelayActor";
    
    public static string ErrorHubRelayActorName = "errorHubRelayActor";
    
    public static string AccountPersistanceActorName = "accountPersistanceActor";

    public static string LobbyPersistanceActorName = "lobbyPersistanceActor";
}
