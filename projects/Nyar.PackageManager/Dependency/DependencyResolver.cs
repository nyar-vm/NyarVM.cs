using Nyar.PackageManager.Config;
using Nyar.PackageManager.Version;
using Nyar.PackageRegistry;
using IRegistry = Nyar.PackageRegistry.IRegistry;

namespace Nyar.PackageManager.Dependency;

public class DependencyResolver
{
    private const int _max_recursion_depth = 100;
    private const string _workspace_prefix = "workspace:";
    private readonly List<string> _auto_resolved_sdks = [];
    private readonly TargetCondition _current_target;
    private readonly Dictionary<string, string> _overrides;

    private readonly Dictionary<string, IRegistry> _registries;
    private readonly Dictionary<string, PackageRegistry.Package> _resolved_packages = new();
    private readonly Dictionary<string, List<VersionRequest>> _version_requests = new();
    private readonly Dictionary<string, string> _workspace_members;
    private int _recursion_depth;

    /// <summary>
    ///     创建依赖解析器
    /// </summary>
    /// <param name="registries">注册表映射</param>
    /// <param name="overrides">版本覆盖（来自 legion.von overrides）</param>
    /// <param name="workspaceMembers">工作区成员映射：包名 → 目录路径（来自 voa.workspace.v）</param>
    public DependencyResolver(Dictionary<string, IRegistry> registries, Dictionary<string, string>? overrides = null,
        Dictionary<string, string>? workspaceMembers = null)
    {
        _registries = registries;
        _overrides = overrides ?? new Dictionary<string, string>();
        _workspace_members = workspaceMembers ?? new Dictionary<string, string>();
        _current_target = new TargetCondition();
    }

    public void set_target(string arch, string? os = null, string? abi = null, string? channel = null)
    {
        _current_target.arch = arch;
        _current_target.os = os;
        _current_target.abi = abi;
        _current_target.channel = channel;
    }

    public async Task<DependencyNode> resolve(string packageName, string version, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
            throw new ArgumentException($"注册器 {registryName} 未找到");

        var visited = new HashSet<string>();
        return await resolve_recursive(packageName, version, registryName, registry, visited, null);
    }

    public async Task<List<DependencyNode>> resolve_all(Dictionary<string, string> dependencies,
        string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
            throw new ArgumentException($"注册器 {registryName} 未找到");

        var results = new List<DependencyNode>();
        var visited = new HashSet<string>();

        foreach (var dep in dependencies)
        {
            var node = await resolve_recursive(dep.Key, dep.Value, registryName, registry, visited, null);
            results.Add(node);
        }

        return results;
    }

    private async Task<DependencyNode> resolve_recursive(
        string packageName,
        string versionSpec,
        string registryName,
        IRegistry registry,
        HashSet<string> visited,
        string? targetCondition)
    {
        _recursion_depth++;
        if (_recursion_depth > _max_recursion_depth)
            throw new DependencyResolutionException(
                $"依赖解析超出最大递归深度 {_max_recursion_depth}，可能存在循环依赖：{packageName}@{versionSpec}");

        // 检查 overrides：用户强制指定的版本优先
        if (_overrides.TryGetValue(packageName, out var overrideVersion)) versionSpec = overrideVersion;

        var resolvedVersion = await resolve_version_spec(packageName, versionSpec, registry);

        var key = $"{packageName}@{resolvedVersion}";

        if (!visited.Add(key))
            return new DependencyNode
            {
                package_name = packageName,
                version = resolvedVersion,
                registry_name = registryName
            };

        record_version_request(packageName, versionSpec, resolvedVersion);

        var package = await registry.get_package(packageName, resolvedVersion);
        _resolved_packages[packageName] = package;

        var node = new DependencyNode
        {
            package_name = package.name,
            version = package.version,
            registry_name = registryName,
            target_condition = targetCondition,
            is_sdk = package.is_sdk_package,
            sdk_module_name = package.sdk_module_name
        };

        if (package.dependencies is not null && package.dependencies.Count > 0)
            foreach (var dep in package.dependencies)
            {
                var parts = dep.Split('@');
                var depName = parts.Length > 1 ? parts[0] : dep;
                var depVersion = parts.Length > 1 ? parts[1] : "latest";

                var depNode = await resolve_recursive(depName, depVersion, registryName, registry, visited,
                    targetCondition);
                node.dependencies.Add(depNode);
            }

        if (package.dependency_versions is not null && package.dependency_versions.Count > 0)
            foreach (var dep in package.dependency_versions)
            {
                var depNode = await resolve_recursive(dep.Key, dep.Value, registryName, registry, visited,
                    targetCondition);
                node.dependencies.Add(depNode);
            }

        if (package.target_conditions.Count > 0)
            foreach (var condition in package.target_conditions)
            {
                if (!matches_target(condition.Key)) continue;

                foreach (var dep in condition.Value)
                {
                    var parts = dep.Split('@');
                    var depName = parts.Length > 1 ? parts[0] : dep;
                    var depVersion = parts.Length > 1 ? parts[1] : "latest";

                    var depNode = await resolve_recursive(depName, depVersion, registryName, registry, visited,
                        condition.Key);
                    node.dependencies.Add(depNode);
                }
            }

        if (package.is_sdk_package && !_auto_resolved_sdks.Contains(package.name))
            await auto_resolve_sdk_dependencies(package, registryName, registry, visited);

        return node;
    }

    /// <summary>
    ///     检查目标条件是否匹配当前编译目标
    /// </summary>
    private bool matches_target(string conditionKey)
    {
        var parts = conditionKey.Split('.');

        if (parts.Length < 2 || parts[0] != "target") return false;

        var targetValue = parts[1];

        return targetValue switch
        {
            "web" => _current_target.arch == "wasm",
            "wasip1" => _current_target.abi == "wasip1",
            "wasip2" => _current_target.abi == "wasip2",
            "jvm" => _current_target.arch == "jvm",
            "clr" => _current_target.arch == "clr",
            "native" => _current_target.arch == "native",
            "linux" => _current_target.os == "linux",
            "windows" => _current_target.os == "windows",
            "macos" => _current_target.os == "macos",
            _ => _current_target.arch == targetValue || _current_target.os == targetValue
        };
    }

    /// <summary>
    ///     自动解析 SDK 包的平台相关依赖
    /// </summary>
    private async Task auto_resolve_sdk_dependencies(
        PackageRegistry.Package sdkPackage,
        string registryName,
        IRegistry registry,
        HashSet<string> visited)
    {
        if (!_registries.TryGetValue(registryName, out var reg)) return;

        _auto_resolved_sdks.Add(sdkPackage.name);

        var sdkMap = new Dictionary<string, (string Arch, string? Os, string? Abi, string? Channel)>
        {
            ["std.adaptor.wasm"] = ("wasm", null, null, null),
            ["std.adaptor.wasip1"] = ("wasm", null, "wasip1", null),
            ["std.adaptor.wasip2"] = ("wasm", null, "wasip2", null),
            ["std.adaptor.dotnet"] = ("clr", null, null, null),
            ["std.adaptor.jvm"] = ("jvm", null, null, null),
            ["std.adaptor.windows"] = ("native", "windows", null, null),
            ["std.adaptor.linux"] = ("native", "linux", null, null),
            ["std.adaptor.macos"] = ("native", "macos", null, null)
        };

        if (sdkMap.TryGetValue(sdkPackage.name, out var sdkTarget))
        {
            var prevTarget = new TargetCondition
            {
                arch = _current_target.arch,
                os = _current_target.os,
                abi = _current_target.abi,
                channel = _current_target.channel
            };

            _current_target.arch = sdkTarget.Arch;
            _current_target.os = sdkTarget.Os;
            _current_target.abi = sdkTarget.Abi;
            _current_target.channel = sdkTarget.Channel;

            if (sdkPackage.dependencies is not null)
                foreach (var dep in sdkPackage.dependencies)
                {
                    var parts = dep.Split('@');
                    var depName = parts.Length > 1 ? parts[0] : dep;
                    var depVersion = parts.Length > 1 ? parts[1] : "latest";
                    await resolve_recursive(depName, depVersion, registryName, reg, visited, null);
                }

            _current_target.arch = prevTarget.arch;
            _current_target.os = prevTarget.os;
            _current_target.abi = prevTarget.abi;
            _current_target.channel = prevTarget.channel;
        }
    }

    public List<DependencyConflict> detect_conflicts()
    {
        var conflicts = new List<DependencyConflict>();

        foreach (var kvp in _version_requests)
            if (kvp.Value.Count > 1)
            {
                var conflict = new DependencyConflict
                {
                    package_name = kvp.Key,
                    requested_versions = [.. kvp.Value.Select(v => v.raw_spec).Distinct()]
                };

                if (_resolved_packages.TryGetValue(kvp.Key, out var package))
                {
                    conflict.resolved_version = package.version;
                    conflict.resolution_strategy = ConflictResolutionStrategy.highest_compatible;
                }

                if (_overrides.ContainsKey(kvp.Key))
                    conflict.resolution_strategy = ConflictResolutionStrategy.@override;
                else if (conflict.is_severe) conflict.resolution_strategy = ConflictResolutionStrategy.manual;

                conflicts.Add(conflict);
            }

        return conflicts;
    }

    /// <summary>
    ///     自动解决所有可解决的冲突，返回仍需手动解决的冲突列表
    /// </summary>
    /// <param name="conflicts">冲突列表</param>
    /// <returns>仍需手动解决的冲突</returns>
    public List<DependencyConflict> auto_resolve_conflicts(List<DependencyConflict> conflicts)
    {
        var unresolved = new List<DependencyConflict>();

        foreach (var conflict in conflicts)
        {
            if (conflict.resolution_strategy == ConflictResolutionStrategy.manual)
            {
                unresolved.Add(conflict);
                continue;
            }

            if (conflict is { resolution_strategy: ConflictResolutionStrategy.highest_compatible, resolved_version: not null })
            {
                Console.WriteLine($"  自动解决冲突：{conflict.package_name} → {conflict.resolved_version}");
            }
            else if (conflict.resolution_strategy == ConflictResolutionStrategy.@override)
            {
                var overrideVersion = _overrides[conflict.package_name];
                conflict.resolved_version = overrideVersion;
                Console.WriteLine($"  覆盖解决冲突：{conflict.package_name} → {overrideVersion}（override）");
            }
        }

        return unresolved;
    }

    public string print_dependency_tree(DependencyNode node, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        var output = $"{indentStr}{node.package_name}@{node.version}";

        foreach (var dep in node.dependencies) output += "\n" + print_dependency_tree(dep, indent + 1);

        return output;
    }

    /// <summary>
    ///     扁平化依赖树，对 SemVer 兼容范围内的版本自动去重
    /// </summary>
    public List<PackageRegistry.Package> get_flat_dependency_list(DependencyNode root)
    {
        var packages = new List<PackageRegistry.Package>();
        var dedupMap = new Dictionary<string, (PackageRegistry.Package Package, SemanticVersion Version)>();

        flatten_dependencies(root, dedupMap);

        return [.. dedupMap.Values.Select(v => v.Package)];
    }

    private void flatten_dependencies(DependencyNode node,
        Dictionary<string, (PackageRegistry.Package, SemanticVersion)> dedupMap)
    {
        var key = node.package_name;

        var nodeVersion = SemanticVersion.try_parse(node.version, out var sv) ? sv : null;

        if (dedupMap.TryGetValue(key, out var existing))
        {
            // 同一包已存在：选更高版本（SemVer 兼容范围内去重）
            if (nodeVersion is not null && existing.Item2 is not null &&
                nodeVersion.CompareTo(existing.Item2) > 0)
                if (_resolved_packages.TryGetValue(node.package_name, out var pkg))
                    dedupMap[key] = (pkg, nodeVersion);

            return;
        }

        if (_resolved_packages.TryGetValue(node.package_name, out var package))
            dedupMap[key] = (package, nodeVersion ?? new SemanticVersion(0, 0, 0));

        foreach (var dep in node.dependencies) flatten_dependencies(dep, dedupMap);
    }

    #region 版本请求记录

    private void record_version_request(string packageName, string rawSpec, string resolvedVersion)
    {
        if (!_version_requests.ContainsKey(packageName)) _version_requests[packageName] = [];

        var alreadyRecorded = _version_requests[packageName].Any(v => v.raw_spec == rawSpec);
        if (!alreadyRecorded)
            _version_requests[packageName].Add(new VersionRequest
            {
                raw_spec = rawSpec,
                resolved_version = resolvedVersion
            });
    }

    #endregion

    #region 版本范围解析

    /// <summary>
    ///     异步解析版本范围，对范围约束（^/~）查询注册表并选择最优版本
    /// </summary>
    private async Task<string> resolve_version_spec(string packageName, string versionSpec, IRegistry registry)
    {
        // workspace:* / workspace:^ → 工作区内部依赖
        if (versionSpec.StartsWith(_workspace_prefix, StringComparison.OrdinalIgnoreCase))
        {
            if (_workspace_members.TryGetValue(packageName,
                    out var memberPath))
                return versionSpec; // 保留 workspace: 前缀，后续由安装器处理

            throw new DependencyResolutionException(
                $"workspace 协议但未找到成员包 '{packageName}'。请在 voa.workspace.v 的 members 中添加此包。");
        }

        // latest / * / 精确 SemVer / 精确 YearlyVersion → 直接返回
        if (versionSpec is "latest" or "*") return versionSpec;

        if (SemanticVersion.try_parse(versionSpec, out _) || YearlyVersion.try_parse(versionSpec, out _))
            return versionSpec;

        // 版本范围 → 查询注册表，选择最优版本
        try
        {
            var range = VersionRange.parse(versionSpec);

            // 精确版本（无运算符，如 "1.2.3"）→ 直接使用
            if (range.min_version is not null && range.max_version is not null &&
                range is { min_inclusive: true, max_inclusive: true } &&
                range.min_version.Equals(range.max_version))
                return range.min_version.ToString();

            // 范围约束 → 查询可用版本列表
            var availableVersions = await registry.get_package_versions(packageName);
            if (availableVersions.Count == 0) return versionSpec;

            var bestVersion = find_best_version(packageName, versionSpec, availableVersions);
            return bestVersion ?? versionSpec;
        }
        catch (FormatException)
        {
            return versionSpec;
        }
        catch (RegistryException)
        {
            return versionSpec;
        }
    }

    /// <summary>
    ///     判断版本范围是否可被某个版本满足（同步版本，用于已解析后的检查）
    /// </summary>
    public bool is_version_satisfied(string versionSpec, string availableVersion)
    {
        if (versionSpec is "latest" or "*") return true;

        if (string.Equals(versionSpec, availableVersion, StringComparison.OrdinalIgnoreCase)) return true;

        if (SemanticVersion.try_parse(availableVersion, out var available) && available is not null)
            try
            {
                var range = VersionRange.parse(versionSpec);
                return range.satisfies(available);
            }
            catch (FormatException)
            {
                return false;
            }

        if (YearlyVersion.try_parse(availableVersion, out var yearlyAvailable) && yearlyAvailable is not null)
            try
            {
                var range = YearlyVersionRange.parse(versionSpec);
                return range.satisfies(yearlyAvailable);
            }
            catch (FormatException)
            {
                return false;
            }

        return false;
    }

    public string? find_best_version(string packageName, string versionSpec, List<string> availableVersions)
    {
        if (versionSpec is "latest" or "*")
            return availableVersions
                .Where(v => SemanticVersion.try_parse(v, out _))
                .Select(v => SemanticVersion.parse(v))
                .OrderByDescending(v => v)
                .FirstOrDefault()?.ToString();

        var matchingVersions = availableVersions
            .Where(v => is_version_satisfied(versionSpec, v))
            .ToList();

        if (matchingVersions.Count == 0) return null;

        return matchingVersions
            .Where(v => SemanticVersion.try_parse(v, out _))
            .Select(v => SemanticVersion.parse(v))
            .OrderByDescending(v => v)
            .FirstOrDefault()?.ToString() ?? matchingVersions.First();
    }

    #endregion
}