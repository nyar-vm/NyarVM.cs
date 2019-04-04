using System.Text;
using System.Xml.Linq;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从 ~/.m2/settings.xml 读取 Maven 认证凭据
/// </summary>
public class MavenSettingsProvider : ICredentialProvider
{
    public string provider_name => "maven (settings.xml)";
    public string vendor_name => "maven";
    public int priority => 10;

    public bool is_available()
    {
        var settingsPath = get_maven_settings_path();

        if (!File.Exists(settingsPath)) return false;

        try
        {
            var content = File.ReadAllText(settingsPath);
            return content.Contains("<server>") && content.Contains("ossrh");
        }
        catch
        {
            return false;
        }
    }

    public Task<DiscoveredCredential?> discover()
    {
        var settingsPath = get_maven_settings_path();

        if (!File.Exists(settingsPath)) return Task.FromResult<DiscoveredCredential?>(null);

        try
        {
            var doc = XDocument.Load(settingsPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var servers = doc.Descendants(ns + "server")
                .Concat(doc.Descendants("server"));

            foreach (var server in servers)
            {
                var id = server.Element(ns + "id")?.Value
                         ?? server.Element("id")?.Value;

                if (id is null || (!id.Contains("ossrh", StringComparison.OrdinalIgnoreCase)
                                   && !id.Contains("central", StringComparison.OrdinalIgnoreCase)
                                   && !id.Contains("sonatype", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var username = server.Element(ns + "username")?.Value
                               ?? server.Element("username")?.Value;
                var password = server.Element(ns + "password")?.Value
                               ?? server.Element("password")?.Value;

                string? token = null;

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    token = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{username}:{password}"));
                }
                else
                {
                    var privateKey = server.Element(ns + "privateKey")?.Value
                                     ?? server.Element("privateKey")?.Value;

                    if (!string.IsNullOrEmpty(privateKey)) token = privateKey;
                }

                if (!string.IsNullOrEmpty(token))
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        token = token,
                        username = username,
                        source = $"~/.m2/settings.xml [server id={id}]"
                    });
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string get_maven_settings_path()
    {
        var home = Environment.GetEnvironmentVariable("USERPROFILE")
                   ?? Environment.GetEnvironmentVariable("HOME")
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".m2", "settings.xml");
    }
}