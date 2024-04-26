using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Asteroids.Shared.Services;

public interface IUserPersistence
{
    Task<bool> StoreUserInformationAsync(AccountInformation userInformation);
    Task<AccountInformation?> GetUserInformationAsync(Guid userId);
}

public class AccountInformation
{
    public AccountInformation(string userName, string password, Guid userId, byte[]? salt)
    {
        UserName = userName;
        Password = password;
        UserId = userId;
        Salt = salt;
    }

    public Guid UserId { get; set; }
    public byte[] Salt { get; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

// Make AsteriodsPersistanceService that takes in a IGatewayClient and implements IAsteriodsPersistance

public class UserPersistanceService : IUserPersistence
{
    private readonly IGatewayClient gatewayService;

    public UserPersistanceService(IGatewayClient gatewayService)
    {
        this.gatewayService = gatewayService;
    }

    public async Task<AccountInformation?> GetUserInformationAsync(Guid userId)
    {
        var stored = await gatewayService.StrongGet(userId.ToString());

        return stored == null ? null : JsonHelper.Deserialize<AccountInformation>(stored.Value);
    }

    public async Task<bool> StoreUserInformationAsync(AccountInformation userInformation)
    {
        var userInformationString = JsonHelper.Serialize(userInformation);
        var userId = userInformation.UserId.ToString();
        var stored = await gatewayService.StrongGet(userId);

        if (stored == null)
        {
            var response = await gatewayService.CompareAndSwap(
                new CompareAndSwapRequest
                {
                    Key = userId,
                    NewValue = userInformationString,
                    OldValue = null
                }
            );

            return response.IsSuccessStatusCode;
        }
        return false;
    }
}
