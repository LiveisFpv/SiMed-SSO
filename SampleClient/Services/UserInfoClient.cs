using System.Net.Http.Headers;
using SampleClient.Models;
using SampleClient.Options;

namespace SampleClient.Services;

public sealed class UserInfoClient : IUserInfoClient
{
    private readonly HttpClient _httpClient;
    private readonly SampleClientOidcOptions _options;

    public UserInfoClient(HttpClient httpClient, SampleClientOidcOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<UserInfoResultViewModel> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > 4000)
        {
            body = body[..4000] + "...";
        }

        return new UserInfoResultViewModel(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            body);
    }
}
