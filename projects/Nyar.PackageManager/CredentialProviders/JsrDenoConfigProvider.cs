using System.Text.RegularExpressions;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从 Deno CLI 配置文件读取 jsr 认证令牌
/// </summary>
public class JsrDenoConfigProvider : ICredentialProvider
{
    public string provider_name => "jsr (deno config)";
    public string vendor_name => "jsr";
    public int priority => 10;

    public bool is_available()
    {
        var configPath = get_deno_config_path();

        if (!File.Exists(configPath)) return false;

        var content = File.ReadAllText(configPath);
        return content.Contains("jsr.io");
    }

    public Task<DiscoveredCredential?> discover()
    {
        var configPath = get_deno_config_path();

        if (!File.Exists(configPath)) return Task.FromResult<DiscoveredCredential?>(null);

        try
        {
            var content = File.ReadAllText(configPath);

            var match = Regex.Match(content,
                @"""net\.jsr""\s*:\s*""([^""]+)""");

            if (!match.Success)
                match = Regex.Match(content,
                    @"""https://jsr\.io""\s*:\s*""([^""]+)""");

            if (match.Success)
                return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                {
                    token = match.Groups[1].Value,
                    source = "Deno 配置文件"
                });
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string get_deno_config_path()
    {
        var appData = Environment.GetEnvironmentVariable("APPDATA")
                      ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "deno", "config.json");
    }
}