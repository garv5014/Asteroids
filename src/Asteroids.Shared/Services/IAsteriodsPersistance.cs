using System.Text.Json;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Asteroids.Shared.Services;

public interface IUserPersistence
{
    Task<bool> StoreUserInformationAsync(AccountInformation userInformation);
    Task<AccountInformation> GetUserInformationAsync(Guid userId);
}

public class AccountInformation
{
    public Guid UserId { get; set; }
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

    public Task<AccountInformation> GetUserInformationAsync(Guid userId)
    {
        throw new NotImplementedException();
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
