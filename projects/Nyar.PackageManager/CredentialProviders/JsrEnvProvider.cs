using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从环境变量读取 jsr 令牌
/// </summary>
public class JsrEnvProvider : ICredentialProvider
{
    public string provider_name => "jsr (环境变量)";
    public string vendor_name => "jsr";
    public int priority => 20;

    public bool is_available()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JSR_TOKEN"))
               || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DENO_AUTH_TOKENS"));
    }

    public Task<DiscoveredCredential?> discover()
    {
        var token = Environment.GetEnvironmentVariable("JSR_TOKEN");

        if (!string.IsNullOrEmpty(token))
            return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
            {
                token = token,
                source = "JSR_TOKEN 环境变量"
            });

        var denoAuthTokens = Environment.GetEnvironmentVariable("DENO_AUTH_TOKENS");

        if (!string.IsNullOrEmpty(denoAuthTokens))
        {
            var parts = denoAuthTokens.Split(';');

            foreach (var part in parts)
                if (part.StartsWith("jsr@"))
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        token = part[4..],
                        source = "DENO_AUTH_TOKENS 环境变量"
                    });
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }
}