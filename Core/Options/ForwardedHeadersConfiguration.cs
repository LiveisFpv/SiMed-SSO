using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Core.Options;

public static class ForwardedHeadersConfiguration
{
    public static void Configure(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var proxy in Split(configuration["TRUSTED_PROXIES"]))
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        foreach (var network in Split(configuration["TRUSTED_NETWORKS"]))
        {
            if (System.Net.IPNetwork.TryParse(network, out var ipNetwork))
            {
                options.KnownIPNetworks.Add(ipNetwork);
            }
        }
    }

    private static IEnumerable<string> Split(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
