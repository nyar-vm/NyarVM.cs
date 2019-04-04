using Core.Data;
using Std.Config;
using Nyar.PackageManager.Package;
using Nyar.PackageManager.Tools;
using Std.Config.Node;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Config;

/// <summary>
///     VOA 椤圭洰鏋勫缓閰嶇疆锛屽搴?voa.config.v 鏂囦欢锛堢被姣?vite.config.ts锛?///     涓?<see cref="LegionManifest" />
///     锛坙egion.von锛岀被姣?package.json锛夌嫭绔嬪叡瀛?///
/// </summary>
[Data]
public class VoaConfig
{
    private readonly string _file_path;
    private readonly SerdeParser _parse;

    /// <summary>
    ///     鍒涘缓 VoaConfig 瀹炰緥
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public VoaConfig(string directoryPath, SerdeParser parse)
    {
        _file_path = Path.Combine(directoryPath, "voa.config.v");
        _parse = parse;
    }

    /// <summary>
    ///     椤圭洰绫诲瀷锛歭ibrary / application / sdk
    /// </summary>
    public string project_type { get; set; } = "application";

    /// <summary>
    ///     涓绘瀯寤虹洰鏍囷紙鍚戝悗鍏煎鍗曠洰鏍囧満鏅級
    /// </summary>
    public TargetConfig target { get; set; } = new();

    /// <summary>
    ///     澶氱洰鏍囩紪璇戝垪琛紝涓虹┖鍒欎粎浣跨敤 <see cref="target" />
    /// </summary>
    public List<TargetConfig> targets { get; set; } = [];

    /// <summary>
    ///     缂栬瘧閫夐」
    /// </summary>
    public BuildConfig build { get; set; } = new();

    /// <summary>
    ///     璧勬簮涓庨潤鎬佹枃浠堕厤缃?    ///
    /// </summary>
    public AssetsConfig assets { get; set; } = new();

    /// <summary>
    ///     鏉′欢缂栬瘧閰嶇疆
    /// </summary>
    public ConditionalConfig conditional { get; set; } = new();

    /// <summary>
    ///     鏉′欢缂栬瘧瀹忓畾涔夛紙鍚戝悗鍏煎鏃ф牸寮忥級
    /// </summary>
    public Dictionary<string, string> defines { get; set; } = new();

    /// <summary>
    ///     鐜鍙橀噺
    /// </summary>
    public Dictionary<string, string> environment { get; set; } = new();

    /// <summary>
    ///     浠庣洰褰曞姞杞?voa.config.v
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public static VoaConfig load(string directoryPath, SerdeParser parse)
    {
        var config = new VoaConfig(directoryPath, parse);
        config.load();
        return config;
    }

    /// <summary>
    ///     瑙ｆ瀽 voa.config.v 鏂囦欢锛屾枃浠朵笉瀛樺湪鏃朵娇鐢ㄩ粯璁ら厤缃?    ///
    /// </summary>
    public void load()
    {
        if (!File.Exists(_file_path)) return;

        var content = File.ReadAllText(_file_path);
        if (SerdeConfigNodeAdapter.from_serde(_parse(content)) is not ObjectConfigNode root) return;

        ConfigProjector.project_into(this, root);
    }

    /// <summary>
    ///     搴忓垪鍖栧苟淇濆瓨鍒?voa.config.v
    /// </summary>
    public void save()
    {
        var dir = Path.GetDirectoryName(_file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fields = new Dictionary<string, SerdeValue>
        {
            ["project_type"] = SerdeValue.@string(project_type),
            ["target"] = SerdeValue.@object(new Dictionary<string, SerdeValue>
            {
                ["arch"] = SerdeValue.@string(target.arch),
                ["os"] = SerdeValue.@string(target.os)
            }),
            ["build"] = SerdeValue.@object(new Dictionary<string, SerdeValue>
            {
                ["optimize"] = SerdeValue.@string(build.optimize),
                ["debug_symbols"] = SerdeValue.boolean(build.debug_symbols),
                ["output_dir"] = SerdeValue.@string(build.output_dir),
                ["sourcemap"] = SerdeValue.boolean(build.sourcemap)
            }),
            ["assets"] = SerdeValue.@object(new Dictionary<string, SerdeValue>
            {
                ["public_dir"] = SerdeValue.@string(assets.public_dir),
                ["assets_dir"] = SerdeValue.@string(assets.assets_dir)
            })
        };

        if (target.abi is not null) fields["target"].fields!["abi"] = SerdeValue.@string(target.abi);

        save_targets_list(fields);
        save_conditional_config(fields);

        if (defines.Count > 0)
        {
            var defineFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in defines) defineFields[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields["defines"] = SerdeValue.@object(defineFields);
        }

        if (environment.Count > 0)
        {
            var envFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in environment) envFields[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields["environment"] = SerdeValue.@object(envFields);
        }

        var root = SerdeValue.@object(fields);
        File.WriteAllText(_file_path, VonFormatter.format(root));
    }

    /// <summary>
    ///     妫€鏌?voa.config.v 鏄惁瀛樺湪
    /// </summary>
    public bool exists()
    {
        return File.Exists(_file_path);
    }

    /// <summary>
    ///     鑾峰彇鎵€鏈夋湁鏁堢紪璇戠洰鏍囷紙Target + Targets 鍚堝苟锛?    ///
    /// </summary>
    public List<TargetConfig> get_all_targets()
    {
        if (targets.Count > 0) return [.. targets];

        return [target];
    }

    private void save_targets_list(Dictionary<string, SerdeValue> fields)
    {
        if (targets.Count > 0)
        {
            var targetElements = new List<SerdeValue>();
            foreach (var target in targets)
            {
                var targetFields = new Dictionary<string, SerdeValue>
                {
                    ["arch"] = SerdeValue.@string(target.arch),
                    ["os"] = SerdeValue.@string(target.os)
                };

                if (target.abi is not null) targetFields["abi"] = SerdeValue.@string(target.abi);

                targetElements.Add(SerdeValue.@object(targetFields));
            }

            fields["targets"] = SerdeValue.array(targetElements);
        }
    }

    private void save_conditional_config(Dictionary<string, SerdeValue> fields)
    {
        if (conditional.define_constants.Count == 0
            && conditional.condition_defines.Count == 0
            && conditional.exclude_files.Count == 0
            && conditional.exclude_directories.Count == 0)
            return;

        var conditionalFields = new Dictionary<string, SerdeValue>();

        if (conditional.define_constants.Count > 0)
        {
            var defineDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in conditional.define_constants)
                defineDict[kvp.Key] = kvp.Value is not null
                    ? SerdeValue.@string(kvp.Value)
                    : SerdeValue.@string(string.Empty);

            conditionalFields["defines"] = SerdeValue.@object(defineDict);
        }

        if (conditional.condition_defines.Count > 0)
        {
            var condDefineDict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in conditional.condition_defines) condDefineDict[kvp.Key] = SerdeValue.@string(kvp.Value);

            conditionalFields["condition_defines"] = SerdeValue.@object(condDefineDict);
        }

        save_conditional_file_patterns(conditionalFields, "exclude_files", conditional.exclude_files);
        save_conditional_file_patterns(conditionalFields, "exclude_directories", conditional.exclude_directories);

        fields["conditional"] = SerdeValue.@object(conditionalFields);
    }

    private static void save_conditional_file_patterns(
        Dictionary<string, SerdeValue> fields,
        string fieldName,
        List<ConditionalFilePattern> sourceList)
    {
        if (sourceList.Count == 0) return;

        var elements = new List<SerdeValue>();
        foreach (var item in sourceList)
            if (item.condition is null)
            {
                elements.Add(SerdeValue.@string(item.pattern));
            }
            else
            {
                var itemFields = new Dictionary<string, SerdeValue>
                {
                    ["pattern"] = SerdeValue.@string(item.pattern),
                    ["condition"] = SerdeValue.@string(item.condition)
                };

                elements.Add(SerdeValue.@object(itemFields));
            }

        fields[fieldName] = SerdeValue.array(elements);
    }

}
