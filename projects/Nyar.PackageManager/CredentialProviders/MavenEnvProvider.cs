using System.Text;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从环境变量读取 Maven 凭据
/// </summary>
public class MavenEnvProvider : ICredentialProvider
{
    public string provider_name => "maven (环境变量)";
    public string vendor_name => "maven";
    public int priority => 20;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MAVEN_TOKEN"))
               || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONATYPE_USERNAME"))
                   && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SONATYPE_PASSWORD")));
    }

    public Task<DiscoveredCredential?> discover()
    {
        var token = Environment.GetEnvironmentVariable("MAVEN_TOKEN");

        if (!string.IsNullOrEmpty(token))
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                token = token,
                source = "MAVEN_TOKEN 环境变量"
            });

        var username = Environment.GetEnvironmentVariable("SONATYPE_USERNAME");
        var password = Environment.GetEnvironmentVariable("SONATYPE_PASSWORD");

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                token = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{username}:{password}")),
                username = username,
                source = "SONATYPE_USERNAME/SONATYPE_PASSWORD 环境变量"
            });

        return Task.FromResult<DiscoveredCredential?>(null);
    }
}