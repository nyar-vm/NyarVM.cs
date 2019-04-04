using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Package;
using Nyar.PackageManager.Tools;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Workspace;

public class LegionsWorkspace
{
    #region 鑷姩鍙戠幇

    private void auto_discover_members()
    {
        var discovered = new List<string>();

        foreach (var memberPath in members.ToList())
        {
            var fullPath = Path.Combine(_directory_path, memberPath);
            if (Directory.Exists(fullPath)) discovered.Add(memberPath);
        }

        var packagesDir = Path.Combine(_directory_path, "packages");
        if (Directory.Exists(packagesDir))
            foreach (var dir in Directory.GetDirectories(packagesDir))
            {
                var relativePath = Path.GetRelativePath(_directory_path, dir);
                var legionPath = Path.Combine(dir, "legion.von");
                if (File.Exists(legionPath) && !discovered.Contains(relativePath)) discovered.Add(relativePath);
            }

        var projectsDir = Path.Combine(_directory_path, "projects");
        if (Directory.Exists(projectsDir))
            foreach (var dir in Directory.GetDirectories(projectsDir))
            {
                var relativePath = Path.GetRelativePath(_directory_path, dir);
                var legionPath = Path.Combine(dir, "legion.von");
                if (File.Exists(legionPath) && !discovered.Contains(relativePath)) discovered.Add(relativePath);
            }

        members = discovered;
    }

    #endregion

    #region 灞炴€?

    public string version { get; set; } = "1";
    public string name { get; set; } = string.Empty;
    public List<string> members { get; set; } = [];

    /// <summary>
    ///     鑴氭湰鏄犲皠
    /// </summary>
    public Dictionary<string, string> scripts { get; set; } = new();

    /// <summary>
    ///     鍏变韩杩愯鏃朵緷璧?    ///
    /// </summary>
    public Dictionary<string, string> dependencies { get; set; } = new();

    /// <summary>
    ///     鍏变韩寮€鍙戜緷璧?    ///
    /// </summary>
    public Dictionary<string, string> dev_dependencies { get; set; } = new();

    /// <summary>
    ///     鏄惁鍚敤渚濊禆鎻愬崌锛坔oisting锛夛細灏嗗叡浜緷璧栨彁鍗囧埌宸ヤ綔鍖烘牴鐩綍
    /// </summary>
    public bool hoisting { get; set; } = true;

    /// <summary>
    ///     鎻愬崌绛栫暐锛歛ll锛堝叏閮ㄦ彁鍗囷級/ shared锛堜粎澶氬寘鍏变韩鐨勪緷璧栵級/ none锛堜笉鎻愬崌锛?    ///
    /// </summary>
    public string hoisting_strategy { get; set; } = "shared";

    /// <summary>
    ///     鍏变韩渚濊禆鐨勬渶灏忓紩鐢ㄦ鏁帮紙杈惧埌姝ゆ鏁版墠鎻愬崌锛岄粯璁?2锛?    ///
    /// </summary>
    public int min_hoist_references { get; set; } = 2;

    private readonly string _file_path;
    private readonly string _directory_path;
    private readonly SerdeParser _parse;

    #endregion

    #region 鏋勯€犱笌鍔犺浇

    /// <summary>
    ///     鍒涘缓宸ヤ綔鍖哄疄渚?    ///
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public LegionsWorkspace(string directoryPath, SerdeParser parse)
    {
        _directory_path = directoryPath;
        _file_path = Path.Combine(directoryPath, "voa.workspace.v");
        _parse = parse;
    }

    /// <summary>
    ///     浠庣洰褰曞姞杞藉伐浣滃尯
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public static LegionsWorkspace load(string directoryPath, SerdeParser parse)
    {
        var workspace = new LegionsWorkspace(directoryPath, parse);
        workspace.load();
        return workspace;
    }

    public void load()
    {
        if (!File.Exists(_file_path)) throw new FileNotFoundException($"voa.workspace.v 鏈壘鍒? {_file_path}");

        var content = File.ReadAllText(_file_path);
        var root = _parse(content);

        if (root.type != SerdeValueType.@object) throw new FormatException("voa.workspace.v 鏍瑰厓绱犲繀椤绘槸瀵硅薄");

        version = ValkyrieValueProjector.bind_utf8_field(root, "version") ?? "1";
        name = ValkyrieValueProjector.bind_utf8_field(root, "name") ?? string.Empty;

        if (ValkyrieValueProjector.bind_utf8_array_field(root, "members") is { } memberValues)
            members = [.. memberValues.Where(s => !string.IsNullOrEmpty(s))];

        load_dictionary(root, "scripts", scripts);
        load_dictionary(root, "dependencies", dependencies);
        load_dictionary(root, "devDependencies", dev_dependencies);

        auto_discover_members();
    }

    public void save()
    {
        var dir = Path.GetDirectoryName(_file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fields = new Dictionary<string, SerdeValue>
        {
            ["version"] = SerdeValue.@string(version)
        };

        if (!string.IsNullOrEmpty(name)) fields["name"] = SerdeValue.@string(name);

        if (members.Count > 0)
            fields["members"] = SerdeValue.array([.. members.Select(m => SerdeValue.@string(m))]);

        save_dictionary(fields, "scripts", scripts);
        save_dictionary(fields, "dependencies", dependencies);
        save_dictionary(fields, "devDependencies", dev_dependencies);

        var root = SerdeValue.@object(fields);
        File.WriteAllText(_file_path, VonFormatter.format(root));
    }

    public bool exists()
    {
        return File.Exists(_file_path);
    }

    #endregion

    #region 鎴愬憳绠＄悊

    public List<LegionManifest> get_member_manifests()
    {
        var manifests = new List<LegionManifest>();

        foreach (var memberPath in members)
        {
            var fullPath = Path.Combine(_directory_path, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath, _parse);
                if (manifest.exists())
                {
                    manifest.load();
                    manifests.Add(manifest);
                }
            }
        }

        return manifests;
    }

    public LegionManifest? get_member_manifest(string memberName)
    {
        foreach (var memberPath in members)
        {
            var fullPath = Path.Combine(_directory_path, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath, _parse);
                if (manifest.exists())
                {
                    manifest.load();
                    if (manifest.name == memberName) return manifest;
                }
            }
        }

        return null;
    }

    public void add_member(string path)
    {
        if (!members.Contains(path)) members.Add(path);
    }

    public void remove_member(string path)
    {
        members.Remove(path);
    }

    public string? find_member_path(string memberName)
    {
        foreach (var memberPath in members)
        {
            var fullPath = Path.Combine(_directory_path, memberPath);
            if (Directory.Exists(fullPath))
            {
                var manifest = new LegionManifest(fullPath, _parse);
                if (manifest.exists())
                {
                    manifest.load();
                    if (manifest.name == memberName) return fullPath;
                }
            }
        }

        return null;
    }

    #endregion

    #region 宸ヤ綔鍖轰緷璧栧浘

    public WorkspaceDependencyGraph build_dependency_graph()
    {
        var graph = new WorkspaceDependencyGraph();
        var manifests = get_member_manifests();
        var memberNames = manifests.Select(m => m.name).ToHashSet();

        foreach (var manifest in manifests)
        {
            var internalDeps = new List<string>();
            var externalDeps = new List<string>();

            foreach (var dep in manifest.dependencies)
                if (memberNames.Contains(dep.Key))
                    internalDeps.Add(dep.Key);
                else
                    externalDeps.Add($"{dep.Key}@{dep.Value}");

            graph.internal_dependencies[manifest.name] = internalDeps;
            graph.external_dependencies[manifest.name] = externalDeps;
        }

        return graph;
    }

    public List<string> get_build_order()
    {
        var graph = build_dependency_graph();
        return graph.get_topological_order();
    }

    public Dictionary<string, string> collect_all_external_dependencies()
    {
        var allDeps = new Dictionary<string, string>();
        var manifests = get_member_manifests();
        var memberNames = manifests.Select(m => m.name).ToHashSet();

        foreach (var dep in dependencies)
            if (!memberNames.Contains(dep.Key))
                allDeps[dep.Key] = dep.Value;

        foreach (var manifest in manifests)
        foreach (var dep in manifest.dependencies)
            if (!memberNames.Contains(dep.Key) && !allDeps.ContainsKey(dep.Key))
                allDeps[dep.Key] = dep.Value;

        return allDeps;
    }

    public bool is_workspace_dependency(string packageName)
    {
        var manifests = get_member_manifests();
        return manifests.Any(m => m.name == packageName);
    }

    #endregion

    #region 鑴氭湰绠＄悊

    public string get_script(string name)
    {
        return scripts.TryGetValue(name, out var script) ? script : string.Empty;
    }

    public bool has_script(string name)
    {
        return scripts.ContainsKey(name);
    }

    #endregion

    #region 绉佹湁鏂规硶

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

    #endregion
}
