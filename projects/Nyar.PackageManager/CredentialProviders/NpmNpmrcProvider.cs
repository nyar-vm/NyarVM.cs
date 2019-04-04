using System.Text;
using System.Text.RegularExpressions;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

#region npm 凭据提供器

/// <summary>
///     从 ~/.npmrc 读取 npm 认证令牌
/// </summary>
public class NpmNpmrcProvider : ICredentialProvider
{
    public string provider_name => "npm-cli (.npmrc)";
    public string vendor_name => "npm";
    public int priority => 10;

    public bool is_available()
    {
        return File.Exists(get_global_npmrc_path());
    }

    public Task<DiscoveredCredential?> discover()
    {
        var npmrcPath = get_global_npmrc_path();

        if (!File.Exists(npmrcPath)) return Task.FromResult<DiscoveredCredential?>(null);

        var content = File.ReadAllText(npmrcPath);
        var token = parse_npmrc_token(content, "//registry.npmjs.org/");

        if (token is null)
        {
            var configuredRegistry = parse_npmrc_registry(content);

            if (!string.IsNullOrEmpty(configuredRegistry)) token = parse_npmrc_token(content, configuredRegistry);
        }

        if (token is null) return Task.FromResult<DiscoveredCredential?>(null);

        return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
        {
            token = token,
            source = "~/.npmrc (用户级)"
        });
    }

    private static string get_global_npmrc_path()
    {
        var home = Environment.GetEnvironmentVariable("USERPROFILE")
                   ?? Environment.GetEnvironmentVariable("HOME")
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".npmrc");
    }

    private static string? parse_npmrc_token(string content, string registryPrefix)
    {
        var escapedPrefix = Regex.Escape(registryPrefix);
        var match = Regex.Match(content,
            $@"^{escapedPrefix}:_authToken\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success) return match.Groups[1].Value.Trim();

        match = Regex.Match(content,
            $@"^{escapedPrefix}:_auth\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success)
            try
            {
                var bytes = Convert.FromBase64String(match.Groups[1].Value.Trim());
                var auth = Encoding.UTF8.GetString(bytes);
                var colonIndex = auth.IndexOf(':');

                if (colonIndex >= 0) return auth[(colonIndex + 1)..];
            }
            catch
            {
            }

        return null;
    }

    private static string? parse_npmrc_registry(string content)
    {
        var match = Regex.Match(content,
            @"^registry\s*=\s*(.+)$", RegexOptions.Multiline);

        if (match.Success)
        {
            var registry = match.Groups[1].Value.Trim();

            if (!registry.EndsWith("/")) registry += "/";

            return registry;
        }

        return null;
    }
}

#endregion