using System.Text;
using Core.Data;
using Nyar.PackageManager.Tools;
using Std.Config;
using Std.Config.Node;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Config;

[Data]
public class LegionConfig
{
    private readonly string _global_config_path;
    private readonly SerdeParser _parse;
    private readonly string? _project_config_path;

    /// <summary>
    ///     创建 LegionConfig 实例
    /// </summary>
    /// <param name="parse">配置文本解析器</param>
    public LegionConfig(SerdeParser parse)
    {
        _parse = parse;
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var globalDir = Path.Combine(userHome, ".valkyrie");

        if (!Directory.Exists(globalDir)) Directory.CreateDirectory(globalDir);

        _global_config_path = Path.Combine(globalDir, "config.von");
        _project_config_path = find_project_config();
    }

    /// <summary>
    ///     榛樿娉ㄥ唽琛ㄥ悕绉?    ///
    /// </summary>
    public string registry { get; set; } = "npm";

    /// <summary>
    ///     HTTP 浠ｇ悊鍦板潃
    /// </summary>
    public string? proxy { get; set; }

    /// <summary>
    ///     浠ｇ悊璁よ瘉鐢ㄦ埛鍚?    ///
    /// </summary>
    [Key("proxyUsername")]
    public string? proxy_username { get; set; }

    /// <summary>
    ///     浠ｇ悊璁よ瘉瀵嗙爜
    /// </summary>
    [Key("proxyPassword")]
    public string? proxy_password { get; set; }

    /// <summary>
    ///     鏄惁鍚敤绂荤嚎妯″紡锛堜粎浣跨敤缂撳瓨鍜屾湰鍦版枃浠讹級
    /// </summary>
    [Key("offline")]
    public bool offline_mode { get; set; }

    /// <summary>
    ///     缃戠粶璇锋眰瓒呮椂绉掓暟
    /// </summary>
    [Key("timeout")]
    public int timeout_seconds { get; set; } = 30;

    /// <summary>
    ///     涓嬭浇澶辫触鏈€澶ч噸璇曟鏁?    ///
    /// </summary>
    [Key("maxRetries")]
    public int max_retries { get; set; } = 3;

    /// <summary>
    ///     鏄惁楠岃瘉鍖呭畬鏁存€у搱甯?    ///
    /// </summary>
    [Key("verifyIntegrity")]
    public bool verify_integrity { get; set; } = true;

    /// <summary>
    ///     鏄惁涓ユ牸楠岃瘉 SSL 璇佷功
    /// </summary>
    [Key("strictSsl")]
    public bool strict_ssl { get; set; } = true;

    /// <summary>
    ///     娉ㄥ唽琛ㄧ鐐瑰湴鍧€鏄犲皠锛堝悕绉?鈫?URL锛?    ///
    /// </summary>
    [Key("registryEndpoints")]
    public Dictionary<string, string> registry_endpoints { get; set; } = new();

    /// <summary>
    ///     娉ㄥ唽琛ㄩ暅鍍忔簮鏄犲皠锛堝師濮嬪煙鍚?鈫?闀滃儚鍩熷悕锛?    ///
    /// </summary>
    [Key("registryMirrors")]
    public Dictionary<string, string> registry_mirrors { get; set; } = new();

    /// <summary>
    ///     鑷畾涔夌幆澧冨彉閲?    ///
    /// </summary>
    [Key("environment")]
    public Dictionary<string, string> environment_variables { get; set; } = new();

    /// <summary>
    ///     鍏ㄥ眬缂撳瓨鐩綍璺緞锛坣ull 琛ㄧず浣跨敤榛樿璺緞锛?    ///
    /// </summary>
    [Key("cacheDirectory")]
    public string? cache_directory { get; set; }

    /// <summary>
    ///     鏈€澶у苟琛屼笅杞芥暟
    /// </summary>
    [Key("maxParallelDownloads")]
    public int max_parallel_downloads { get; set; } = 8;

    public void load()
    {
        load_global_config();

        if (_project_config_path is not null) load_project_config(_project_config_path);
    }

    public void save_global()
    {
        save_config(_global_config_path);
    }

    public void save_project(string projectDirectory)
    {
        var projectConfigPath = Path.Combine(projectDirectory, "valkyrie.von");
        save_config(projectConfigPath);
    }

    public string? get(string key)
    {
        return key switch
        {
            "registry" => registry,
            "proxy" => proxy,
            "proxyUsername" => proxy_username,
            "proxyPassword" => proxy_password,
            "offline" => offline_mode.ToString().ToLower(),
            "timeout" => timeout_seconds.ToString(),
            "maxRetries" => max_retries.ToString(),
            "verifyIntegrity" => verify_integrity.ToString().ToLower(),
            "strictSsl" => strict_ssl.ToString().ToLower(),
            "cacheDirectory" => cache_directory,
            "maxParallelDownloads" => max_parallel_downloads.ToString(),
            _ => environment_variables.GetValueOrDefault(key)
        };
    }

    public void set(string key, string value)
    {
        switch (key)
        {
            case "registry":
                registry = value;
                break;
            case "proxy":
                proxy = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "proxyUsername":
                proxy_username = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "proxyPassword":
                proxy_password = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "offline":
                offline_mode = bool.TryParse(value, out var offline) && offline;
                break;
            case "timeout":
                timeout_seconds = int.TryParse(value, out var timeout) ? timeout : 30;
                break;
            case "maxRetries":
                max_retries = int.TryParse(value, out var retries) ? retries : 3;
                break;
            case "verifyIntegrity":
                verify_integrity = !bool.TryParse(value, out var verify) || verify;
                break;
            case "strictSsl":
                strict_ssl = !bool.TryParse(value, out var ssl) || ssl;
                break;
            case "cacheDirectory":
                cache_directory = string.IsNullOrEmpty(value) ? null : value;
                break;
            case "maxParallelDownloads":
                max_parallel_downloads = int.TryParse(value, out var parallel) ? parallel : 8;
                break;
            default:
                environment_variables[key] = value;
                break;
        }
    }

    /// <summary>
    ///     瑙ｆ瀽闀滃儚婧?URL锛屽鏋滈厤缃簡闀滃儚鍒欒繑鍥為暅鍍忓湴鍧€
    /// </summary>
    /// <param name="originalUrl">鍘熷娉ㄥ唽琛?URL</param>
    /// <returns>鏇挎崲鍚庣殑 URL</returns>
    public string resolve_mirror(string originalUrl)
    {
        foreach (var mirror in registry_mirrors)
            if (originalUrl.Contains(mirror.Key))
                return originalUrl.Replace(mirror.Key, mirror.Value);

        return originalUrl;
    }

    public bool validate()
    {
        if (timeout_seconds <= 0) return false;

        if (max_retries < 0) return false;

        return true;
    }

    private void load_global_config()
    {
        if (!File.Exists(_global_config_path)) return;

        load_config_file(_global_config_path, true);
    }

    private void load_project_config(string path)
    {
        if (!File.Exists(path)) return;

        load_config_file(path, false);
    }

    private void load_config_file(string path, bool isGlobal)
    {
        try
        {
            var content = File.ReadAllText(path, Encoding.UTF8);
            if (SerdeConfigNodeAdapter.from_serde(_parse(content)) is not ObjectConfigNode root) return;

            ConfigProjector.project_into(this, root);
        }
        catch
        {
            // 配置文件解析失败时使用默认配置。
        }
    }

    private void save_config(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fields = new Dictionary<string, SerdeValue>
        {
            ["registry"] = SerdeValue.@string(registry),
            ["offline"] = SerdeValue.boolean(offline_mode),
            ["timeout"] = SerdeValue.integer(timeout_seconds.ToString()),
            ["maxRetries"] = SerdeValue.integer(max_retries.ToString()),
            ["verifyIntegrity"] = SerdeValue.boolean(verify_integrity),
            ["strictSsl"] = SerdeValue.boolean(strict_ssl),
            ["maxParallelDownloads"] = SerdeValue.integer(max_parallel_downloads.ToString())
        };

        if (proxy is not null) fields["proxy"] = SerdeValue.@string(proxy);

        if (proxy_username is not null) fields["proxyUsername"] = SerdeValue.@string(proxy_username);

        if (proxy_password is not null) fields["proxyPassword"] = SerdeValue.@string(proxy_password);

        if (cache_directory is not null) fields["cacheDirectory"] = SerdeValue.@string(cache_directory);

        if (registry_endpoints.Count > 0)
        {
            var endpointsDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in registry_endpoints) endpointsDict[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields["registryEndpoints"] = SerdeValue.@object(endpointsDict);
        }

        if (registry_mirrors.Count > 0)
        {
            var mirrorsDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in registry_mirrors) mirrorsDict[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields["registryMirrors"] = SerdeValue.@object(mirrorsDict);
        }

        if (environment_variables.Count > 0)
        {
            var envDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in environment_variables) envDict[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields["environment"] = SerdeValue.@object(envDict);
        }

        var root = SerdeValue.@object(fields);
        var content = VonFormatter.format(root);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private string? find_project_config()
    {
        var currentDir = Environment.CurrentDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            var configPath = Path.Combine(currentDir, "valkyrie.von");
            if (File.Exists(configPath)) return configPath;

            var parent = Path.GetDirectoryName(currentDir);
            if (parent == currentDir) break;

            currentDir = parent ?? string.Empty;
        }

        return null;
    }

}
