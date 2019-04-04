using Nyar.Language.Von;
using Nyar.Types.Targets;
using Std.Data.Text.Diagnostics;
using Std.DataProcess.Serialize;
using Std.Math.GraphTheory.Graph;
using Arch = Nyar.Types.Targets.TargetArch;
using Abi = Nyar.Types.Targets.TargetAbi;
using Os = Nyar.Types.Targets.TargetSpecification;

namespace Legion.CLI.Compiler;

/// <summary>
///     包图解析器，负责从项目清单出发构建源码闭包。
///     根据 CanonicalTarget 的四元组 (arch, impl, spec, abi) 匹配目标适配器，
///     替代旧的手工 <c>switch arch</c> + 目录拼接方式。
///     依赖关系使用 <see cref="PackageDependencyGraph"/> 存储，
///     循环检测委托给 <see cref="GraphAlgorithm.HasCycle{TNode,TEdge}"/>。
/// </summary>
public sealed class PackageGraph
{
    /// <summary>
    ///     Workspace 根目录的 legions.von 解析出的依赖路径映射。
    ///     key: 包名, value: 本地仓库根目录（如 std → .../std.v）。
    /// </summary>
    private Dictionary<string, string>? _dependency_path_map;

    /// <summary>
    ///     Workspace 级 auto_link 默认值（从 legions.von 的 workspace 段继承）。
    ///     当子项目未显式声明 auto_link 时使用此默认值。
    /// </summary>
    private (bool Core, bool Std)? _workspace_auto_link;

    /// <summary>
    ///     构建目标的源码闭包。
    /// </summary>
    /// <param name="projectDir">项目根目录</param>
    /// <param name="target">编译目标</param>
    /// <param name="verbose">是否为 verbose 模式</param>
    /// <returns>源文件路径列表，失败返回空列表并携带错误信息</returns>
    public PackageGraphResult resolve(string projectDir, CompilationTarget target, bool verbose, bool includeTests = false)
    {
        var files = new List<string>();
        var errors = new List<string>();
        var examplesRootDir = Path.GetDirectoryName(projectDir);
        if (string.IsNullOrEmpty(examplesRootDir))
        {
            errors.Add($"无法定位 examples 根目录：'{projectDir}'");
            return new PackageGraphResult(false, [], errors);
        }

        // 从 projectDir 向上查找 workspace 根目录，解析依赖路径映射和 workspace 配置
        var workspaceDir = find_workspace_root(projectDir);
        _dependency_path_map = build_dependency_path_map(workspaceDir);
        _workspace_auto_link = parse_workspace_auto_link(workspaceDir);

        var projectManifest = try_load_manifest(projectDir, errors);
        var projectName = get_package_name(projectDir, projectManifest);

        var dependencyGraph = new PackageDependencyGraph();

        var visitedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queuedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageQueue = new Queue<string>();

        foreach (var implicitDependency in get_implicit_dependencies(projectName, projectManifest, target))
        {
            dependencyGraph.AddEdge(new Edge<string>(projectName, implicitDependency));
            enqueue_dependency(implicitDependency, packageQueue, queuedPackages);
        }

        foreach (var dependency in enumerate_dependencies(projectManifest, target))
        {
            dependencyGraph.AddEdge(new Edge<string>(projectName, dependency));
            enqueue_dependency(dependency, packageQueue, queuedPackages);
        }

        while (packageQueue.Count > 0)
        {
            var packageName = packageQueue.Dequeue();
            if (!visitedPackages.Add(packageName))
            {
                continue;
            }

            var packageDir = resolve_package_dir(examplesRootDir, packageName);
            if (packageDir is null)
            {
                if (!is_optional_package(packageName))
                {
                    errors.Add($"无法解析依赖包：{packageName}");
                }

                continue;
            }

            var packageManifest = try_load_manifest(packageDir, errors);
            add_project_sources(packageDir, files, packageManifest, target);
            if (packageManifest is null)
            {
                continue;
            }

            foreach (var depKey in enumerate_dependencies(packageManifest, target))
            {
                dependencyGraph.AddEdge(new Edge<string>(packageName, depKey));
                enqueue_dependency(depKey, packageQueue, queuedPackages);
            }

            if (string.Equals(packageName, "std", StringComparison.OrdinalIgnoreCase))
            {
                var adaptor = resolve_adaptor_package(target);
                if (!string.IsNullOrWhiteSpace(adaptor))
                {
                    dependencyGraph.AddEdge(new Edge<string>(packageName, adaptor));
                    enqueue_dependency(adaptor, packageQueue, queuedPackages);
                }
            }
        }

        add_project_sources(projectDir, files, projectManifest, target, includeTests);

        // 使用图算法检测包依赖循环
        if (dependencyGraph.HasCycle())
        {
            var cycle = dependencyGraph.FindCycle();
            errors.Add($"检测到包依赖循环：{string.Join(" → ", cycle)}");
        }

        if (verbose)
        {
            Console.WriteLine($"[PackageGraph] 依赖图：{dependencyGraph.NodeCount} 个包，{dependencyGraph.EdgeCount} 条依赖边");

            foreach (var file in files.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[PackageGraph] source: {file}");
            }
        }

        return new PackageGraphResult(
            errors.Count == 0,
            [.. files.Distinct(StringComparer.OrdinalIgnoreCase)],
            errors);
    }

    /// <summary>
    ///     根据 CanonicalTarget 四元组匹配选择适配器包名。
    ///     匹配优先级：arch+impl+spec+abi → arch+impl+spec → arch+spec+abi → arch+spec → arch。
    /// </summary>
    /// <param name="target">编译目标</param>
    /// <returns>匹配的适配器包名，无匹配返回 null</returns>
    public string? resolve_adaptor_package(CompilationTarget target)
    {
        var arch = arch_to_string(target.arch);
        var impl = impl_to_string(target);
        var spec = spec_to_string(target);
        var abi = abi_to_string(target.abi);

        // 候选适配器列表，按优先级从高到低排列（前缀匹配）
        var candidates = new List<(string Name, int Score)>();

        // 根据 arch 类型选择适配器族
        var adaptorFamily = target.arch switch
        {
            Arch.clr => "std.adaptor.clr",
            Arch.jvm => "std.adaptor.jvm",
            Arch.wasm32 or Arch.wasm64 when target.abi == Abi.wasi_p1 => "std.adaptor.wasip1",
            Arch.wasm32 or Arch.wasm64 when target.abi == Abi.wasi_p2 => "std.adaptor.wasip2",
            Arch.wasm32 or Arch.wasm64 => "std.adaptor.wasm",
            Arch.nyar_vm => "std.adaptor.nyar",
            _ => null
        };

        if (adaptorFamily is null)
        {
            // 原生目标按 OS 匹配
            adaptorFamily = target.os switch
            {
                Os.windows => "std.adaptor.windows",
                Os.linux => "std.adaptor.linux",
                Os.mac_os => "std.adaptor.macos",
                _ => null
            };
        }

        return adaptorFamily;
    }

    #region 内部图类型

    /// <summary>
    ///     包依赖有向图，基于 <see cref="Graph{TNode,TEdge}"/> 存储依赖关系。
    ///     边方向为 A→B 表示包 A 依赖包 B。
    ///     循环检测和遍历委托给 <see cref="GraphAlgorithm"/> 扩展方法。
    /// </summary>
    private sealed class PackageDependencyGraph : Graph<string, Edge<string>>
    {
        /// <summary>
        ///     初始化包依赖图，使用不区分大小写的字符串比较器。
        /// </summary>
        public PackageDependencyGraph()
            : base(StringComparer.OrdinalIgnoreCase, EqualityComparer<Edge<string>>.Default)
        {
        }

        /// <inheritdoc />
        protected override string GetEdgeSource(Edge<string> edge)
        {
            return edge.Source;
        }

        /// <inheritdoc />
        protected override string GetEdgeTarget(Edge<string> edge)
        {
            return edge.Target;
        }
    }

    #endregion

    #region 私有辅助

    private static void add_project_sources(
        string projectDir,
        List<string> files,
        SerdeValue? manifest,
        CompilationTarget target,
        bool includeTests = false)
    {
        var excludedDirectories = get_excluded_paths(projectDir, manifest, target, "exclude_directories");
        var excludedFiles = get_excluded_paths(projectDir, manifest, target, "exclude_files");

        var sourceDir = Path.Combine(projectDir, "source");
        if (Directory.Exists(sourceDir))
        {
            files.AddRange(Directory.GetFiles(sourceDir, "*.v", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            files.AddRange(Directory.GetFiles(sourceDir, "*.ggs", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        var scriptDir = Path.Combine(projectDir, "script");
        if (Directory.Exists(scriptDir))
        {
            files.AddRange(Directory.GetFiles(scriptDir, "*.v", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            files.AddRange(Directory.GetFiles(scriptDir, "*.ggs", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }

        if (!includeTests)
        {
            return;
        }

        var testDir = Path.Combine(projectDir, "test");
        if (Directory.Exists(testDir))
        {
            files.AddRange(Directory.GetFiles(testDir, "*.v", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
            files.AddRange(Directory.GetFiles(testDir, "*.ggs", SearchOption.AllDirectories)
                .Where(path => !is_excluded_path(path, excludedDirectories, excludedFiles))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
        }
    }

    private static HashSet<string> get_excluded_paths(
        string projectDir,
        SerdeValue? manifest,
        CompilationTarget target,
        string fieldName)
    {
        var excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in enumerate_manifest_string_array(manifest, fieldName))
        {
            add_excluded_path(projectDir, relativePath, excludedPaths);
        }

        foreach (var buildEntry in enumerate_matching_build_entries(manifest, target))
        {
            foreach (var relativePath in enumerate_manifest_string_array(buildEntry, fieldName))
            {
                add_excluded_path(projectDir, relativePath, excludedPaths);
            }
        }

        return excludedPaths;
    }

    private static IEnumerable<SerdeValue> enumerate_matching_build_entries(SerdeValue? manifest, CompilationTarget target)
    {
        var buildField = manifest?.get_field("build");
        if (buildField is not { type: SerdeValueType.array, elements: not null })
        {
            yield break;
        }

        var canonicalTriple = build_canonical_triple(target);
        foreach (var entry in buildField.elements)
        {
            if (entry is not { type: SerdeValueType.@object, fields: not null })
            {
                continue;
            }

            var entryTarget = entry.get_field("target")?.get_string();
            if (string.Equals(entryTarget, canonicalTriple, StringComparison.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    private static IEnumerable<string> enumerate_manifest_string_array(SerdeValue? value, string fieldName)
    {
        var field = value?.get_field(fieldName);
        if (field is not { type: SerdeValueType.array, elements: not null })
        {
            yield break;
        }

        foreach (var element in field.elements)
        {
            var text = element.get_string();
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }

    private static void add_excluded_path(string projectDir, string relativePath, HashSet<string> excludedPaths)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Trim();
        if (string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            return;
        }

        excludedPaths.Add(Path.GetFullPath(Path.Combine(projectDir, normalizedRelativePath))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static bool is_excluded_path(
        string path,
        HashSet<string> excludedDirectories,
        HashSet<string> excludedFiles)
    {
        var fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (excludedFiles.Contains(fullPath))
        {
            return true;
        }

        foreach (var directory in excludedDirectories)
        {
            if (fullPath.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fullPath, directory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string build_canonical_triple(CompilationTarget target)
    {
        return $"{arch_to_string(target.arch)}-{impl_to_string(target)}-{spec_to_string(target)}-{abi_to_string(target.abi)}";
    }

    private static void enqueue_dependency(string packageName, Queue<string> queue, HashSet<string> queuedPackages)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return;
        }

        if (queuedPackages.Add(packageName))
        {
            queue.Enqueue(packageName);
        }
    }

    private static SerdeValue? try_load_manifest(string projectDir, List<string> errors)
    {
        var manifestPath = Path.Combine(projectDir, "legion.von");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(manifestPath);
            var manifest = parser.deserialize(source);
            if (diagnostics.has_errors)
            {
                errors.Add($"清单解析失败：{manifestPath}");
                return null;
            }

            return manifest;
        }
        catch (Exception ex)
        {
            errors.Add($"读取清单失败：{manifestPath}，{ex.Message}");
            return null;
        }
    }

    private static string get_package_name(string projectDir, SerdeValue? manifest)
    {
        return manifest?.get_field("name")?.get_string()
               ?? Path.GetFileName(projectDir);
    }

    private static bool is_optional_package(string packageName)
    {
        return string.Equals(packageName, "core", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(packageName, "std", StringComparison.OrdinalIgnoreCase) ||
               packageName.StartsWith("std.adaptor.", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<string> get_implicit_dependencies(string packageName, SerdeValue? manifest,
        CompilationTarget target)
    {
        // core 自身不自动链接任何包
        if (string.Equals(packageName, "core", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        // 仅当项目显式声明 auto_link，或 workspace 默认值实际启用了某个隐式依赖时，
        // 才走新 schema。这样旧 manifest 在 workspace `auto_link = false` 下
        // 仍然可以回退到 legacy `dependencies.core/std` 兼容逻辑。
        var projectAutoLink = try_get_auto_link(manifest);
        var autoLink = projectAutoLink;
        if (autoLink is null &&
            _workspace_auto_link is { Core: true } or { Std: true })
        {
            autoLink = _workspace_auto_link;
        }

        if (autoLink is not null)
        {
            if (autoLink.Value.Core)
            {
                yield return "core";
            }

            // std 自身和 std.adaptor.* 不自动链接 std
            if (string.Equals(packageName, "std", StringComparison.OrdinalIgnoreCase) ||
                packageName.StartsWith("std.adaptor.", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (autoLink.Value.Std)
            {
                yield return "std";
            }

            yield break;
        }

        // 回退：兼容旧 schema（dependencies 中的 core/std 布尔值）
        if (!is_explicitly_disabled(manifest, "core", target))
        {
            yield return "core";
        }

        if (string.Equals(packageName, "std", StringComparison.OrdinalIgnoreCase) ||
            packageName.StartsWith("std.adaptor.", StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        if (!is_explicitly_disabled(manifest, "std", target))
        {
            yield return "std";
        }
    }

    private static bool is_explicitly_disabled(SerdeValue? manifest, string packageName, CompilationTarget target)
    {
        var dependencyObject = manifest?.get_field("dependencies");
        if (dependencyObject?.fields is null)
        {
            return false;
        }

        foreach (var entry in dependencyObject.fields)
        {
            if (string.Equals(entry.Key, packageName, StringComparison.OrdinalIgnoreCase) &&
                !matches_target(entry.Value, target))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     从 manifest 的 auto_link 顶层字段读取自动链接控制标志。
    ///     若不存在 auto_link 则返回 null，由调用方回退到旧逻辑。
    /// </summary>
    private static (bool Core, bool Std)? try_get_auto_link(SerdeValue? manifest)
    {
        var autoLink = manifest?.get_field("auto_link");
        if (autoLink?.type != SerdeValueType.@object || autoLink.fields is null)
        {
            return null;
        }

        var core = autoLink.fields.TryGetValue("core", out var coreValue) && coreValue.get_boolean();
        var std = autoLink.fields.TryGetValue("std", out var stdValue) && stdValue.get_boolean();

        return (core, std);
    }

    /// <summary>
    ///     从 workspace legions.von 的 workspace 段解析 auto_link 默认值。
    /// </summary>
    /// <param name="workspaceDir">workspace 根目录</param>
    /// <returns>auto_link 默认值，若未配置则返回 null</returns>
    private (bool Core, bool Std)? parse_workspace_auto_link(string? workspaceDir)
    {
        if (string.IsNullOrWhiteSpace(workspaceDir))
        {
            return null;
        }

        var legionsPath = Path.Combine(workspaceDir, "legions.von");
        if (!File.Exists(legionsPath))
        {
            return null;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(legionsPath);
            var manifest = parser.deserialize(source);

            var workspaceField = manifest.get_field("workspace");
            if (workspaceField?.type != SerdeValueType.@object || workspaceField.fields is null)
            {
                return null;
            }

            return try_get_auto_link(workspaceField);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> enumerate_dependencies(SerdeValue? manifest, CompilationTarget target)
    {
        var dependencyObject = manifest?.get_field("dependencies");
        if (dependencyObject?.fields is null)
        {
            yield break;
        }

        foreach (var entry in dependencyObject.fields)
        {
            // 过滤掉 core/std 布尔控制标志（新 schema 中这些不应出现，兼容旧 schema）
            if (entry.Value.type == SerdeValueType.boolean)
            {
                continue;
            }

            if (matches_target(entry.Value, target))
            {
                yield return entry.Key;
            }
        }
    }

    private static bool matches_target(SerdeValue dependencyConfig, CompilationTarget target)
    {
        if (dependencyConfig.type == SerdeValueType.boolean)
        {
            return dependencyConfig.get_boolean();
        }

        if (dependencyConfig.type != SerdeValueType.@object)
        {
            return true;
        }

        var osValue = dependencyConfig.get_field("os");
        if (osValue is not null)
        {
            var osTarget = osValue.get_string()
                           ?? osValue.get_field("target")?.get_string();
            if (!string.IsNullOrWhiteSpace(osTarget) && !matches_os(osTarget, target))
            {
                return false;
            }
        }

        var cpuValue = dependencyConfig.get_field("cpu");
        if (cpuValue is not null)
        {
            var cpuArch = cpuValue.get_string()
                          ?? cpuValue.get_field("arch")?.get_string();
            if (!string.IsNullOrWhiteSpace(cpuArch) && !matches_arch(cpuArch, target))
            {
                return false;
            }
        }

        var abiValue = dependencyConfig.get_field("abi");
        if (abiValue is not null)
        {
            var abiName = abiValue.get_string();
            if (!string.IsNullOrWhiteSpace(abiName) && !matches_abi(abiName, target))
            {
                return false;
            }
        }

        return true;
    }

    private static bool matches_os(string os, CompilationTarget target)
    {
        return os.ToLowerInvariant() switch
        {
            "windows" => target.os == Os.windows,
            "linux" => target.os == Os.linux,
            "macos" or "darwin" => target.os == Os.mac_os,
            "wasm" or "browser" or "web" => target.os == Os.web,
            "wasip1" => target.abi == Abi.wasi_p1,
            "wasip2" => target.abi == Abi.wasi_p2,
            "jvm" => target.arch == Arch.jvm,
            "nyar" => target.arch == Arch.nyar_vm,
            _ => true
        };
    }

    private static bool matches_arch(string arch, CompilationTarget target)
    {
        return arch.ToLowerInvariant() switch
        {
            "clr" => target.arch == Arch.clr,
            "jvm" => target.arch == Arch.jvm,
            "wasm32" => target.arch == Arch.wasm32,
            "wasm64" => target.arch == Arch.wasm64,
            "x86_64" or "native" => target.arch is Arch.x86_64 or Arch.native,
            "aarch64" => target.arch == Arch.a_arch64,
            "nyar" => target.arch == Arch.nyar_vm,
            _ => true
        };
    }

    private static bool matches_abi(string abi, CompilationTarget target)
    {
        return abi.ToLowerInvariant() switch
        {
            "clr" or "managed" => target.abi == Abi.clr,
            "jvm" => target.abi == Abi.jvm,
            "wasm" or "web" or "browser" => target.abi == Abi.web_assembly,
            "wasip1" => target.abi == Abi.wasi_p1,
            "wasip2" => target.abi == Abi.wasi_p2,
            "nyar" => target.arch == Arch.nyar_vm,
            "native" => target.abi is Abi.system_v or Abi.microsoft_x64 or Abi.aapcs or Abi.aapcs64,
            _ => true
        };
    }

    private string? resolve_package_dir(string examplesRootDir, string packageName)
    {
        // 1. 尝试 side-by-side（在 examples 目录下）
        var candidate = Path.Combine(examplesRootDir, packageName);
        if (Directory.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        // 2. 尝试 workspace legions.von 中声明的依赖路径（path 字段）
        if (_dependency_path_map is not null)
        {
            // 优先精确匹配包名
            if (_dependency_path_map.TryGetValue(packageName, out var exactRepoRoot))
            {
                var pathCandidate = Path.Combine(exactRepoRoot, "projects", packageName);
                if (Directory.Exists(pathCandidate))
                {
                    return Path.GetFullPath(pathCandidate);
                }
            }

            // 再尝试所有已知 repo 路径（如 core 在 std 的 repo 中）
            foreach (var repoRoot in _dependency_path_map.Values.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var pathCandidate = Path.Combine(repoRoot, "projects", packageName);
                if (Directory.Exists(pathCandidate))
                {
                    return Path.GetFullPath(pathCandidate);
                }
            }
        }

        // 3. 硬编码回退（兼容未配置 path 的旧工作区）
        // 尝试常见的仓库名：std.v、valkyrie.v
        var repoRootDir = Path.GetFullPath(Path.Combine(examplesRootDir, "..", ".."));
        var repoNames = new[] { "std.v", "valkyrie.v" };
        foreach (var repoName in repoNames)
        {
            var stdCandidate = Path.Combine(repoRootDir, repoName, "projects", packageName);
            if (Directory.Exists(stdCandidate))
            {
                return Path.GetFullPath(stdCandidate);
            }
        }

        // 4. 最终回退：直接在 workspaceDir/projects 下查找
        var workspaceCandidate = Path.Combine(repoRootDir, "projects", packageName);
        if (Directory.Exists(workspaceCandidate))
        {
            return Path.GetFullPath(workspaceCandidate);
        }

        return null;
    }

    /// <summary>
    ///     从 projectDir 向上查找 workspace 根目录（含 legions.von 的目录）。
    /// </summary>
    private static string? find_workspace_root(string projectDir)
    {
        var current = Path.GetFullPath(projectDir);
        while (true)
        {
            if (File.Exists(Path.Combine(current, "legions.von")))
            {
                return current;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent is null || parent == current)
            {
                break;
            }

            current = parent;
        }

        return null;
    }

    /// <summary>
    ///     从 workspace legions.von 构建依赖包名到本地仓库根目录的映射。
    /// </summary>
    private static Dictionary<string, string> build_dependency_path_map(string? workspaceDir)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (workspaceDir is null)
        {
            return map;
        }

        var manifestPath = Path.Combine(workspaceDir, "legions.von");
        if (!File.Exists(manifestPath))
        {
            return map;
        }

        try
        {
            var diagnostics = new DiagnosticSink();
            var parser = new VonParser(diagnostics);
            var source = File.ReadAllText(manifestPath);
            var manifest = parser.deserialize(source);
            if (diagnostics.has_errors)
            {
                return map;
            }

            // 1. 读取 dependencies 对象中的 path 字段（旧 schema）
            var deps = manifest.get_field("dependencies");
            if (deps?.fields is not null)
            {
                foreach (var dep in deps.fields)
                {
                    // 只处理有 path 字段的对象型依赖声明
                    if (dep.Value.type != SerdeValueType.@object)
                    {
                        continue;
                    }

                    var path = dep.Value.get_field("path")?.get_string();
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    var repoRoot = Path.GetFullPath(Path.Combine(workspaceDir, path));
                    map[dep.Key] = repoRoot;
                }
            }

            // 2. 读取 members 数组（新 schema），推断包名→仓库根路径映射
            // members 格式如 "projects/std.adaptor.clr" 或 "asgard.v/projects/asgard"
            // 包名取最后一段路径，仓库根取第一个路径段
            var members = manifest.get_field("members");
            if (members is not { type: SerdeValueType.array, elements: not null })
            {
                return map;
            }

            foreach (var member in members.elements)
            {
                var memberPath = member.get_string();
                if (string.IsNullOrWhiteSpace(memberPath))
                {
                    continue;
                }

                // 标准化路径分隔符
                memberPath = memberPath.Replace('\\', '/');

                // 取最后一段作为包名（如 "projects/std.adaptor.clr" → "std.adaptor.clr"）
                var lastSlash = memberPath.LastIndexOf('/');
                if (lastSlash < 0)
                {
                    continue;
                }

                var packageName = memberPath[(lastSlash + 1)..];

                // 仓库根路径：第一个路径段（如 "projects/std.adaptor.clr" → workspaceDir；
                //              "asgard.v/projects/asgard" → workspaceDir/asgard.v）
                var firstSlash = memberPath.IndexOf('/');
                if (firstSlash < 0)
                {
                    continue;
                }

                var firstSegment = memberPath[..firstSlash];
                string repoRoot;
                if (string.Equals(firstSegment, "projects", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(firstSegment, "examples", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(firstSegment, "tests", StringComparison.OrdinalIgnoreCase))
                {
                    // 路径以 workspace 子目录开头，仓库根就是 workspaceDir
                    repoRoot = workspaceDir;
                }
                else
                {
                    // 路径以子仓库开头（如 "asgard.v"），仓库根是 workspaceDir/firstSegment
                    repoRoot = Path.GetFullPath(Path.Combine(workspaceDir, firstSegment));
                }

                // 只在不存在精确映射时填充（dependencies 中的 path 优先）
                map.TryAdd(packageName, repoRoot);
            }
        }
        catch
        {
            // 忽略 workspace 清单解析错误，继续使用回退路径
        }

        return map;
    }

    private static string arch_to_string(Arch arch)
    {
        return arch switch
        {
            Arch.nyar_vm => "nyar",
            Arch.clr => "clr",
            Arch.jvm => "jvm",
            Arch.wasm32 => "wasm32",
            Arch.wasm64 => "wasm64",
            Arch.x86_64 => "x86_64",
            Arch.a_arch64 => "aarch64",
            Arch.native => "x86_64",
            _ => string.Empty
        };
    }

    private static string impl_to_string(CompilationTarget target)
    {
        return target.vendor.to_triple_string();
    }

    private static string spec_to_string(CompilationTarget target)
    {
        return target.os switch
        {
            Os.windows => "windows",
            Os.linux => "linux",
            Os.mac_os => "darwin",
            Os.web => "browser",
            Os.android => "android",
            Os.ios => "ios",
            _ => "unknown"
        };
    }

    private static string abi_to_string(Abi abi)
    {
        return abi switch
        {
            Abi.clr or Abi.jvm => "managed",
            Abi.wasi_p1 => "wasip1",
            Abi.wasi_p2 => "wasip2",
            Abi.web_assembly => "wasm",
            Abi.microsoft_x64 => "msvc",
            Abi.system_v => "gnu",
            _ => "native"
        };
    }

    #endregion
}
