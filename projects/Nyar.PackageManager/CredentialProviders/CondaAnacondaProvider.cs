using System.Text.RegularExpressions;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从 Anaconda nucleus 读取 conda 认证令牌
/// </summary>
public class CondaAnacondaProvider : ICredentialProvider
{
    public string provider_name => "conda (anaconda nucleus)";
    public string vendor_name => "conda";
    public int priority => 10;

    public bool is_available()
    {
        return try_find_token_file() is not null;
    }

    public Task<DiscoveredCredential?> discover()
    {
        var tokenPath = try_find_token_file();

        if (tokenPath is null || !File.Exists(tokenPath)) return Task.FromResult<DiscoveredCredential?>(null);

        try
        {
            var content = File.ReadAllText(tokenPath);

            var match = Regex.Match(content, @"""token""\s*:\s*""([^""]+)""");

            if (!match.Success) match = Regex.Match(content, @"""access_token""\s*:\s*""([^""]+)""");

            if (match.Success)
            {
                string? username = null;
                var userMatch = Regex.Match(content, @"""login""\s*:\s*""([^""]+)""");

                if (userMatch.Success) username = userMatch.Groups[1].Value;

                return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                {
                    token = match.Groups[1].Value,
                    username = username,
                    source = "Anaconda Nucleus 令牌"
                });
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string? try_find_token_file()
    {
        var home = Environment.GetEnvironmentVariable("USERPROFILE")
                   ?? Environment.GetEnvironmentVariable("HOME")
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nucleusDir = Path.Combine(home, ".anaconda", "nucleus");

        if (!Directory.Exists(nucleusDir)) return null;

        foreach (var userDir in Directory.GetDirectories(nucleusDir))
        {
            var tokensFile = Path.Combine(userDir, "tokens.json");

            if (File.Exists(tokensFile)) return tokensFile;
        }

        return null;
    }
}