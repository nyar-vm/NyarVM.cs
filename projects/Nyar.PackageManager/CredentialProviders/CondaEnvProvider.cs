using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从环境变量读取 conda 令牌
/// </summary>
public class CondaEnvProvider : ICredentialProvider
{
    public string provider_name => "conda (环境变量)";
    public string vendor_name => "conda";
    public int priority => 20;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANACONDA_API_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONDA_TOKEN"));
    }

    public Task<DiscoveredCredential?> discover()
    {
        var token = Environment.GetEnvironmentVariable("ANACONDA_API_TOKEN")
                    ?? Environment.GetEnvironmentVariable("CONDA_TOKEN");

        if (string.IsNullOrEmpty(token)) return Task.FromResult<DiscoveredCredential?>(null);

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            token = token,
            source = "ANACONDA_API_TOKEN 环境变量"
        });
    }
}