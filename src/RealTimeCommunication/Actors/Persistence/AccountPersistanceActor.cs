using System.Security.Cryptography;
using System.Text;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Event;
using Asteroids.Shared.Messages;
using Asteroids.Shared.Services;
using RealTimeCommunication.Actors.Session;

namespace RealTimeCommunication;

public class AccountPersistanceActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IUserPersistence _persistenceService;

    public AccountPersistanceActor(IServiceProvider serviceProvider)
    {
        // Make a http client to send requests to gateway
        // Send a request to the gateway
        var s = serviceProvider.CreateScope();
        _persistenceService = s.ServiceProvider.GetRequiredService<IUserPersistence>();
        Receive<StoreAccountInformationMessage>(msg => StoreAccountInformation(msg));
        Receive<LoginMessage>(msg => Login(msg));
        Receive<LoginToConfirmMessage>(ConfirmLogin);
    }

    private void ConfirmLogin(LoginToConfirmMessage msg)
    {
        var user = msg.AccountInformation;
        if (user == null)
        {
            CreateAccount(msg);
            return;
        }

        if (!CompareAccountInformation(user, msg.Password, msg.User))
        {
            _log.Info("User {0} failed comparison", msg.User);
            Sender.Tell(
                new LoginConfirmedMessage(
                    ActorPath: msg.SessionActorPath,
                    ConnectionId: msg.ConnectionId,
                    AccountInformation: new AccountInformation(
                        userName: msg.User,
                        password: msg.Password,
                        userId: new Guid(ActorHelper.GetActorNameFromPath(msg.SessionActorPath)),
                        salt: null
                    ),
                    Status: LoginStatus.InvalidAccount
                )
            );
            return;
        }

        _log.Info("User {0} found", msg.User);
        Sender.Tell(
            new LoginConfirmedMessage(
                ActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                AccountInformation: user,
                Status: LoginStatus.ExistingAccount
            )
        );
    }

    private void CreateAccount(LoginToConfirmMessage msg)
    {
        _log.Info("User {0} not found creating user", msg.User);

        var salt = GenerateSalt();
        var hashedPassword = HashPassword(msg.Password, salt);
        Sender.Tell(
            new LoginConfirmedMessage(
                ActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId,
                AccountInformation: new AccountInformation(
                    userName: msg.User,
                    password: hashedPassword,
                    userId: Guid.Empty,
                    salt: salt
                ),
                Status: LoginStatus.NewAccount
            )
        );
    }

    private async Task Login(LoginMessage msg)
    {
        _log.Info("Logging in {0}", msg.User);

        await GetAccountInformation(msg).PipeTo(Self, Sender);

        async Task<LoginToConfirmMessage> GetAccountInformation(LoginMessage msg)
        {
            var parsable = Guid.TryParse(
                ActorHelper.GetActorNameFromPath(msg.SessionActorPath),
                out var userId
            );
            var aI = await _persistenceService.GetUserInformationAsync(
                parsable ? userId : Guid.Empty
            );

            return new LoginToConfirmMessage(
                AccountInformation: aI,
                User: msg.User,
                Password: msg.Password,
                SessionActorPath: msg.SessionActorPath,
                ConnectionId: msg.ConnectionId
            );
        }
    }

    private async Task StoreAccountInformation(StoreAccountInformationMessage msg)
    {
        _log.Info("Storing account information for {0}", msg.AccountInformation.UserName);
        await _persistenceService.StoreUserInformationAsync(msg.AccountInformation);
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("StorageActor started");
    }

    protected override void PostStop()
    {
        base.PostStop();
        _log.Info("StorageActor stopped");
    }

    public static Props Props()
    {
        var spExtension = DependencyResolver.For(Context.System);
        return spExtension.Props<AccountPersistanceActor>();
    }

    public static Props Props(ActorSystem system)
    {
        var spExtension = DependencyResolver.For(system);
        return spExtension.Props<AccountPersistanceActor>();
    }

    public static Props Props(IServiceProvider serviceProvider)
    {
        return Akka.Actor.Props.Create(() => new AccountPersistanceActor(serviceProvider));
    }

    static string HashPassword(string password, byte[] salt)
    {
        using (var sha256 = new SHA256Managed())
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltedPassword = new byte[passwordBytes.Length + salt.Length];

            // Concatenate password and salt
            Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);

            // Hash the concatenated password and salt
            byte[] hashedBytes = sha256.ComputeHash(saltedPassword);

            // Concatenate the salt and hashed password for storage
            byte[] hashedPasswordWithSalt = new byte[hashedBytes.Length + salt.Length];
            Buffer.BlockCopy(salt, 0, hashedPasswordWithSalt, 0, salt.Length);
            Buffer.BlockCopy(
                hashedBytes,
                0,
                hashedPasswordWithSalt,
                salt.Length,
                hashedBytes.Length
            );

            return Convert.ToBase64String(hashedPasswordWithSalt);
        }
    }

    static byte[] GenerateSalt()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] salt = new byte[16]; // Adjust the size based on your security requirements
            rng.GetBytes(salt);
            return salt;
        }
    }

    private bool CompareAccountInformation(AccountInformation? aI, string password, string userName)
    {
        if (aI == null)
        {
            _log.Info("Account information not found");
            return false;
        }
        var hashedPassword = HashPassword(password, aI.Salt);
        _log.Info("Comparing account information for {0}", hashedPassword, aI.Password);
        return hashedPassword == aI.Password && aI.UserName == userName;
    }
}

public record StoreAccountInformationMessage(AccountInformation AccountInformation);

public record LoginToConfirmMessage(
    AccountInformation? AccountInformation,
    string User,
    string Password,
    string SessionActorPath,
    string ConnectionId
);
