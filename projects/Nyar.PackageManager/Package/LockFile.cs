using System.Security.Cryptography;
using System.Text;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Nyar.PackageManager.Version;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Package;

public class LockFile
{
    private readonly string _file_path;
    private readonly SerdeParser _parse;

    /// <summary>
    ///     鍒涘缓 LockFile 瀹炰緥
    /// </summary>
    /// <param name="directoryPath">椤圭洰鐩綍璺緞</param>
    /// <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public LockFile(string directoryPath, SerdeParser parse)
    {
        _file_path = Path.Combine(directoryPath, "legion-lock.von");
        _parse = parse;
    }

    public string version { get; set; } = "1";
    public Dictionary<string, LockEntry> packages { get; set; } = new();

    public bool exists()
    {
        return File.Exists(_file_path);
    }

    public void load()
    {
        if (!File.Exists(_file_path))
        {
            packages = new Dictionary<string, LockEntry>();
            return;
        }

        var content = File.ReadAllText(_file_path, Encoding.UTF8);
        var root = _parse(content);

        if (root.type != SerdeValueType.@object)
        {
            packages = new Dictionary<string, LockEntry>();
            return;
        }

        version = ValkyrieValueProjector.bind_utf8_field(root, "version") ?? "1";

        var packagesField = root.get_field("packages");
        if (packagesField is { type: SerdeValueType.@object, fields: not null })
        {
            packages = new Dictionary<string, LockEntry>();
            foreach (var kvp in packagesField.fields)
            {
                var entryObj = kvp.Value;
                if (entryObj.type != SerdeValueType.@object) continue;

                var entry = new LockEntry
                {
                    name = ValkyrieValueProjector.bind_utf8_field(entryObj, "name") ?? string.Empty,
                    version = ValkyrieValueProjector.bind_utf8_field(entryObj, "version") ?? string.Empty,
                    registry = ValkyrieValueProjector.bind_utf8_field(entryObj, "registry") ?? string.Empty,
                    resolved = ValkyrieValueProjector.bind_utf8_field(entryObj, "resolved") ?? string.Empty,
                    integrity = ValkyrieValueProjector.bind_utf8_field(entryObj, "integrity") ?? string.Empty,
                    license = ValkyrieValueProjector.bind_utf8_field(entryObj, "license") ?? string.Empty,
                    is_dev = ValkyrieValueProjector.bind_bool_field(entryObj, "is_dev") ?? false,
                    is_workspace = ValkyrieValueProjector.bind_bool_field(entryObj, "is_workspace") ?? false,
                    install_path = ValkyrieValueProjector.bind_utf8_field(entryObj, "install_path") ?? string.Empty
                };

                entry.dependencies = ValkyrieValueProjector.bind_utf8_array_field(entryObj, "dependencies")?.ToList() ?? [];

                packages[kvp.Key] = entry;
            }
        }
    }

    public void save()
    {
        var dir = Path.GetDirectoryName(_file_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var packagesFields = new Dictionary<string, SerdeValue>();
        foreach (var kvp in packages.OrderBy(p => p.Key))
        {
            var entryFields = new Dictionary<string, SerdeValue>
            {
                ["name"] = SerdeValue.@string(kvp.Value.name),
                ["version"] = SerdeValue.@string(kvp.Value.version),
                ["registry"] = SerdeValue.@string(kvp.Value.registry),
                ["resolved"] = SerdeValue.@string(kvp.Value.resolved),
                ["integrity"] = SerdeValue.@string(kvp.Value.integrity)
            };

            if (!string.IsNullOrEmpty(kvp.Value.license))
                entryFields["license"] = SerdeValue.@string(kvp.Value.license);

            if (kvp.Value.is_dev) entryFields["is_dev"] = SerdeValue.boolean(true);

            if (kvp.Value.is_workspace) entryFields["is_workspace"] = SerdeValue.boolean(true);

            if (!string.IsNullOrEmpty(kvp.Value.install_path))
                entryFields["install_path"] = SerdeValue.@string(kvp.Value.install_path);

            if (kvp.Value.dependencies is not null && kvp.Value.dependencies.Count > 0)
                entryFields["dependencies"] = SerdeValue.array(
                    [.. kvp.Value.dependencies.Select(d => SerdeValue.@string(d))]);

            packagesFields[kvp.Key] = SerdeValue.@object(entryFields);
        }

        var rootFields = new Dictionary<string, SerdeValue>
        {
            ["version"] = SerdeValue.@string(version),
            ["packages"] = SerdeValue.@object(packagesFields)
        };

        var root = SerdeValue.@object(rootFields);
        var content = VonFormatter.format(root);
        File.WriteAllText(_file_path, content, Encoding.UTF8);
    }

    public void add_package(PackageRegistry.Package package, string registryName, string resolvedUrl)
    {
        var key = $"{package.name}@{package.version}";

        var integrity = package.dist_integrity ?? compute_package_integrity(package);

        packages[key] = new LockEntry
        {
            name = package.name,
            version = package.version,
            registry = registryName,
            resolved = resolvedUrl,
            integrity = integrity,
            dependencies = package.dependencies ?? []
        };
    }

    public void remove_package(string packageName)
    {
        var keysToRemove = packages.Keys
            .Where(k => k.StartsWith($"{packageName}@"))
            .ToList();

        foreach (var key in keysToRemove) packages.Remove(key);
    }

    public LockEntry? get_package(string packageName, string? version = null)
    {
        if (version is not null)
        {
            var key = $"{packageName}@{version}";
            return packages.GetValueOrDefault(key);
        }

        var matchingKey = packages.Keys.FirstOrDefault(k => k.StartsWith($"{packageName}@"));
        return matchingKey is not null ? packages[matchingKey] : null;
    }

    public bool is_package_locked(string packageName, string version)
    {
        var key = $"{packageName}@{version}";
        return packages.ContainsKey(key);
    }

    public List<LockEntry> get_all_packages()
    {
        return [.. packages.Values];
    }

    public void clear()
    {
        packages.Clear();
    }

    public static string compute_package_integrity(PackageRegistry.Package package)
    {
        using var sha512 = SHA512.Create();

        var data = $"{package.name}@{package.version}";
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    public static string compute_file_integrity(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha512.ComputeHash(stream);

        return $"sha512-{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    ///     楠岃瘉閿佹枃浠朵腑鎵€鏈夊寘鐨勫畬鏁存€э紝杩斿洖鏍￠獙缁撴灉
    /// </summary>
    /// <param name="vendorsDir">渚濊禆瀹夎鐩綍</param>
    /// <returns>鏍￠獙澶辫触鐨勫寘鍒楄〃锛堢┖鍒楄〃琛ㄧず鍏ㄩ儴閫氳繃锛?/returns>
    public List<IntegrityCheckResult> verify_integrity(string vendorsDir)
    {
        var failures = new List<IntegrityCheckResult>();

        foreach (var kvp in packages)
        {
            var entry = kvp.Value;

            if (entry.is_workspace) continue;

            if (string.IsNullOrEmpty(entry.integrity))
            {
                failures.Add(new IntegrityCheckResult
                {
                    package_name = entry.name,
                    version = entry.version,
                    issue = IntegrityIssue.missing_integrity,
                    message = "缺少完整性哈希"
                });
                continue;
            }

            var installPath = string.IsNullOrEmpty(entry.install_path)
                ? Path.Combine(vendorsDir, entry.name)
                : Path.Combine(vendorsDir, entry.install_path);

            if (!Directory.Exists(installPath))
            {
                failures.Add(new IntegrityCheckResult
                {
                    package_name = entry.name,
                    version = entry.version,
                    issue = IntegrityIssue.missing_package,
                    message = $"瀹夎鐩綍涓嶅瓨鍦細{installPath}"
                });
                continue;
            }

            var packageHash = compute_directory_integrity(installPath);
            var expectedPrefix = extract_hash_prefix(entry.integrity);

            if (!string.IsNullOrEmpty(expectedPrefix) && !packageHash.StartsWith(expectedPrefix))
                failures.Add(new IntegrityCheckResult
                {
                    package_name = entry.name,
                    version = entry.version,
                    issue = IntegrityIssue.hash_mismatch,
                    message =
                        $"瀹屾暣鎬ф牎楠屽け璐ワ細鏈熸湜 {entry.integrity[..Math.Min(24, entry.integrity.Length)]}...锛屽疄闄?{packageHash[..Math.Min(24, packageHash.Length)]}..."
                });
        }

        return failures;
    }

    /// <summary>
    ///     璁＄畻鐩綍鐨勫畬鏁存€у搱甯岋紙閫掑綊鎵€鏈夋枃浠讹級
    /// </summary>
    /// <param name="directoryPath">鐩綍璺緞</param>
    /// <returns>sha512-Base64 鏍煎紡鐨勫搱甯屽€?/returns>
    public static string compute_directory_integrity(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return string.Empty;

        using var sha512 = SHA512.Create();

        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        foreach (var file in files)
        {
            var relativePath = file[(directoryPath.Length + 1)..];
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            sha512.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

            using var stream = File.OpenRead(file);
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                sha512.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha512.TransformFinalBlock([], 0, 0);
        return $"sha512-{Convert.ToBase64String(sha512.Hash!)}";
    }

    /// <summary>
    ///     浠庡畬鏁存€у搱甯屼腑鎻愬彇鍓嶇紑鐢ㄤ簬蹇€熸瘮杈?    ///
    /// </summary>
    private static string extract_hash_prefix(string integrity)
    {
        var dashIndex = integrity.IndexOf('-');
        if (dashIndex < 0 || dashIndex + 1 >= integrity.Length) return string.Empty;

        return integrity[(dashIndex + 1)..];
    }

    /// <summary>
    ///     妫€鏌ラ攣鏂囦欢鏄惁涓?manifest 涓€鑷达紙妫€娴嬫槸鍚﹂渶瑕侀噸鏂板畨瑁咃級
    /// </summary>
    /// <param name="manifest">椤圭洰娓呭崟</param>
    /// <returns>涓嶄竴鑷寸殑渚濊禆鍒楄〃</returns>
    public List<string> detect_drift(LegionManifest manifest)
    {
        var drifted = new List<string>();

        foreach (var dep in manifest.dependencies)
        {
            var locked = get_package(dep.Key);
            if (locked is null)
            {
                drifted.Add(dep.Key);
                continue;
            }

            if (!satisfies_constraint(locked.version, dep.Value)) drifted.Add(dep.Key);
        }

        foreach (var dep in manifest.dev_dependencies)
        {
            var locked = get_package(dep.Key);
            if (locked is null)
            {
                drifted.Add(dep.Key);
                continue;
            }

            if (!satisfies_constraint(locked.version, dep.Value)) drifted.Add(dep.Key);
        }

        return drifted;
    }

    /// <summary>
    ///     妫€鏌ョ増鏈彿鏄惁婊¤冻绾︽潫鏉′欢
    /// </summary>
    private static bool satisfies_constraint(string version, string constraint)
    {
        if (constraint is "*" or "latest") return true;

        if (constraint == version) return true;

        if (constraint.StartsWith('^'))
        {
            var minVersion = constraint[1..];
            var minSemVer = SemanticVersion.parse(minVersion);
            var actualSemVer = SemanticVersion.parse(version);
            return actualSemVer >= minSemVer &&
                   actualSemVer.major == minSemVer.major;
        }

        if (constraint.StartsWith('~'))
        {
            var minVersion = constraint[1..];
            var minSemVer = SemanticVersion.parse(minVersion);
            var actualSemVer = SemanticVersion.parse(version);
            return actualSemVer >= minSemVer &&
                   actualSemVer.major == minSemVer.major &&
                   actualSemVer.minor == minSemVer.minor;
        }

        return version == constraint;
    }
}
