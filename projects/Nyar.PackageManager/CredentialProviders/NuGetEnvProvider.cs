using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从环境变量读取 NuGet API 密钥
/// </summary>
public class NuGetEnvProvider : ICredentialProvider
{
    public string provider_name => "nuget (环境变量)";
    public string vendor_name => "nuget";
    public int priority => 20;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NUGET_API_KEY"));
    }

    public Task<DiscoveredCredential?> discover()
    {
        var token = Environment.GetEnvironmentVariable("NUGET_API_KEY");

        if (string.IsNullOrEmpty(token)) return Task.FromResult<DiscoveredCredential?>(null);

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            token = token,
            source = "NUGET_API_KEY 环境变量"
        });
    }
}