using System.Collections.Concurrent;
using System.Reflection;
using Nyar.PackageManager.Auth;
using Nyar.PackageManager.Config;
using Nyar.PackageManager.Dependency;
using Nyar.PackageManager.Package;
using Nyar.PackageManager.Scripts;
using Nyar.PackageManager.Security;
using Nyar.PackageManager.Tools;
using Nyar.PackageManager.Version;
using Nyar.PackageManager.Workspace;
using Nyar.PackageRegistry;
using Nyar.PackageRegistry.Conda;
using Nyar.PackageRegistry.Jsr;
using Nyar.PackageRegistry.Maven;
using Nyar.PackageRegistry.Npm;
using Nyar.PackageRegistry.Nuget;

namespace Nyar.PackageManager;

public class Legion
{
    #region 构造函数

    /// <summary>
    ///     创建 Legion 实例
    /// </summary>
    /// <param name="parse">配置文本解析器</param>
    public Legion(SerdeParser parse)
    {
        _parse = parse;
        base_directory = resolve_base_directory();
        vendors_directory = resolve_vendors_directory();

        if (!Directory.Exists(vendors_directory)) Directory.CreateDirectory(vendors_directory);

        config = new LegionConfig(_parse);
        config.load();

        cache = new PackageCache(base_directory, _parse);
        lock_file = new LockFile(base_directory, _parse);
        source_manager = new RegistrySourceManager(base_directory, _parse);
        _auth_store = new VendorAuthStore(base_directory, _parse);
        _auth_store.load();

        security_audit = new SecurityAudit();
        script_runner = new ScriptRunner(base_directory);

        register_default_registries();

        vendor_manager = new VendorManager(source_manager, _auth_store, _registries);
        publisher = new PackagePublisher(_registries, _parse);

        load_project_files();

        voa_config = new VoaConfig(base_directory, _parse);
        if (voa_config.exists()) voa_config.load();

        if (lock_file.exists()) lock_file.load();
    }

    #endregion

    #region 包搜索

    public async Task<List<PackageInfo>> search(string query, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
            throw new ArgumentException($"注册器 {registryName} 未找到");

        var packages = await registry.search_packages(query);
        Console.WriteLine($"在 {registryName} 中找到 {packages.Count} 个匹配 '{query}' 的包");
        return to_package_info_list(packages);
    }

    #endregion

    #region 字段

    private readonly SerdeParser _parse;
    private readonly Dictionary<string, IRegistry> _registries = new();
    private readonly VendorAuthStore _auth_store;

    #endregion

    #region 属性

    public LegionConfig config { get; }

    public PackageCache cache { get; }

    public LockFile lock_file { get; }

    public RegistrySourceManager source_manager { get; }

    public SecurityAudit security_audit { get; }

    public PackagePublisher publisher { get; }

    public ScriptRunner script_runner { get; }

    public VendorManager vendor_manager { get; }

    public string vendors_directory { get; }

    public string base_directory { get; }

    public LegionManifest? manifest { get; private set; }

    public LegionsWorkspace? workspace { get; private set; }

    public VoaConfig? voa_config { get; private set; }

    public LegionIgnore? ignore { get; private set; }

    public LegionConfigDirectory? config_dir { get; private set; }

    public bool is_workspace => workspace is not null;
    public bool has_manifest => manifest is not null;
    public bool is_standalone => !is_workspace && !has_manifest;

    /// <summary>
    ///     是否为 CI 环境（禁用交互式提示）
    /// </summary>
    public bool is_ci_mode { get; set; }

    /// <summary>
    ///     是否冻结锁文件（锁文件不匹配时直接报错，不更新）
    /// </summary>
    public bool is_frozen_lockfile { get; set; }

    /// <summary>
    ///     是否为 CI 环境（别名，兼容现有约定）
    /// </summary>
    public bool no_interactive => is_ci_mode;

    #endregion

    #region 项目文件加载

    private void load_project_files()
    {
        // 优先检测工作区
        var workspacePath = Path.Combine(base_directory, "voa.workspace.v");
        if (File.Exists(workspacePath))
            try
            {
                workspace = new LegionsWorkspace(base_directory, _parse);
                workspace.load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载工作区失败: {ex.Message}");
            }

        // 检测包清单
        var manifestPath = Path.Combine(base_directory, "legion.von");
        if (File.Exists(manifestPath))
            try
            {
                manifest = new LegionManifest(base_directory, _parse);
                manifest.load();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载清单失败: {ex.Message}");
            }

        // 加载 ignore 文件
        ignore = new LegionIgnore(base_directory);

        // 加载配置目录
        config_dir = new LegionConfigDirectory(base_directory);
    }

    public void reload_manifest()
    {
        load_project_files();
    }

    #endregion

    #region 注册器管理

    public void register_registry(IRegistry registry)
    {
        _registries[registry.name] = registry;
    }

    public void register_registry_endpoint(string registryName, string endpointUrl)
    {
        var registry = registryName switch
        {
            "npm" => new NpmRegistry { endpoint = endpointUrl },
            "jsr" => new JsrRegistry { endpoint = endpointUrl },
            "conda" => new CondaRegistry { endpoint = endpointUrl },
            "maven" => new MavenRegistry { endpoint = endpointUrl },
            "nuget" => new NuGetRegistry { endpoint = endpointUrl },
            "valhalla" => create_valhalla_registry(endpointUrl),
            _ => throw new ArgumentException($"不支持的注册器类型: {registryName}")
        };

        register_registry(registry);
        Console.WriteLine($"已注册注册器 {registryName}，端点 {endpointUrl}");
    }

    public IRegistry? get_registry(string registryName)
    {
        return _registries.GetValueOrDefault(registryName);
    }

    public Dictionary<string, IRegistry> get_all_registries()
    {
        return new Dictionary<string, IRegistry>(_registries);
    }

    public void remove_registry(string registryName)
    {
        if (_registries.Remove(registryName))
            Console.WriteLine($"已移除注册表：{registryName}");
        else
            Console.WriteLine($"注册表 '{registryName}' 不存在");

        source_manager.remove_source(registryName);
    }

    public string get_registry_endpoint(string registryName)
    {
        return _registries.TryGetValue(registryName, out var registry)
            ? registry.endpoint
            : string.Empty;
    }

    public async Task<List<PackageInfo>> search_packages(string query, string? registryName = null)
    {
        var results = new List<PackageInfo>();

        if (registryName is not null)
        {
            if (_registries.TryGetValue(registryName, out var registry))
                return to_package_info_list(await registry.search_packages(query));

            return results;
        }

        foreach (var registry in _registries.Values)
            results.AddRange(to_package_info_list(await registry.search_packages(query)));

        return results;
    }

    #endregion

    #region 包安装

    public async Task<PackageInfo> install(string packageName, string version = "latest",
        string registryName = "npm")
    {
        if (version.StartsWith("workspace:", StringComparison.OrdinalIgnoreCase))
            return await install_workspace_package(packageName, version);

        if (!_registries.TryGetValue(registryName, out var registry))
            throw new ArgumentException($"注册器 {registryName} 未找到");

        if (config.offline_mode)
        {
            if (cache.has_package(packageName, version))
            {
                Console.WriteLine($"[离线模式] 从缓存安装 {packageName}@{version}");
                return new PackageInfo { name = packageName, version = version };
            }

            throw new InvalidOperationException($"离线模式下缓存中没有 {packageName}@{version}");
        }

        var package = await registry.get_package(packageName, version);
        Console.WriteLine($"正在安装 {package.name}@{package.version}（来源: {registryName}）");

        var registryEndpoint = resolve_registry_endpoint(registryName);
        var orgName = extract_org_name(packageName);
        var packagePath = build_package_path(registryName, registryEndpoint, orgName, package.name, package.version);

        Directory.CreateDirectory(packagePath);

        try
        {
            Console.WriteLine($"正在下载 {package.name}@{package.version}...");
            var extractedPath = await registry.download_package(package, packagePath);
            Console.WriteLine($"下载完成：{extractedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败：{ex.Message}，仅记录元数据");
        }

        cache.add_package(package.name, package.version, packagePath);
        lock_file.add_package(package, registryName, registry.endpoint);
        lock_file.save();

        // 更新 legion.von 中的依赖
        if (manifest is not null)
        {
            manifest.add_dependency(packageName, package.version);
            manifest.save();
        }

        Console.WriteLine($"已安装到: {packagePath}");
        return to_package_info(package);
    }

    /// <summary>
    ///     安装工作区内部依赖（创建软链接而非下载）
    /// </summary>
    /// <param name="packageName">包名称</param>
    /// <param name="versionSpec">版本规范，如 workspace:* 或 workspace:^1.0.0</param>
    /// <returns>包元数据</returns>
    private async Task<PackageInfo> install_workspace_package(string packageName, string versionSpec)
    {
        var workspaceMembers = get_workspace_member_map();

        if (!workspaceMembers.TryGetValue(packageName, out var memberPath))
            throw new InvalidOperationException(
                $"工作区中未找到成员包 '{packageName}'。" +
                " 请在 voa.workspace.v 的 members 中添加此包。");

        var memberManifest = new LegionManifest(memberPath, _parse);
        if (!memberManifest.exists())
            throw new InvalidOperationException($"成员包 '{packageName}' 目录中未找到 legion.von：{memberPath}");

        memberManifest.load();

        var linkPath = Path.Combine(vendors_directory, packageName);
        var sourcePath = Path.GetFullPath(memberPath);

        if (Directory.Exists(linkPath)) Directory.Delete(linkPath, true);

        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);

        try
        {
            Directory.CreateSymbolicLink(linkPath, sourcePath);
            Console.WriteLine($"已创建符号链接：{linkPath} → {sourcePath}");
        }
        catch (UnauthorizedAccessException)
        {
            copy_directory(sourcePath, linkPath);
            Console.WriteLine($"已复制（无符号链接权限）：{sourcePath} → {linkPath}");
        }

        var package = new PackageInfo
        {
            name = memberManifest.name,
            version = memberManifest.version,
            dependencies = [.. memberManifest.dependencies.Keys],
            dependency_versions = memberManifest.dependencies
        };

        lock_file.packages[$"{packageName}@{memberManifest.version}"] = new LockEntry
        {
            name = packageName,
            version = memberManifest.version,
            registry = "workspace",
            resolved = sourcePath,
            integrity = string.Empty,
            is_workspace = true,
            install_path = packageName
        };
        lock_file.save();

        return package;
    }

    /// <summary>
    ///     递归复制目录
    /// </summary>
    private static void copy_directory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destDir, fileName), true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            copy_directory(dir, Path.Combine(destDir, dirName));
        }
    }

    public async Task<List<PackageInfo>> install(string type = "dependencies")
    {
        if (is_workspace)
        {
            Console.WriteLine("[Workspace 模式] 安装所有工作区成员的依赖");
            return await install_workspace(type);
        }

        if (has_manifest)
        {
            Console.WriteLine("[Package 模式] 安装当前包的依赖");
            return await install_package(type);
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法安装依赖");
    }

    private async Task<List<PackageInfo>> install_workspace(string type)
    {
        var installed = new List<PackageInfo>();

        if (workspace is null) return installed;

        // 安装工作区级依赖
        var workspaceDeps = type switch
        {
            "dev" or "devDependencies" => workspace.dev_dependencies,
            _ => workspace.dependencies
        };

        if (workspaceDeps.Count > 0)
        {
            var workspaceInstalled = await install_dependencies(workspaceDeps);
            installed.AddRange(workspaceInstalled);
        }

        // 安装各成员包的依赖
        var manifests = workspace.get_member_manifests();
        foreach (var manifest in manifests)
        {
            Console.WriteLine($"  安装成员包: {manifest.name}");
            var memberDeps = type switch
            {
                "dev" or "devDependencies" => manifest.dev_dependencies,
                "peer" or "peerDependencies" => manifest.peer_dependencies,
                "optional" or "optionalDependencies" => manifest.optional_dependencies,
                _ => manifest.dependencies
            };

            if (memberDeps.Count > 0)
            {
                var memberInstalled = await install_dependencies(memberDeps);
                installed.AddRange(memberInstalled);
            }
        }

        await run_hook_if_present(LegionManifest.HookNames.post_install);

        return installed;
    }

    private async Task<List<PackageInfo>> install_package(string type)
    {
        if (manifest is null) throw new InvalidOperationException("当前目录没有 legion.von");

        await run_hook_if_present(LegionManifest.HookNames.pre_install);

        var deps = type switch
        {
            "dev" or "devDependencies" => manifest.dev_dependencies,
            "peer" or "peerDependencies" => manifest.peer_dependencies,
            "optional" or "optionalDependencies" => manifest.optional_dependencies,
            _ => manifest.dependencies
        };

        var result = await install_dependencies(deps);

        await run_hook_if_present(LegionManifest.HookNames.post_install);

        return result;
    }

    /// <summary>
    ///     如果指定名称的钩子存在则执行，否则静默跳过
    /// </summary>
    private async Task run_hook_if_present(string hookName)
    {
        if (manifest?.has_hook(hookName) == true)
        {
            var result = await run_hook(hookName);
            if (!result.success) Console.WriteLine($"钩子 '{hookName}' 执行失败：{result.error}");
        }
    }

    public async Task<List<PackageInfo>> install_dependencies(Dictionary<string, string> dependencies,
        string registryName = "npm")
    {
        var overrides = manifest?.overrides ?? new Dictionary<string, string>();
        var workspaceMembers = get_workspace_member_map();
        var resolver = new DependencyResolver(_registries, overrides, workspaceMembers);

        if (overrides.Count > 0)
            Console.WriteLine(
                $"应用 {overrides.Count} 个版本覆盖：{string.Join(", ", overrides.Select(kv => $"{kv.Key}→{kv.Value}"))}");

        if (workspaceMembers.Count > 0) Console.WriteLine($"工作区成员：{workspaceMembers.Count} 个包");

        var rootNodes = await resolver.resolve_all(dependencies, registryName);

        var packagesToInstall = new List<PackageRegistry.Package>();
        var lockedPackages = new List<PackageRegistry.Package>();

        foreach (var node in rootNodes)
        {
            var flatList = resolver.get_flat_dependency_list(node);
            foreach (var package in flatList)
                if (!lock_file.is_package_locked(package.name, package.version))
                {
                    if (is_frozen_lockfile)
                        throw new InvalidOperationException(
                            $"锁文件已冻结：{package.name}@{package.version} 不在锁文件中。" +
                            " 请运行 'legion install' 更新锁文件后重试。");

                    packagesToInstall.Add(package);
                }
                else
                {
                    if (!is_ci_mode) Console.WriteLine($"已锁定: {package.name}@{package.version}，跳过安装");

                    lockedPackages.Add(package);
                }
        }

        var installed = new List<PackageInfo>(lockedPackages.Select(to_package_info));

        if (packagesToInstall.Count > 0)
        {
            Console.WriteLine($"并行安装 {packagesToInstall.Count} 个包...");
            var parallelResults = await install_packages_parallel(
                [.. packagesToInstall.Select(to_package_info)], registryName);
            installed.AddRange(parallelResults);
        }

        var conflicts = resolver.detect_conflicts();
        if (conflicts.Count > 0)
        {
            Console.WriteLine("\n检测到版本冲突:");
            foreach (var conflict in conflicts)
                Console.WriteLine(
                    $"  {conflict.package_name}: 请求版本 [{string.Join(", ", conflict.requested_versions)}]");
        }

        await check_peer_dependencies(installed);

        return installed;
    }

    /// <summary>
    ///     并行安装多个包，使用信号量控制并发度
    /// </summary>
    /// <param name="packages">待安装的包列表</param>
    /// <param name="registryName">注册表名称</param>
    /// <param name="maxParallelism">最大并行度，默认 8</param>
    private async Task<List<PackageInfo>> install_packages_parallel(
        List<PackageInfo> packages, string registryName, int maxParallelism = 8)
    {
        var results = new ConcurrentBag<PackageInfo>();
        var semaphore = new SemaphoreSlim(maxParallelism);

        var tasks = packages.Select(async package =>
        {
            await semaphore.WaitAsync();
            try
            {
                var installedPackage = await install(package.name, package.version, registryName);
                results.Add(installedPackage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安装失败: {package.name}@{package.version} - {ex.Message}");
                results.Add(package);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return [.. results];
    }

    /// <summary>
    ///     获取工作区成员映射：包名 → 目录路径
    /// </summary>
    private Dictionary<string, string> get_workspace_member_map()
    {
        var map = new Dictionary<string, string>();

        if (workspace is null) return map;

        foreach (var memberPath in workspace.members)
        {
            var fullPath = Path.Combine(base_directory, memberPath);
            if (!Directory.Exists(fullPath)) continue;

            var memberManifest = new LegionManifest(fullPath, _parse);
            if (memberManifest.exists())
            {
                memberManifest.load();
                if (!string.IsNullOrEmpty(memberManifest.name)) map[memberManifest.name] = fullPath;
            }
        }

        return map;
    }

    /// <summary>
    ///     检查所有已安装包的同伴依赖是否满足
    /// </summary>
    private async Task check_peer_dependencies(List<PackageInfo> installed)
    {
        var installedNames = new HashSet<string>(installed.Select(p => p.name));
        var missingPeers = new List<string>();

        foreach (var package in installed)
        {
            if (package.peer_dependencies is null || package.peer_dependencies.Count == 0) continue;

            foreach (var (peerName, peerVersionSpec) in package.peer_dependencies)
                if (!installedNames.Contains(peerName))
                    missingPeers.Add($"  {package.name}@{package.version} 需要 {peerName}@{peerVersionSpec}");
        }

        if (missingPeers.Count > 0)
        {
            Console.WriteLine("\n⚠ peerDependencies 警告：");
            foreach (var warning in missingPeers) Console.WriteLine(warning);

            Console.WriteLine("请手动安装以上同伴依赖");
        }
    }

    #endregion

    #region 包卸载

    public async Task<PackageInfo> uninstall(string packageName)
    {
        Console.WriteLine($"正在卸载 {packageName}");

        var lockedPackage = lock_file.get_package(packageName);
        if (lockedPackage is not null)
        {
            var registryEndpoint = resolve_registry_endpoint(lockedPackage.registry);
            var orgName = extract_org_name(packageName);
            var packagePath = build_package_path(lockedPackage.registry, registryEndpoint, orgName, lockedPackage.name,
                lockedPackage.version);

            if (Directory.Exists(packagePath)) Directory.Delete(packagePath, true);

            lock_file.remove_package(packageName);
            lock_file.save();
        }

        await cache.remove_package(packageName, lockedPackage?.version ?? "0.0.0");

        // 更新 legion.von 中的依赖
        if (manifest is not null)
        {
            manifest.remove_dependency(packageName);
            manifest.save();
        }

        return new PackageInfo { name = packageName, version = "0.0.0" };
    }

    /// <summary>
    ///     添加依赖到 legion.von 并安装
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="version">版本约束</param>
    /// <param name="registryName">注册表名</param>
    /// <param name="isDev">是否为开发依赖</param>
    public async Task<PackageInfo> add_dependency(string packageName, string version = "latest",
        string registryName = "npm", bool isDev = false)
    {
        var installedPackage = await install(packageName, version, registryName);

        if (manifest is not null)
        {
            var depType = isDev ? "dev" : "dependencies";
            var versionConstraint = version == "latest" ? $"^{installedPackage.version}" : version;
            manifest.add_dependency(packageName, versionConstraint, depType);
            manifest.save();

            Console.WriteLine($"已添加 {packageName}@{versionConstraint} 到 legion.von [{depType}]");
        }

        return installedPackage;
    }

    /// <summary>
    ///     从 legion.von 移除依赖并卸载
    /// </summary>
    /// <param name="packageName">包名</param>
    public async Task<PackageInfo> remove_dependency(string packageName)
    {
        var removed = await uninstall(packageName);

        if (manifest is not null)
        {
            manifest.remove_dependency(packageName);
            manifest.save();

            Console.WriteLine($"已从 legion.von 移除 {packageName}");
        }

        return removed;
    }

    #endregion

    #region 包更新

    public async Task<PackageInfo> update(string packageName, string version = "latest",
        string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
            throw new ArgumentException($"注册器 {registryName} 未找到");

        var package = await registry.get_package(packageName, version);
        Console.WriteLine($"正在更新 {package.name} 到 {package.version}（来源: {registryName}）");

        var lockedPackage = lock_file.get_package(packageName);
        if (lockedPackage is not null)
        {
            var oldRegistryEndpoint = resolve_registry_endpoint(lockedPackage.registry);
            var oldOrgName = extract_org_name(packageName);
            var oldPackagePath = build_package_path(lockedPackage.registry, oldRegistryEndpoint, oldOrgName,
                lockedPackage.name, lockedPackage.version);

            if (Directory.Exists(oldPackagePath)) Directory.Delete(oldPackagePath, true);
        }

        var registryEndpoint = resolve_registry_endpoint(registryName);
        var orgName = extract_org_name(packageName);
        var packagePath = build_package_path(registryName, registryEndpoint, orgName, package.name, package.version);

        Directory.CreateDirectory(packagePath);

        lock_file.remove_package(packageName);
        lock_file.add_package(package, registryName, registry.endpoint);
        lock_file.save();

        // 更新 legion.von 中的依赖
        if (manifest is not null)
        {
            manifest.add_dependency(packageName, package.version);
            manifest.save();
        }

        Console.WriteLine($"已更新到: {packagePath}");
        return package;
    }

    public async Task<List<PackageInfo>> update()
    {
        if (is_workspace)
        {
            Console.WriteLine("[Workspace 模式] 更新所有工作区成员的依赖");
            return await update_workspace();
        }

        if (has_manifest)
        {
            Console.WriteLine("[Package 模式] 更新当前包的依赖");
            return await update_package();
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法更新依赖");
    }

    private async Task<List<PackageInfo>> update_workspace()
    {
        var updated = new List<PackageInfo>();

        if (workspace is null) return updated;

        // 更新工作区级依赖
        foreach (var dep in workspace.dependencies)
            try
            {
                var package = await update(dep.Key);
                updated.Add(package);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新工作区依赖 {dep.Key} 失败: {ex.Message}");
            }

        // 更新各成员包的依赖
        var manifests = workspace.get_member_manifests();
        foreach (var manifest in manifests)
        {
            Console.WriteLine($"  更新成员包: {manifest.name}");
            foreach (var dep in manifest.dependencies)
                try
                {
                    var package = await update(dep.Key);
                    updated.Add(package);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  更新 {dep.Key} 失败: {ex.Message}");
                }
        }

        return updated;
    }

    private async Task<List<PackageInfo>> update_package()
    {
        var updated = new List<PackageInfo>();
        var lockedPackages = lock_file.get_all_packages();

        foreach (var locked in lockedPackages)
            try
            {
                var package = await update(locked.name, "latest", locked.registry);
                updated.Add(package);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新 {locked.name} 失败: {ex.Message}");
            }

        return updated;
    }

    #endregion

    #region 依赖解析

    public async Task<DependencyNode> resolve_dependencies(string packageName, string version,
        string registryName = "npm")
    {
        var resolver = new DependencyResolver(_registries);
        return await resolver.resolve(packageName, version, registryName);
    }

    public string print_dependency_tree(DependencyNode node)
    {
        var resolver = new DependencyResolver(_registries);
        return resolver.print_dependency_tree(node);
    }

    #endregion

    #region 脚本执行

    public async Task<ScriptResult> run(string scriptName)
    {
        if (is_standalone)
        {
            // Script 模式：尝试直接执行 vcc 命令
            Console.WriteLine("[Script 模式] 直接执行脚本");
            return await script_runner.run(scriptName);
        }

        if (workspace is not null && workspace.has_script(scriptName))
        {
            Console.WriteLine("[Workspace 模式] 执行工作区脚本");
            return await script_runner.run_script(workspace, scriptName);
        }

        if (manifest is not null && manifest.has_script(scriptName))
        {
            Console.WriteLine("[Package 模式] 执行包脚本");
            return await script_runner.run_script(manifest, scriptName);
        }

        return new ScriptResult
        {
            success = false,
            exit_code = -1,
            error = $"脚本 '{scriptName}' 未在 legion.von 或 voa.workspace.v 中定义"
        };
    }

    public async Task<ScriptResult> run_script(string scriptName)
    {
        return await run(scriptName);
    }

    /// <summary>
    ///     执行生命周期钩子，支持条件判断和 Shell 选择
    /// </summary>
    /// <param name="hookName">钩子名称</param>
    public async Task<ScriptResult> run_hook(string hookName)
    {
        var hook = manifest?.get_hook(hookName);
        if (hook is null)
            return new ScriptResult
            {
                success = true,
                exit_code = 0,
                output = $"钩子 '{hookName}' 未定义，跳过"
            };

        if (!evaluate_condition(hook.condition))
            return new ScriptResult
            {
                success = true,
                exit_code = 0,
                output = $"钩子 '{hookName}' 条件不满足（{hook.condition}），跳过"
            };

        Console.WriteLine($"> 执行钩子: {hookName}" +
                          (hook.description is not null ? $" — {hook.description}" : ""));

        var env = new Dictionary<string, string>
        {
            ["LEGION_HOOK_NAME"] = hookName,
            ["LEGION_HOOK_SHELL"] = hook.shell ?? string.Empty
        };

        var runner = hook.shell is not null
            ? new ScriptRunner(base_directory, env)
            : script_runner;

        var result = await runner.run(hook.command);

        if (!result.success && hook.fail_on_error)
            Console.WriteLine($"钩子 '{hookName}' 执行失败（FailOnError=true）：{result.error}");

        return result;
    }

    /// <summary>
    ///     评估平台条件表达式
    /// </summary>
    /// <param name="condition">条件表达式，如 "windows"、"!linux"、"macos"</param>
    private static bool evaluate_condition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        var cond = condition.Trim();

        if (cond.StartsWith('!'))
        {
            var negated = cond[1..];
            return !evaluate_platform_condition(negated);
        }

        return evaluate_platform_condition(cond);
    }

    private static bool evaluate_platform_condition(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "windows" or "win" => OperatingSystem.IsWindows(),
            "linux" or "unix" => OperatingSystem.IsLinux(),
            "macos" or "mac" or "osx" => OperatingSystem.IsMacOS(),
            _ => true
        };
    }

    /// <summary>
    ///     获取当前包中所有标记为 [main] 的 micro 函数
    /// </summary>
    public Dictionary<string, (string FilePath, string Line)> get_micro_functions()
    {
        if (manifest is null) return new Dictionary<string, (string, string)>();

        return manifest.get_micro_functions();
    }

    public List<string> list_scripts()
    {
        var scripts = new List<string>();

        if (workspace is not null) scripts.AddRange(script_runner.list_scripts(workspace));

        if (manifest is not null)
            foreach (var script in script_runner.list_scripts(manifest))
                if (!scripts.Contains(script))
                    scripts.Add(script);

        return scripts;
    }

    #endregion

    #region 安全审计

    public async Task<SecurityAuditResult> audit()
    {
        if (is_workspace)
        {
            Console.WriteLine("[Workspace 模式] 审计所有工作区成员");
            return await audit_workspace();
        }

        if (has_manifest)
        {
            Console.WriteLine("[Package 模式] 审计当前包");
            return await audit_package();
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法审计");
    }

    private async Task<SecurityAuditResult> audit_workspace()
    {
        var result = new SecurityAuditResult();

        if (workspace is null) return result;

        // 审计工作区级依赖
        var workspacePackages = new List<PackageInfo>();
        foreach (var dep in workspace.dependencies)
            workspacePackages.Add(new PackageInfo { name = dep.Key, version = dep.Value });

        var workspaceResult = await security_audit.audit_dependencies(
            [.. workspacePackages.Select(p => p.to_registry_package())]);
        result.vulnerabilities.AddRange(workspaceResult.vulnerabilities);
        result.licenses.AddRange(workspaceResult.licenses);

        // 审计各成员包
        var manifests = workspace.get_member_manifests();
        foreach (var manifest in manifests)
        {
            var memberPackages = new List<PackageInfo>();
            foreach (var dep in manifest.dependencies)
                memberPackages.Add(new PackageInfo { name = dep.Key, version = dep.Value });

            var memberResult = await security_audit.audit_dependencies(
                [.. memberPackages.Select(p => p.to_registry_package())]);
            result.vulnerabilities.AddRange(memberResult.vulnerabilities);
            result.licenses.AddRange(memberResult.licenses);
        }

        return result;
    }

    private async Task<SecurityAuditResult> audit_package()
    {
        var lockedPackages = lock_file.get_all_packages();
        var packages = new List<PackageInfo>();

        foreach (var locked in lockedPackages)
            packages.Add(new PackageInfo
            {
                name = locked.name,
                version = locked.version,
                dependencies = locked.dependencies
            });

        return await security_audit.audit_dependencies(
            [.. packages.Select(p => p.to_registry_package())]);
    }

    public async Task<SecurityAuditResult> audit_package(PackageInfo package)
    {
        return await security_audit.audit_package(package.to_registry_package());
    }

    #endregion

    #region 包发布

    public async Task<PublishResult> publish(PublishOptions? options = null, string registryName = "npm")
    {
        if (is_workspace)
        {
            Console.WriteLine("[Workspace 模式] 发布所有工作区成员");
            return await publish_workspace(options, registryName);
        }

        if (has_manifest)
        {
            Console.WriteLine("[Package 模式] 发布当前包");
            return await publish_package(options, registryName);
        }

        throw new InvalidOperationException("当前目录不是 Legion 包或工作区，无法发布");
    }

    private async Task<PublishResult> publish_workspace(PublishOptions? options, string registryName = "npm")
    {
        if (workspace is null || manifest is null) throw new InvalidOperationException("工作区未加载");

        await run_hook_if_present(LegionManifest.HookNames.pre_publish);

        var rootOptions = options ?? new PublishOptions
        {
            package_name = manifest.name,
            version = manifest.version,
            package_path = base_directory
        };

        rootOptions.registry_name = registryName;
        inject_auth_token(rootOptions);
        var result = await publisher.publish(rootOptions);

        await run_hook_if_present(LegionManifest.HookNames.post_publish);

        return result;
    }

    private async Task<PublishResult> publish_package(PublishOptions? options, string registryName = "npm")
    {
        if (manifest is null) throw new InvalidOperationException("包清单未加载");

        await run_hook_if_present(LegionManifest.HookNames.pre_publish);

        var packageOptions = options ?? new PublishOptions
        {
            package_name = manifest.name,
            version = manifest.version,
            package_path = base_directory
        };

        packageOptions.registry_name = registryName;
        inject_auth_token(packageOptions);
        var result = await publisher.publish(packageOptions);

        await run_hook_if_present(LegionManifest.HookNames.post_publish);

        return result;
    }

    /// <summary>
    ///     自动注入已存储的认证令牌到发布选项
    /// </summary>
    private void inject_auth_token(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.auth_token))
        {
            var storedToken = _auth_store.get_token(options.registry_name);

            if (storedToken is not null)
            {
                options.auth_token = storedToken;
                Console.WriteLine($"已自动使用 {options.registry_name} 的已存储认证令牌");
            }
        }
    }

    #endregion

    #region 缓存管理

    public async Task clean_cache()
    {
        await cache.clean();
    }

    public async Task clear_cache()
    {
        await cache.clear();
    }

    #endregion

    #region 清单操作

    /// <summary>
    ///     创建新的包清单文件 legion.von，同时生成 voa.config.v
    /// </summary>
    /// <param name="name">包名</param>
    /// <param name="version">初始版本</param>
    /// <param name="description">包描述</param>
    /// <param name="author">作者</param>
    /// <param name="license">许可证</param>
    /// <param name="projectType">项目类型：library / application / sdk</param>
    /// <param name="targetArch">目标架构</param>
    public LegionManifest create_manifest(
        string name,
        string version = "0.0.0",
        string? description = null,
        string? author = null,
        string? license = null,
        string projectType = "application",
        string targetArch = "wasm")
    {
        var manifest = new LegionManifest(base_directory, _parse)
        {
            name = name,
            version = version,
            description = description ?? string.Empty,
            author = author ?? string.Empty,
            license = license ?? "MIT"
        };

        manifest.save();
        this.manifest = manifest;

        voa_config = new VoaConfig(base_directory, _parse)
        {
            project_type = projectType
        };

        voa_config.target.arch = targetArch;
        voa_config.save();

        return manifest;
    }

    public LegionsWorkspace create_workspace()
    {
        var workspace = new LegionsWorkspace(base_directory, _parse);
        workspace.save();
        this.workspace = workspace;

        return workspace;
    }

    public LegionIgnore create_ignore()
    {
        var ignore = LegionIgnore.create_default(base_directory);
        this.ignore = ignore;

        return ignore;
    }

    #endregion

    #region 过期检查

    /// <summary>
    ///     验证所有已安装包的完整性，返回校验结果
    /// </summary>
    /// <returns>校验失败的包列表（空列表表示全部通过）</returns>
    public List<IntegrityCheckResult> verify_integrity()
    {
        if (!lock_file.exists()) throw new InvalidOperationException("未找到 legion-lock.von，请先运行 legion install");

        return lock_file.verify_integrity(vendors_directory);
    }

    /// <summary>
    ///     检测锁文件与 manifest 之间的版本漂移
    /// </summary>
    /// <returns>漂移的依赖名称列表</returns>
    public List<string> detect_drift()
    {
        if (manifest is null) throw new InvalidOperationException("未加载 legion.von");

        if (!lock_file.exists()) throw new InvalidOperationException("未找到 legion-lock.von");

        return lock_file.detect_drift(manifest);
    }

    /// <summary>
    ///     检查所有依赖是否过期，返回可更新的依赖列表
    /// </summary>
    public async Task<List<OutdatedDependency>> check_outdated()
    {
        var result = new List<OutdatedDependency>();

        var dependencies = new Dictionary<string, string>();
        if (manifest is not null)
        {
            foreach (var dep in manifest.dependencies) dependencies[dep.Key] = dep.Value;

            foreach (var dep in manifest.dev_dependencies) dependencies[dep.Key] = dep.Value;
        }

        if (workspace is not null)
            foreach (var dep in workspace.dependencies)
                dependencies[dep.Key] = dep.Value;

        var lockedPackages = lock_file.get_all_packages();
        var lockedMap = new Dictionary<string, string>();
        foreach (var locked in lockedPackages) lockedMap[locked.name] = locked.version;

        foreach (var dep in dependencies)
        {
            var currentVersion = lockedMap.TryGetValue(dep.Key, out var locked) ? locked : dep.Value;
            var latestVersion = await get_latest_version(dep.Key);

            if (latestVersion is null) continue;

            if (currentVersion == latestVersion) continue;

            var currentSemVer = SemanticVersion.parse(currentVersion.TrimStart('^', '~', '>', '<', '='));
            var latestSemVer = SemanticVersion.parse(latestVersion);

            if (currentSemVer < latestSemVer)
                result.Add(new OutdatedDependency
                {
                    package_name = dep.Key,
                    current_version = currentVersion,
                    latest_version = latestVersion,
                    constraint = dep.Value
                });
        }

        return result;
    }

    /// <summary>
    ///     获取指定包的最新版本号
    /// </summary>
    private async Task<string?> get_latest_version(string packageName)
    {
        foreach (var registry in _registries.Values)
            try
            {
                var packages = await registry.search_packages(packageName);
                var match = packages.FirstOrDefault(p => p.name == packageName);
                if (match is not null) return match.version;
            }
            catch
            {
            }

        return null;
    }

    #endregion

    #region 私有方法

    private void register_default_registries()
    {
        register_registry(new NpmRegistry());
        register_registry(new JsrRegistry());
        register_registry(new CondaRegistry());
        register_registry(new MavenRegistry());
        register_registry(new NuGetRegistry());
        register_valhalla_registry();

        foreach (var endpoint in config.registry_endpoints) register_registry_endpoint(endpoint.Key, endpoint.Value);
    }

    private string resolve_base_directory()
    {
        var valkyrieHome = Environment.GetEnvironmentVariable("VALKYRIE_HOME");
        if (!string.IsNullOrEmpty(valkyrieHome)) return valkyrieHome;

        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "legion.von")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "voa.workspace.v")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "valkyrie.von")) ||
            File.Exists(Path.Combine(Environment.CurrentDirectory, "project.von")))
            return Environment.CurrentDirectory;

        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userHome)) return Path.Combine(userHome, ".valkyrie");

        return Environment.CurrentDirectory;
    }

    private string resolve_registry_endpoint(string registryName)
    {
        if (_registries.TryGetValue(registryName, out var registry)) return registry.endpoint;

        return registryName switch
        {
            "npm" => "registry.npmjs.org",
            "jsr" => "jsr.io",
            "conda" => "api.anaconda.org",
            "maven" => "search.maven.org",
            "nuget" => "api.nuget.org",
            "valhalla" => "valhalla.nyar.dev",
            _ => registryName
        };
    }

    private string extract_org_name(string packageName)
    {
        if (packageName.StartsWith("@"))
        {
            var slashIndex = packageName.IndexOf('/');
            if (slashIndex > 0) return packageName[1..slashIndex];
        }

        return "default";
    }

    private string build_package_path(string registryName, string endpoint, string orgName, string packageName,
        string version)
    {
        var safePackageName = packageName.Replace("@", "").Replace("/", "-");
        return Path.Combine(vendors_directory, $"{registryName}@{endpoint}", $"{orgName}@{safePackageName}@{version}");
    }

    private string resolve_vendors_directory()
    {
        // 1. 检查当前目录是否有 vendors（项目级，优先级最高）
        var localVendors = Path.Combine(Environment.CurrentDirectory, "vendors");
        if (Directory.Exists(localVendors)) return localVendors;

        // 2. 如果在工作区成员中，检查工作区根目录的 vendors
        var workspaceRoot = find_workspace_root(Environment.CurrentDirectory);
        if (workspaceRoot is not null)
        {
            var workspaceVendors = Path.Combine(workspaceRoot, "vendors");
            if (Directory.Exists(workspaceVendors)) return workspaceVendors;
        }

        // 3. 回退到全局 vendors
        return Path.Combine(base_directory, "vendors");
    }

    private string? find_workspace_root(string startDirectory)
    {
        var currentDir = startDirectory;

        while (!string.IsNullOrEmpty(currentDir))
        {
            if (File.Exists(Path.Combine(currentDir, "voa.workspace.v"))) return currentDir;

            var parent = Path.GetDirectoryName(currentDir);
            if (parent == currentDir) break;

            currentDir = parent ?? string.Empty;
        }

        return null;
    }

    /// <summary>
    ///     通过反射创建 ValhallaRegistry 实例（避免循环依赖）
    /// </summary>
    private static IRegistry create_valhalla_registry(string endpointUrl)
    {
        var registry = load_valhalla_registry_plugin();
        if (registry is null)
            throw new InvalidOperationException("无法加载 ValhallaRegistry 插件，请确保 Legion.Registry.Valhalla.dll 存在");

        registry.endpoint = endpointUrl;
        return registry;
    }

    /// <summary>
    ///     注册默认的 Valhalla 注册表
    /// </summary>
    private void register_valhalla_registry()
    {
        var registry = load_valhalla_registry_plugin();
        if (registry is not null) register_registry(registry);
    }

    /// <summary>
    ///     从插件目录加载 ValhallaRegistry 类型
    /// </summary>
    private static IRegistry? load_valhalla_registry_plugin()
    {
        const string typeFullName = "Legion.ValhallaRegistry";
        const string assemblyName = "Legion.Registry.Valhalla";

        try
        {
            var assembly = Assembly.Load(assemblyName);
            var type = assembly.GetType(typeFullName);
            if (type is not null && Activator.CreateInstance(type) is IRegistry registry) return registry;
        }
        catch (FileNotFoundException)
        {
            var assemblyPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"{assemblyName}.dll");

            if (File.Exists(assemblyPath))
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var type = assembly.GetType(typeFullName);
                    if (type is not null && Activator.CreateInstance(type) is IRegistry registry) return registry;
                }
                catch
                {
                    // 无法加载插件注册器
                }
        }
        catch
        {
            // 无法加载插件注册器，Valhalla 功能不可用
        }

        return null;
    }

    /// <summary>
    ///     获取远程包信息
    /// </summary>
    /// <param name="packageName">包名</param>
    /// <param name="registryName">注册表名</param>
    public async Task<PackageInfo?> get_package_info(string packageName, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry))
        {
            Console.WriteLine($"未找到注册器：{registryName}");
            return null;
        }

        try
        {
            var packages = await registry.search_packages(packageName);
            var match = packages.FirstOrDefault(p => p.name == packageName);

            if (match is null) return null;

            var info = new PackageInfo
            {
                name = match.name,
                latest_version = match.version,
                license = match.license ?? "unknown",
                description = match.description ?? string.Empty,
                author = match.author ?? string.Empty
            };

            if (manifest is not null)
                info.is_installed = lock_file.get_all_packages()
                    .Any(p => p.name == packageName);

            return info;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region 类型转换

    private static PackageInfo to_package_info(PackageRegistry.Package pkg)
    {
        return new PackageInfo
        {
            name = pkg.name,
            version = pkg.version,
            latest_version = pkg.version,
            license = pkg.license ?? "unknown",
            description = pkg.description ?? string.Empty,
            author = pkg.author ?? string.Empty,
            dependencies = pkg.dependencies,
            dependency_versions = pkg.dependency_versions,
            peer_dependencies = pkg.peer_dependencies
        };
    }

    private static List<PackageInfo> to_package_info_list(List<PackageRegistry.Package> packages)
    {
        return [.. packages.Select(to_package_info)];
    }

    #endregion
}