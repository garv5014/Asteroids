namespace Asteroids.Shared.Messages;

public class LoginMessage : HubMessage
{
    public string User { get; set; }
    public string Password { get; set; }
}

public class LoginResponseMessage : HubMessage
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class CreateAccountMessage : HubMessage
{
    public string User { get; set; }
    public string Password { get; set; }
}

public class CreateAccountResponseMessage : HubMessage
{
    public bool Success { get; set; }
    public string Message { get; set; }
}
