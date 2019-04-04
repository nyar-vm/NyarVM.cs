using System.Xml.Linq;
using Nyar.PackageManager.Auth;

namespace Nyar.PackageManager.CredentialProviders;

/// <summary>
///     从 NuGet.Config 读取 NuGet API 密钥
/// </summary>
public class NuGetConfigProvider : ICredentialProvider
{
    public string provider_name => "nuget (NuGet.Config)";
    public string vendor_name => "nuget";
    public int priority => 10;

    public bool is_available()
    {
        var configPath = get_nu_get_config_path();

        if (!File.Exists(configPath)) return false;

        try
        {
            var content = File.ReadAllText(configPath);
            return content.Contains("packageSourceCredentials") || content.Contains("apikeys");
        }
        catch
        {
            return false;
        }
    }

    public Task<DiscoveredCredential?> discover()
    {
        var configPath = get_nu_get_config_path();

        if (!File.Exists(configPath)) return Task.FromResult<DiscoveredCredential?>(null);

        try
        {
            var doc = XDocument.Load(configPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var credentials = doc.Descendants(ns + "packageSourceCredentials")
                .Concat(doc.Descendants("packageSourceCredentials"));

            foreach (var credential in credentials)
            foreach (var source in credential.Elements())
            {
                var username = source.Element(ns + "add")?.Attribute("key")
                    ?.Value == "Username"
                    ? source.Element(ns + "add")?.Attribute("value")?.Value
                    : source.Element("add")?.Attribute("key")?.Value == "Username"
                        ? source.Element("add")?.Attribute("value")?.Value
                        : null;

                string? password = null;

                foreach (var add in source.Elements(ns + "add").Concat(source.Elements("add")))
                {
                    var key = add.Attribute("key")?.Value;

                    if (key is "ClearTextPassword" or "Password")
                    {
                        password = add.Attribute("value")?.Value;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(password))
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        token = password,
                        username = username,
                        source = $"NuGet.Config [{source.Name.LocalName}]"
                    });
            }

            var apiKeys = doc.Descendants(ns + "apikeys")
                .Concat(doc.Descendants("apikeys"));

            foreach (var apiKey in apiKeys)
            foreach (var add in apiKey.Elements(ns + "add").Concat(apiKey.Elements("add")))
            {
                var value = add.Attribute("value")?.Value;

                if (!string.IsNullOrEmpty(value))
                    return Task.FromResult<DiscoveredCredential?>(new DiscoveredCredential
                    {
                        token = value,
                        source = $"NuGet.Config [apikeys/{add.Attribute("key")?.Value}]"
                    });
            }
        }
        catch
        {
        }

        return Task.FromResult<DiscoveredCredential?>(null);
    }

    private static string get_nu_get_config_path()
    {
        var appData = Environment.GetEnvironmentVariable("APPDATA")
                      ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "NuGet", "NuGet.Config");
    }
}