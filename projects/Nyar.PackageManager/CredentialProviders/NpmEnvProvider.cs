using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从环境变量读取 npm 令牌
/// </summary>
public class NpmEnvProvider : ICredentialProvider
{
    public string provider_name => "npm (环境变量)";
    public string vendor_name => "npm";
    public int priority => 20;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NPM_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NODE_AUTH_TOKEN"));
    }

    public Task<DiscoveredCredential?> discover()
    {
        var token = Environment.GetEnvironmentVariable("NPM_TOKEN")
                    ?? Environment.GetEnvironmentVariable("NODE_AUTH_TOKEN");

        if (string.IsNullOrEmpty(token)) return Task.FromResult<DiscoveredCredential?>(null);

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            token = token,
            source = "NODE_AUTH_TOKEN 环境变量"
        });
    }
}