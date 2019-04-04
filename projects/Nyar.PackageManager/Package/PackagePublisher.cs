using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using Nyar.PackageManager.Scripts;
using Nyar.PackageManager.Tools;
using Nyar.PackageManager.Version;
using Nyar.PackageRegistry;

namespace Nyar.PackageManager.Package;

/// <summary>
///     包发布器，负责打包、版本递增、Git Tag 和发布流程
/// </summary>
public class PackagePublisher
{
    private readonly SerdeParser _parse;
    private readonly Dictionary<string, IRegistry> _registries;

    public PackagePublisher(Dictionary<string, IRegistry> registries, SerdeParser parse)
    {
        _registries = registries;
        _parse = parse;
    }

    /// <summary>
    ///     执行完整发布流程：版本递增 → Git 检查 → 打包 → 发布 → Tag
    /// </summary>
    public async Task<PublishResult> publish(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.package_name)) throw new ArgumentException("包名不能为空");

        if (string.IsNullOrWhiteSpace(options.package_path)) throw new ArgumentException("包路径不能为空");

        if (!Directory.Exists(options.package_path))
            throw new DirectoryNotFoundException($"包目录不存在: {options.package_path}");

        // 1. 版本递增
        if (options.bump is not null)
        {
            options.version = bump_version(options.package_path, options.version, options.bump.Value);
            Console.WriteLine($"版本已递增 → {options.version}");
        }

        if (string.IsNullOrWhiteSpace(options.version)) throw new ArgumentException("版本号不能为空");

        // 2. Git 工作区检查
        if (!options.skip_git_check)
        {
            var (gitClean, gitMessage) = check_git_status(options.package_path);
            if (!gitClean)
            {
                Console.WriteLine("⚠ Git 工作区不干净，建议先提交或使用 --skip-git-check：");
                Console.WriteLine($"  {gitMessage}");

                if (!options.create_git_tag)
                {
                }
            }
        }

        // 3. 包验证
        if (!validate_package(options))
            return new PublishResult
            {
                success = false,
                package_name = options.package_name,
                version = options.version,
                message = "包验证失败"
            };

        // 4. 发布前脚本
        if (options.run_pre_publish_script) await run_pre_publish_script(options.package_path);

        // 5. 打包
        var ignore = new LegionIgnore(options.package_path);
        var ignoreFilePath = Path.Combine(options.package_path, "legion.ignore");
        if (File.Exists(ignoreFilePath)) ignore.load();

        var packResult = PackagePacker.pack(options.package_path, ignore);
        var tarballData = packResult.tarball_data;
        Console.WriteLine($"打包完成: {options.package_name}@{options.version} " +
                          $"({packResult.size / 1024.0:F1} KB, {packResult.file_count} 个文件, {packResult.sha256[..24]}...)");

        // 6. 发布
        if (!_registries.TryGetValue(options.registry_name, out var registry))
            throw new ArgumentException($"注册器 {options.registry_name} 未找到");

        Console.WriteLine($"正在发布到 {options.registry_name} ({registry.endpoint})...");
        var result = await registry.publish_package(options, tarballData);

        // 7. Git Tag
        if (result.success && options.create_git_tag)
        {
            var tagResult = create_git_tag(options.package_path, options.version, options.git_tag_prefix);
            if (tagResult.success)
            {
                Console.WriteLine($"Git Tag 已创建：{tagResult.tag_name}");

                if (tagResult.pushed) Console.WriteLine("Tag 已推送到远程仓库");
            }
            else
            {
                Console.WriteLine($"Git Tag 创建失败：{tagResult.error}");
            }
        }

        return result;
    }

    #region 发布前脚本

    /// <summary>
    ///     执行发布前脚本（legion.von 中 scripts.prePublish）
    /// </summary>
    private async Task run_pre_publish_script(string packagePath)
    {
        var scriptRunner = new ScriptRunner(packagePath);

        try
        {
            var manifest = new LegionManifest(packagePath, _parse);
            manifest.load();

            if (manifest.scripts.TryGetValue("prePublish", out var script))
            {
                Console.WriteLine("执行 prePublish 脚本...");
                await scriptRunner.run(script);
            }
        }
        catch
        {
            Console.WriteLine("prePublish 脚本执行失败，继续发布...");
        }
    }

    #endregion

    public bool validate_package(PublishOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.package_name)) return false;

        if (string.IsNullOrWhiteSpace(options.version)) return false;

        if (!SemanticVersion.try_parse(options.version, out _)) return false;

        if (!Directory.Exists(options.package_path)) return false;

        return true;
    }

    public async Task<bool> check_package_exists(string packageName, string registryName = "npm")
    {
        if (!_registries.TryGetValue(registryName, out var registry)) return false;

        try
        {
            var package = await registry.get_package(packageName, "latest");
            return package is not null;
        }
        catch (RegistryException ex) when (ex.status_code == 404)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public byte[] create_tarball(string packagePath, string packageName, string version)
    {
        var ignore = new LegionIgnore(packagePath);
        var ignoreFilePath = Path.Combine(packagePath, "legion.ignore");
        if (File.Exists(ignoreFilePath)) ignore.load();

        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true))
        {
            using var tarWriter = new TarWriter(gzipStream, true);

            foreach (var filePath in Directory.EnumerateFiles(packagePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(packagePath, filePath);

                if (should_ignore_file(relativePath, ignore)) continue;

                var entryPath = $"package/{relativePath.Replace('\\', '/')}";

                var fileInfo = new FileInfo(filePath);
                var entry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
                {
                    ModificationTime = fileInfo.LastWriteTimeUtc
                };

                using (var fileStream = fileInfo.OpenRead())
                {
                    entry.DataStream = fileStream;
                    tarWriter.WriteEntry(entry);
                }
            }
        }

        return memoryStream.ToArray();
    }

    private bool should_ignore_file(string relativePath, LegionIgnore ignore)
    {
        var normalizedPath = relativePath.Replace('\\', '/');

        if (normalizedPath.StartsWith("vendors/") || normalizedPath.StartsWith("vendors\\")) return true;

        if (normalizedPath.StartsWith(".cache/") || normalizedPath.StartsWith(".cache\\")) return true;

        if (normalizedPath.StartsWith("node_modules/") || normalizedPath.StartsWith("node_modules\\")) return true;

        if (normalizedPath == "legion-lock.von") return true;

        if (ignore.is_ignored(normalizedPath)) return true;

        return false;
    }

    public static string compute_integrity(byte[] data)
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(data);
        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    public static string compute_file_integrity(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha512.ComputeHash(stream);
        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    #region 版本递增

    /// <summary>
    ///     按指定类型递增版本号
    /// </summary>
    /// <param name="packagePath">包路径（用于写回 legion.von）</param>
    /// <param name="currentVersion">当前版本号字符串</param>
    /// <param name="bump">递增类型</param>
    /// <returns>新版本号字符串</returns>
    public string bump_version(string packagePath, string currentVersion, VersionBump bump)
    {
        if (!SemanticVersion.try_parse(currentVersion, out var semVer) || semVer is null)
            throw new ArgumentException($"无效的版本号：{currentVersion}");

        var newVersion = bump switch
        {
            VersionBump.patch => new SemanticVersion(semVer.major, semVer.minor, semVer.patch + 1),
            VersionBump.minor => new SemanticVersion(semVer.major, semVer.minor + 1, 0),
            VersionBump.major => new SemanticVersion(semVer.major + 1, 0, 0),
            _ => semVer
        };

        var newVersionStr = newVersion.ToString() ?? "0.0.0";
        update_version_in_manifest(packagePath, newVersionStr);
        return newVersionStr;
    }

    /// <summary>
    ///     写回 legion.von 中的版本号
    /// </summary>
    private void update_version_in_manifest(string packagePath, string newVersion)
    {
        var manifestPath = Path.Combine(packagePath, "legion.von");

        if (!File.Exists(manifestPath)) return;

        var manifest = new LegionManifest(packagePath, _parse);
        manifest.load();
        manifest.version = newVersion;
        manifest.save();
    }

    #endregion

    #region Git 集成

    /// <summary>
    ///     检查 Git 工作区状态
    /// </summary>
    /// <returns>(是否干净, 状态信息)</returns>
    public static (bool IsClean, string Message) check_git_status(string packagePath)
    {
        try
        {
            var status = run_git_command(packagePath, "status --porcelain");
            var isClean = string.IsNullOrWhiteSpace(status.std_out);

            if (!isClean)
            {
                var changedFiles = status.std_out?.Trim().Split('\n').Length ?? 0;
                return (false, $"{changedFiles} 个文件有改动（git status --porcelain）");
            }

            return (true, "工作区干净");
        }
        catch
        {
            return (true, "无法检测 Git 状态（未安装 Git 或非 Git 仓库）");
        }
    }

    /// <summary>
    ///     创建 Git Tag 并尝试推送
    /// </summary>
    public static GitTagResult create_git_tag(string packagePath, string version, string? prefix)
    {
        var tagName = $"{prefix ?? string.Empty}{version}";

        try
        {
            var addResult = run_git_command(packagePath, $"tag -a \"{tagName}\" -m \"Release {version}\"");
            if (addResult.exit_code != 0) return new GitTagResult { success = false, error = addResult.std_err };

            var pushResult = run_git_command(packagePath, "push origin --tags");
            return new GitTagResult
            {
                success = true,
                tag_name = tagName,
                pushed = pushResult.exit_code == 0
            };
        }
        catch (Exception ex)
        {
            return new GitTagResult { success = false, error = ex.Message };
        }
    }

    /// <summary>
    ///     获取当前分支名
    /// </summary>
    public static string? get_git_branch(string packagePath)
    {
        try
        {
            var result = run_git_command(packagePath, "rev-parse --abbrev-ref HEAD");
            return result.exit_code == 0 ? result.std_out?.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private static GitCommandResult run_git_command(string workingDir, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(TimeSpan.FromSeconds(10).Milliseconds);

        return new GitCommandResult
        {
            exit_code = process.ExitCode,
            std_out = stdout,
            std_err = stderr
        };
    }

    private struct GitCommandResult
    {
        public int exit_code;
        public string? std_out;
        public string? std_err;
    }

    /// <summary>
    ///     Git Tag 操作结果
    /// </summary>
    public class GitTagResult
    {
        public bool success { get; set; }
        public string? tag_name { get; set; }
        public bool pushed { get; set; }
        public string? error { get; set; }
    }

    #endregion
}