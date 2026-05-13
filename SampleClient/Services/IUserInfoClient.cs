using SampleClient.Models;

namespace SampleClient.Services;

public interface IUserInfoClient
{
    Task<UserInfoResultViewModel> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken);
}
