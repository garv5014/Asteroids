using System.Text.Json;
using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Asteroids.Shared.Services;

public interface IAsteroidPersistence
{
    Task<bool> StoreUserInformationAsync(UserInformation userInformation);
}

public class UserInformation
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

// Make AsteriodsPersistanceService that takes in a IGatewayClient and implements IAsteriodsPersistance

public class AsteroidsPersistanceService : IAsteroidPersistence
{
    private readonly IGatewayClient gatewayService;

    public AsteroidsPersistanceService(IGatewayClient gatewayService)
    {
        this.gatewayService = gatewayService;
    }

    public async Task<bool> StoreUserInformationAsync(UserInformation userInformation)
    {
        var userInformationString = JsonHelper.Serialize(userInformation);

        var stored = await gatewayService.StrongGet(userInformation.UserId);

        if (stored == null)
        {
            var response = await gatewayService.CompareAndSwap(
                new CompareAndSwapRequest
                {
                    Key = userInformation.UserId,
                    NewValue = userInformationString,
                    OldValue = null
                }
            );

            return response.IsSuccessStatusCode;
        }
        return false;
    }
}
