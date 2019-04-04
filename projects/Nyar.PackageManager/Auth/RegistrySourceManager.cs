using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Nyar.PackageRegistry;
using Nyar.PackageRegistry.Conda;
using Nyar.PackageRegistry.Jsr;
using Nyar.PackageRegistry.Maven;
using Nyar.PackageRegistry.Npm;
using Nyar.PackageRegistry.Nuget;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Auth;

/// <summary>
///     娉ㄥ唽琛ㄦ簮绠＄悊鍣紝鎸佷箙鍖栫鐞嗘敞鍐岃〃绔偣閰嶇疆
/// </summary>
public class RegistrySourceManager
{
    private static readonly Dictionary<string, string> _s_default_sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["npm"] = "https://registry.npmjs.org",
        ["jsr"] = "https://jsr.io",
        ["conda"] = "https://api.anaconda.org",
        ["maven"] = "https://search.maven.org",
        ["nuget"] = "https://api.nuget.org/v3"
    };

    private readonly SerdeParser _parse;

    private readonly Dictionary<string, string> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _sources_file_path;

    /// <summary>
    ///     鍒涘缓娉ㄥ唽琛ㄦ簮绠＄悊鍣?    ///
    /// </summary>
    /// <param name="configDirectory">
    ///     閰嶇疆鐩綍璺緞锛堥€氬父涓?~/.valkyrie/锛?/param>
    ///     <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public RegistrySourceManager(string configDirectory, SerdeParser parse)
    {
        _sources_file_path = Path.Combine(configDirectory, "registry-sources.von");
        _parse = parse;
        ensure_default_sources();
    }

    /// <summary>
    ///     鑾峰彇鎵€鏈夊凡閰嶇疆鐨勬敞鍐岃〃婧?    ///
    /// </summary>
    public IReadOnlyDictionary<string, string> sources => _sources;

    public void remove_source(string registryName)
    {
        _sources.Remove(registryName.ToLowerInvariant());
    }

    /// <summary>
    ///     鑾峰彇鎸囧畾娉ㄥ唽琛ㄧ殑绔偣
    /// </summary>
    public string? get_endpoint(string registryName)
    {
        return _sources.GetValueOrDefault(registryName.ToLowerInvariant());
    }

    /// <summary>
    ///     璁剧疆娉ㄥ唽琛ㄧ鐐?    ///
    /// </summary>
    public void set_endpoint(string registryName, string endpoint)
    {
        _sources[registryName.ToLowerInvariant()] = endpoint;
    }

    /// <summary>
    ///     绉婚櫎娉ㄥ唽琛ㄦ簮閰嶇疆
    /// </summary>
    /// <param name="registryName">
    ///     娉ㄥ唽琛ㄥ悕绉?/param>
    ///     <returns>鏄惁鎴愬姛绉婚櫎</returns>
    public bool remove_endpoint(string registryName)
    {
        return _sources.Remove(registryName.ToLowerInvariant());
    }

    /// <summary>
    ///     浠庢枃浠跺姞杞芥敞鍐岃〃婧愰厤缃?    ///
    /// </summary>
    public void load()
    {
        if (!File.Exists(_sources_file_path))
        {
            ensure_default_sources();
            return;
        }

        try
        {
            var content = File.ReadAllText(_sources_file_path);

            if (string.IsNullOrWhiteSpace(content))
            {
                ensure_default_sources();
                return;
            }

            _sources.Clear();

            var value = _parse(content);

            if (ValkyrieValueProjector.as_utf8_map(
                    ValkyrieValueProjector.bind_literal(value, ValkyrieValueProjector.utf8_map_type)) is { } values)
                foreach (var field in values)
                    _sources[field.Key.ToLowerInvariant()] = field.Value;
        }
        catch
        {
            ensure_default_sources();
        }
    }

    /// <summary>
    ///     淇濆瓨娉ㄥ唽琛ㄦ簮閰嶇疆鍒版枃浠?    ///
    /// </summary>
    public async Task save()
    {
        var dir = Path.GetDirectoryName(_sources_file_path);

        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var entries = _sources.Select(kv => $"{kv.Key}: \"{kv.Value}\"").ToList();
        var content = "{\n" + string.Join(",\n", entries.Select(e => "    " + e)) + "\n}\n";

        await File.WriteAllTextAsync(_sources_file_path, content);
    }

    /// <summary>
    ///     鍒涘缓瀵瑰簲娉ㄥ唽琛ㄧ殑瀹炰緥
    /// </summary>
    /// <param name="registryName">
    ///     娉ㄥ唽琛ㄥ悕绉?/param>
    ///     <returns>娉ㄥ唽琛ㄥ疄渚?/returns>
    public IRegistry create_registry(string registryName)
    {
        var endpoint = get_endpoint(registryName)
                       ?? get_default_endpoint(registryName);

        return registryName.ToLowerInvariant() switch
        {
            "npm" => new NpmRegistry { endpoint = endpoint },
            "jsr" => new JsrRegistry { endpoint = endpoint },
            "conda" => new CondaRegistry { endpoint = endpoint },
            "maven" => new MavenRegistry { endpoint = endpoint },
            "nuget" => new NuGetRegistry { endpoint = endpoint },
            _ => throw new ArgumentException($"涓嶆敮鎸佺殑娉ㄥ唽琛ㄧ被鍨? {registryName}")
        };
    }

    private void ensure_default_sources()
    {
        foreach (var (name, endpoint) in _s_default_sources) _sources.TryAdd(name, endpoint);
    }

    private static string get_default_endpoint(string registryName)
    {
        return _s_default_sources.TryGetValue(registryName.ToLowerInvariant(), out var endpoint)
            ? endpoint
            : throw new ArgumentException($"鏈煡鐨勬敞鍐岃〃绫诲瀷: {registryName}");
    }
}
