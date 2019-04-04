using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Nyar.PackageManager.Version;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Package;

/// <summary>
///     鍖呮竻鍗?legion.von 鐨勬暟鎹ā鍨嬶紙绫绘瘮 package.json锛?///
/// </summary>
public class LegionManifest
{
    private readonly string _file_path;
    private readonly SerdeParser _parse;

    /// <summary>
    ///     鍒涘缓 LegionManifest 瀹炰緥
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public LegionManifest(string directoryPath, SerdeParser parse)
    {
        _file_path = Path.Combine(directoryPath, "legion.von");
        _parse = parse;
    }

    /// <summary>
    ///     鍖呭悕绉帮紙蹇呴』绗﹀悎 @scope/name 鎴?name 鏍煎紡锛?    ///
    /// </summary>
    public string name { get; set; } = string.Empty;

    /// <summary>
    ///     鐗堟湰鍙凤紙閬靛惊 SemVer 2.0锛?    ///
    /// </summary>
    public string version { get; set; } = "0.0.0";

    /// <summary>
    ///     包描述
    /// </summary>
    public string? description { get; set; }

    /// <summary>
    ///     Runner 配置列表，对应 legion.von 中 [runner] 段
    /// </summary>
    public List<RunnerConfig> runner_configs { get; set; } = [];

    /// <summary>
    ///     椤圭洰涓婚〉 URL
    /// </summary>
    public string? homepage { get; set; }

    /// <summary>
    ///     璁稿彲璇佹爣璇嗭紙濡?MIT銆丄pache-2.0锛?    ///
    /// </summary>
    public string? license { get; set; }

    /// <summary>
    ///     浣滆€呬俊鎭?    ///
    /// </summary>
    public string? author { get; set; }

    /// <summary>
    ///     璐＄尞鑰呭垪琛?    ///
    /// </summary>
    public List<string> contributors { get; set; } = [];

    /// <summary>
    ///     浠ｇ爜浠撳簱鍦板潃
    /// </summary>
    public string? repository { get; set; }

    /// <summary>
    ///     Bug 鍙嶉鍦板潃
    /// </summary>
    public string? bugs { get; set; }

    /// <summary>
    ///     璧勯噾璧炲姪淇℃伅
    /// </summary>
    public string? funding { get; set; }

    /// <summary>
    ///     涓诲叆鍙ｆ枃浠惰矾寰?    ///
    /// </summary>
    public string? main { get; set; }

    /// <summary>
    ///     CLI 鍙墽琛屾枃浠惰矾寰?    ///
    /// </summary>
    public string? bin { get; set; }

    /// <summary>
    ///     ES Module 鍏ュ彛鏂囦欢璺緞
    /// </summary>
    public string? module { get; set; }

    /// <summary>
    ///     TypeScript 绫诲瀷瀹氫箟鏂囦欢璺緞
    /// </summary>
    public string? types { get; set; }

    /// <summary>
    ///     瀵煎嚭鏄犲皠
    /// </summary>
    public Dictionary<string, object?>? exports { get; set; }

    /// <summary>
    ///     鎼滅储鍏抽敭璇?    ///
    /// </summary>
    public List<string>? keywords { get; set; }

    /// <summary>
    ///     鏄惁涓虹鏈夊寘锛堜笉鍙戝竷鍒版敞鍐岃〃锛?    ///
    /// </summary>
    public bool @private { get; set; }

    /// <summary>
    ///     鍙戝竷閰嶇疆锛堟寚瀹氱洰鏍囨敞鍐岃〃銆佽闂骇鍒瓑锛?    ///
    /// </summary>
    public PublishConfig publish_config { get; set; } = new();

    /// <summary>
    ///     鏋勫缓鐩爣鍒楄〃锛坙egion.von 鐨?build 瀛楁锛?    ///
    /// </summary>
    public List<BuildTarget> build_targets { get; set; } = [];

    /// <summary>
    ///     是否自动链接 core（来自 auto_link.core，默认 true）
    /// </summary>
    public bool auto_link_core { get; set; } = true;

    /// <summary>
    ///     是否自动链接 std（来自 auto_link.std，默认 true）
    /// </summary>
    public bool auto_link_std { get; set; } = true;

    /// <summary>
    ///     运行时依赖杩愯鏃朵緷璧?    ///
    /// </summary>
    public Dictionary<string, string> dependencies { get; set; } = new();

    /// <summary>
    ///     寮€鍙戜緷璧?    ///
    /// </summary>
    public Dictionary<string, string> dev_dependencies { get; set; } = new();

    /// <summary>
    ///     瀵圭瓑渚濊禆
    /// </summary>
    public Dictionary<string, string> peer_dependencies { get; set; } = new();

    /// <summary>
    ///     鍙€変緷璧?    ///
    /// </summary>
    public Dictionary<string, string> optional_dependencies { get; set; } = new();

    /// <summary>
    ///     鐗堟湰瑕嗙洊瑙勫垯
    /// </summary>
    public Dictionary<string, string> overrides { get; set; } = new();

    /// <summary>
    ///     鑴氭湰瀹氫箟
    /// </summary>
    public Dictionary<string, string> scripts { get; set; } = new();

    /// <summary>
    ///     鐢熷懡鍛ㄦ湡閽╁瓙瀹氫箟
    /// </summary>
    public Dictionary<string, LifecycleHook> hooks { get; set; } = new();

    /// <summary>
    ///     鍖呭惈鏂囦欢鍒楄〃锛堝彂甯冩椂浠呭寘鍚繖浜涙枃浠讹級
    /// </summary>
    public List<string> files { get; set; } = [];

    /// <summary>
    ///     寮曟搸鐗堟湰瑕佹眰锛堝 valkyrie: ">=1.0.0"锛?    ///
    /// </summary>
    public Dictionary<string, string> engines { get; set; } = new();

    /// <summary>
    ///     鎿嶄綔绯荤粺鍏煎鎬?    ///
    /// </summary>
    public Dictionary<string, string> os { get; set; } = new();

    /// <summary>
    ///     CPU 鏋舵瀯鍏煎鎬?    ///
    /// </summary>
    public Dictionary<string, string> cpu { get; set; } = new();

    public static LegionManifest load(string directoryPath, SerdeParser parse)
    {
        var manifest = new LegionManifest(directoryPath, parse);
        manifest.load();
        return manifest;
    }

    public void load()
    {
        if (!File.Exists(_file_path)) throw new FileNotFoundException($"legion.von 鏈壘鍒? {_file_path}");

        var content = File.ReadAllText(_file_path);
        var root = _parse(content);

        if (root.type != SerdeValueType.@object) throw new FormatException("legion.von 鏍瑰厓绱犲繀椤绘槸瀵硅薄");

        name = ValkyrieValueProjector.bind_utf8_field(root, "name") ?? string.Empty;
        version = ValkyrieValueProjector.bind_utf8_field(root, "version") ?? "0.0.0";
        description = ValkyrieValueProjector.bind_utf8_field(root, "description");
        homepage = ValkyrieValueProjector.bind_utf8_field(root, "homepage");
        license = ValkyrieValueProjector.bind_utf8_field(root, "license");
        author = ValkyrieValueProjector.bind_utf8_field(root, "author");
        repository = ValkyrieValueProjector.bind_utf8_field(root, "repository");
        main = ValkyrieValueProjector.bind_utf8_field(root, "main");
        bin = ValkyrieValueProjector.bind_utf8_field(root, "bin");
        module = ValkyrieValueProjector.bind_utf8_field(root, "module");
        types = ValkyrieValueProjector.bind_utf8_field(root, "types");

        var exportsField = root.get_field("exports");
        if (ValkyrieValueProjector.bind_utf8_map_field(root, "exports") is { } exportMap)
            exports = exportMap.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

        if (ValkyrieValueProjector.bind_utf8_array_field(root, "keywords") is { } keywordValues)
            keywords = [.. keywordValues.Where(s => !string.IsNullOrEmpty(s))];

        @private = ValkyrieValueProjector.bind_bool_field(root, "private") ?? false;

        if (ValkyrieValueProjector.bind_utf8_array_field(root, "contributors") is { } contributorValues)
            contributors = [.. contributorValues.Where(s => !string.IsNullOrEmpty(s))];

        var publishConfigField = root.get_field("publishConfig");
        if (publishConfigField is { type: SerdeValueType.@object, fields: not null })
            publish_config = new PublishConfig
            {
                registry = ValkyrieValueProjector.bind_utf8_field(publishConfigField, "registry"),
                access = ValkyrieValueProjector.bind_utf8_field(publishConfigField, "access") ?? "public",
                tag = ValkyrieValueProjector.bind_utf8_field(publishConfigField, "tag") ?? "latest"
            };

        var buildField = root.get_field("build");
        if (buildField is { type: SerdeValueType.array, elements: not null })
            build_targets =
            [
                .. buildField.elements
                    .Where(e => e is { type: SerdeValueType.@object, fields: not null })
                    .Select(e => new BuildTarget
                    {
                        target = ValkyrieValueProjector.bind_utf8_field(e, "target") ?? string.Empty,
                        source_map = ValkyrieValueProjector.bind_bool_field(e, "source_map") ?? false,
                        type_script = ValkyrieValueProjector.bind_bool_field(e, "typescript") ?? false,
                        wat = ValkyrieValueProjector.bind_bool_field(e, "wat") ?? false,
                        msil = ValkyrieValueProjector.bind_bool_field(e, "msil") ?? false
                    })
                    .Where(bt => !string.IsNullOrEmpty(bt.target))
            ];

        var autoLinkField = root.get_field("auto_link");
        if (autoLinkField is { type: SerdeValueType.@object, fields: not null })
        {
            auto_link_core = ValkyrieValueProjector.bind_bool_field(autoLinkField, "core") ?? auto_link_core;
            auto_link_std = ValkyrieValueProjector.bind_bool_field(autoLinkField, "std") ?? auto_link_std;
        }

        load_dictionary(root, "dependencies", dependencies);
        load_dictionary(root, "devDependencies", dev_dependencies);
        load_dictionary(root, "peerDependencies", peer_dependencies);
        load_dictionary(root, "optionalDependencies", optional_dependencies);
        load_dictionary(root, "scripts", scripts);
        load_hooks(root);
        load_dictionary(root, "engines", engines);
        load_dictionary(root, "os", os);
        load_dictionary(root, "cpu", cpu);
        load_dictionary(root, "overrides", overrides);

        var runnerSection = root.get_field("runner")?.elements;
        if (runnerSection is not null)
            foreach (var entry in runnerSection)
            {
                var target = ValkyrieValueProjector.bind_utf8_field(entry, "target");
                var command = ValkyrieValueProjector.bind_utf8_field(entry, "command");
                var args = ValkyrieValueProjector.bind_utf8_array_field(entry, "args")?
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? [];

                if (!string.IsNullOrWhiteSpace(target) && !string.IsNullOrWhiteSpace(command))
                    runner_configs.Add(new RunnerConfig
                    {
                        target = target,
                        command = command,
                        args = args
                    });
            }

        if (ValkyrieValueProjector.bind_utf8_array_field(root, "files") is { } fileValues)
            files = [.. fileValues.Where(s => !string.IsNullOrEmpty(s))];

        var baseDir = Path.GetDirectoryName(_file_path);
        if (baseDir is not null)
        {
            var scriptDir = Path.Combine(baseDir, "script");
            if (Directory.Exists(scriptDir))
                foreach (var scriptFile in Directory.GetFiles(scriptDir, "*.v"))
                {
                    var scriptName = Path.GetFileNameWithoutExtension(scriptFile);
                    if (!scripts.ContainsKey(scriptName)) scripts[scriptName] = $"run {scriptFile}";
                }
        }
    }

    public void save()
    {
        var dir = Path.GetDirectoryName(_file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fields = new Dictionary<string, SerdeValue>
        {
            ["name"] = SerdeValue.@string(name),
            ["version"] = SerdeValue.@string(version)
        };

        if (description is not null) fields["description"] = SerdeValue.@string(description);

        // 始终输出 auto_link 字段（即使是默认值，用于显式声明意图）
        var autoLinkFields = new Dictionary<string, SerdeValue>
        {
            ["core"] = SerdeValue.boolean(auto_link_core),
            ["std"] = SerdeValue.boolean(auto_link_std)
        };
        fields["auto_link"] = SerdeValue.@object(autoLinkFields);

        if (homepage is not null) fields["homepage"] = SerdeValue.@string(homepage);

        if (license is not null) fields["license"] = SerdeValue.@string(license);

        if (author is not null) fields["author"] = SerdeValue.@string(author);

        if (repository is not null) fields["repository"] = SerdeValue.@string(repository);

        if (bugs is not null) fields["bugs"] = SerdeValue.@string(bugs);

        if (funding is not null) fields["funding"] = SerdeValue.@string(funding);

        if (main is not null) fields["main"] = SerdeValue.@string(main);

        if (bin is not null) fields["bin"] = SerdeValue.@string(bin);

        if (module is not null) fields["module"] = SerdeValue.@string(module);

        if (types is not null) fields["types"] = SerdeValue.@string(types);

        if (@private) fields["private"] = SerdeValue.boolean(@private);

        if (contributors.Count > 0)
            fields["contributors"] = SerdeValue.array([.. contributors.Select(c => SerdeValue.@string(c))]);

        if (publish_config.registry is not null || publish_config.access != "public" || publish_config.tag != "latest")
        {
            var publishConfigFields = new Dictionary<string, SerdeValue>();
            if (publish_config.registry is not null)
                publishConfigFields["registry"] = SerdeValue.@string(publish_config.registry);

            if (publish_config.access != "public")
                publishConfigFields["access"] = SerdeValue.@string(publish_config.access);

            if (publish_config.tag != "latest") publishConfigFields["tag"] = SerdeValue.@string(publish_config.tag);

            fields["publishConfig"] = SerdeValue.@object(publishConfigFields);
        }

        if (exports is not null && exports.Count > 0)
        {
            var exportFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in exports)
                exportFields[kvp.Key] = SerdeValue.@string(kvp.Value?.ToString() ?? string.Empty);

            fields["exports"] = SerdeValue.@object(exportFields);
        }

        if (keywords is not null && keywords.Count > 0)
            fields["keywords"] = SerdeValue.array([.. keywords.Select(k => SerdeValue.@string(k))]);

        if (build_targets.Count > 0)
        {
            var buildElements = build_targets.Select(bt =>
            {
                var btFields = new Dictionary<string, SerdeValue>
                {
                    ["target"] = SerdeValue.@string(bt.target)
                };
                if (bt.source_map) btFields["source_map"] = SerdeValue.boolean(true);

                if (bt.type_script) btFields["typescript"] = SerdeValue.boolean(true);

                if (bt.wat) btFields["wat"] = SerdeValue.boolean(true);

                if (bt.msil) btFields["msil"] = SerdeValue.boolean(true);

                return SerdeValue.@object(btFields);
            }).ToList();

            fields["build"] = SerdeValue.array(buildElements);
        }

        save_dictionary(fields, "dependencies", dependencies);
        save_dictionary(fields, "devDependencies", dev_dependencies);
        save_dictionary(fields, "peerDependencies", peer_dependencies);
        save_dictionary(fields, "optionalDependencies", optional_dependencies);
        save_dictionary(fields, "scripts", scripts);
        save_hooks(fields);
        save_dictionary(fields, "engines", engines);
        save_dictionary(fields, "os", os);
        save_dictionary(fields, "cpu", cpu);
        save_dictionary(fields, "overrides", overrides);

        if (runner_configs.Count > 0)
        {
            var runnerElements = runner_configs.Select(rc =>
            {
                var rcFields = new Dictionary<string, SerdeValue>
                {
                    ["target"] = SerdeValue.@string(rc.target),
                    ["command"] = SerdeValue.@string(rc.command)
                };

                if (rc.args.Count > 0)
                    rcFields["args"] = SerdeValue.array([.. rc.args.Select(a => SerdeValue.@string(a))]);

                return SerdeValue.@object(rcFields);
            }).ToList();

            fields["runner"] = SerdeValue.array(runnerElements);
        }

        if (files.Count > 0) fields["files"] = SerdeValue.array([.. files.Select(f => SerdeValue.@string(f))]);

        var root = SerdeValue.@object(fields);
        File.WriteAllText(_file_path, VonFormatter.format(root));
    }

    public bool exists()
    {
        return File.Exists(_file_path);
    }

    public bool validate()
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (!SemanticVersion.try_parse(version, out _)) return false;

        if (name.StartsWith('@') && !name.Contains('/')) return false;

        if (name.Any(c => char.IsUpper(c))) return false;

        return true;
    }

    /// <summary>
    ///     璇︾粏楠岃瘉锛岃繑鍥炴墍鏈夐獙璇侀敊璇垪琛?    ///
    /// </summary>
    /// <returns>楠岃瘉閿欒鍒楄〃锛堢┖鍒楄〃琛ㄧず閫氳繃锛?/returns>
    public List<string> validate_detailed()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("包名称不能为空");
        }
        else
        {
            if (name.Any(c => char.IsUpper(c))) errors.Add("包名称不能包含大写字母");

            if (name.StartsWith('@') && !name.Contains('/')) errors.Add("浣滅敤鍩熷寘鍚嶇О鏍煎紡搴斾负 @scope/name");

            if (name.Length > 214) errors.Add("包名称长度不能超过 214 个字符");
        }

        if (!SemanticVersion.try_parse(version, out _)) errors.Add($"版本号 '{version}' 不是有效的 SemVer 2.0 格式");

        if (@private && publish_config.access == "public") errors.Add("私有包不能设置 publishConfig.access 为 public");

        if (engines.TryGetValue("valkyrie", out var engineVersion))
            if (!SemanticVersion.try_parse(engineVersion.TrimStart('^', '~', '>', '<', '='), out _))
                errors.Add($"寮曟搸鐗堟湰绾︽潫 '{engineVersion}' 鏍煎紡鏃犳晥");

        foreach (var dep in dependencies)
            if (string.IsNullOrWhiteSpace(dep.Key))
                errors.Add("渚濊禆鍚嶇О涓嶈兘涓虹┖");

        return errors;
    }

    public string get_script(string name)
    {
        return scripts.TryGetValue(name, out var script) ? script : string.Empty;
    }

    public bool has_script(string name)
    {
        return scripts.ContainsKey(name);
    }

    /// <summary>
    ///     鑾峰彇鎸囧畾鍚嶇О鐨勯挬瀛愬畾涔?    ///
    /// </summary>
    /// <param name="name">閽╁瓙鍚嶇О</param>
    public LifecycleHook? get_hook(string name)
    {
        return hooks.GetValueOrDefault(name);
    }

    /// <summary>
    ///     妫€鏌ユ寚瀹氶挬瀛愭槸鍚﹀瓨鍦?    ///
    /// </summary>
    /// <param name="name">閽╁瓙鍚嶇О</param>
    public bool has_hook(string name)
    {
        return hooks.ContainsKey(name);
    }

    /// <summary>
    ///     鑾峰彇婧愮爜涓墍鏈夋爣璁颁负 [main] 鐨?micro 鍑芥暟鍚嶇О鍙婂叾鎵€灞炴枃浠?    ///
    /// </summary>
    /// <returns>micro 鍑芥暟鍚?鈫?(鏂囦欢璺緞, 琛屽唴瀹? 鐨勬槧灏?/returns>
    public Dictionary<string, (string FilePath, string Line)> get_micro_functions()
    {
        var result = new Dictionary<string, (string, string)>();

        var baseDir = Path.GetDirectoryName(_file_path);
        if (baseDir is null) return result;

        var sourceDir = Path.Combine(baseDir, "source");
        if (!Directory.Exists(sourceDir)) return result;

        foreach (var vFile in Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(vFile);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();

                if (line.StartsWith("micro ") && i >= 1)
                {
                    var prevLine = lines[i - 1].Trim();
                    if (prevLine == "[main]")
                    {
                        var funcName = line["micro ".Length..].Trim();
                        var parenIdx = funcName.IndexOf('(');
                        if (parenIdx > 0) funcName = funcName[..parenIdx].Trim();

                        var braceIdx = funcName.IndexOf('{');
                        if (braceIdx > 0) funcName = funcName[..braceIdx].Trim();

                        result[funcName] = (vFile, line);
                    }
                }
            }
        }

        return result;
    }

    public void add_dependency(string packageName, string version, string type = "dependencies")
    {
        var dict = type switch
        {
            "dev" or "devDependencies" => dev_dependencies,
            "peer" or "peerDependencies" => peer_dependencies,
            "optional" or "optionalDependencies" => optional_dependencies,
            _ => dependencies
        };

        dict[packageName] = version;
    }

    public void remove_dependency(string packageName, string type = "dependencies")
    {
        var dict = type switch
        {
            "dev" or "devDependencies" => dev_dependencies,
            "peer" or "peerDependencies" => peer_dependencies,
            "optional" or "optionalDependencies" => optional_dependencies,
            _ => dependencies
        };

        dict.Remove(packageName);
    }

    private void load_dictionary(SerdeValue root, string key, Dictionary<string, string> target)
    {
        target.Clear();
        if (ValkyrieValueProjector.bind_utf8_map_field(root, key) is { } mapValues)
            foreach (var kvp in mapValues)
                target[kvp.Key] = kvp.Value;
    }

    private void save_dictionary(Dictionary<string, SerdeValue> fields, string key, Dictionary<string, string> source)
    {
        if (source.Count > 0)
        {
            var dict = new Dictionary<string, SerdeValue>();
            foreach (var kvp in source.OrderBy(p => p.Key)) dict[kvp.Key] = SerdeValue.@string(kvp.Value);

            fields[key] = SerdeValue.@object(dict);
        }
    }

    private void load_hooks(SerdeValue root)
    {
        hooks.Clear();
        var hooksField = root.get_field("hooks");
        if (hooksField?.type != SerdeValueType.@object || hooksField.fields is null) return;

        foreach (var kvp in hooksField.fields)
        {
            var hookValue = kvp.Value;
            var hook = new LifecycleHook();

            if (hookValue.type == SerdeValueType.@string)
            {
                hook.command = ValkyrieValueProjector.as_utf8(
                    ValkyrieValueProjector.bind_literal(hookValue, ValkyrieValueProjector.utf8_type)) ?? string.Empty;
            }
            else if (hookValue is { type: SerdeValueType.@object, fields: not null })
            {
                hook.command = ValkyrieValueProjector.bind_utf8_field(hookValue, "command") ?? string.Empty;
                hook.fail_on_error = ValkyrieValueProjector.bind_bool_field(hookValue, "failOnError") ?? hook.fail_on_error;
                hook.shell = ValkyrieValueProjector.bind_utf8_field(hookValue, "shell");
                hook.condition = ValkyrieValueProjector.bind_utf8_field(hookValue, "condition");
                hook.description = ValkyrieValueProjector.bind_utf8_field(hookValue, "description");
            }

            hooks[kvp.Key] = hook;
        }
    }

    private void save_hooks(Dictionary<string, SerdeValue> fields)
    {
        if (hooks.Count > 0)
        {
            var hookFields = new Dictionary<string, SerdeValue>();
            foreach (var kvp in hooks.OrderBy(p => p.Key))
            {
                var hookDict = new Dictionary<string, SerdeValue>
                {
                    ["command"] = SerdeValue.@string(kvp.Value.command),
                    ["failOnError"] = SerdeValue.boolean(kvp.Value.fail_on_error)
                };

                if (kvp.Value.shell is not null) hookDict["shell"] = SerdeValue.@string(kvp.Value.shell);

                if (kvp.Value.condition is not null) hookDict["condition"] = SerdeValue.@string(kvp.Value.condition);

                if (kvp.Value.description is not null)
                    hookDict["description"] = SerdeValue.@string(kvp.Value.description);

                hookFields[kvp.Key] = SerdeValue.@object(hookDict);
            }

            fields["hooks"] = SerdeValue.@object(hookFields);
        }
    }

    /// <summary>
    ///     从 workspace legions.von 解析 workspace 级共享配置（license、version、author）。
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录</param>
    /// <param name="parse">VON 解析器</param>
    /// <returns>解析结果，找不到或解析失败则返回 null</returns>
    public static (string? license, string? version, string? author)? parse_workspace_config(string workspaceDir,
        SerdeParser parse)
    {
        var legionsPath = Path.Combine(workspaceDir, "legions.von");
        if (!File.Exists(legionsPath))
        {
            return null;
        }

        try
        {
            var content = File.ReadAllText(legionsPath);
            var root = parse(content);
            var workspaceField = root.get_field("workspace");
            if (workspaceField?.type != SerdeValueType.@object || workspaceField.fields is null)
            {
                return null;
            }

            var license = ValkyrieValueProjector.bind_utf8_field(workspaceField, "license");
            var version = ValkyrieValueProjector.bind_utf8_field(workspaceField, "version");
            var author = ValkyrieValueProjector.bind_utf8_field(workspaceField, "author");

            return (license, version, author);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     应用 workspace 共享配置：
    ///     若 version 为 "workspace" 则替换为实际 workspace 版本；
    ///     若 license / author 为空则从 workspace 继承。
    ///     若 workspaceConfig 为 null 则不修改。
    /// </summary>
    /// <param name="workspaceConfig">parse_workspace_config 的返回值</param>
    public void apply_workspace_defaults(
        (string? license, string? version, string? author)? workspaceConfig)
    {
        if (workspaceConfig is not { } ws)
        {
            return;
        }

        if (string.Equals(version, "workspace", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(ws.version))
        {
            version = ws.version;
        }

        if (string.IsNullOrWhiteSpace(license) && !string.IsNullOrWhiteSpace(ws.license))
        {
            license = ws.license;
        }

        if (string.IsNullOrWhiteSpace(author) && !string.IsNullOrWhiteSpace(ws.author))
        {
            author = ws.author;
        }
    }

    /// <summary>
    ///     标准生命周期钩子名称
    /// </summary>
    public static class HookNames
    {
        public const string pre_build = "preBuild";
        public const string post_build = "postBuild";
        public const string pre_publish = "prePublish";
        public const string post_publish = "postPublish";
        public const string pre_install = "preInstall";
        public const string post_install = "postInstall";
        public const string pre_pack = "prePack";
        public const string post_pack = "postPack";
        public const string pre_test = "preTest";
        public const string post_test = "postTest";
        public const string pre_clean = "preClean";
        public const string post_clean = "postClean";
    }
}

/// <summary>
///     依赖项元数据（支持对象格式的依赖声明）
/// </summary>
public class DependencyEntry
{
    /// <summary>
    ///     版本号或版本约束
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     是否为仅 test/bench/coverage 模式可见的依赖
    /// </summary>
    public bool test { get; set; }
}

/// <summary>
///     Runner 配置，对应 legion.von 中 [runner] 段的单个条目
/// </summary>
public sealed class RunnerConfig
{
    /// <summary>
    ///     目标标签（如 clr、jvm、node）
    /// </summary>
    public string target { get; set; } = string.Empty;

    /// <summary>
    ///     执行命令（可执行文件名或绝对路径）
    /// </summary>
    public string command { get; set; } = string.Empty;

    /// <summary>
    ///     参数模板数组，支持占位符 {artifact}、{classpath}、{entry}
    /// </summary>
    public List<string> args { get; set; } = [];
}
