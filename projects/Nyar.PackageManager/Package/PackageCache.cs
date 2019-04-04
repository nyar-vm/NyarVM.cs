using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Nyar.Language.Valkyrie.Semantic;
using Nyar.PackageManager.Tools;
using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Package;

/// <summary>
///     鍏ㄥ眬鍖呯紦瀛橈紙Content-Addressable Store锛夛紝绫绘瘮 pnpm store銆?///     鍖呮寜鍐呭鍝堝笇瀛樺偍锛屾敮鎸佺‖閾炬帴瀹夎浠ュ噺灏戠鐩樺崰鐢ㄣ€?///
/// </summary>
public class PackageCache
{
    private const string _store_dir_name = "store";
    private const string _index_file_name = "cache-index.von";
    private readonly Dictionary<string, List<CachedPackage>> _index = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _index_file_path;
    private readonly SemaphoreSlim _index_lock = new(1, 1);
    private readonly SerdeParser _parse;

    private readonly string _root_directory;
    private readonly string _store_directory;

    private long _hits;
    private long _misses;

    /// <summary>
    ///     鍒涘缓鍖呯紦瀛樺疄渚?    ///
    /// </summary>
    /// <param name="rootDirectory">
    ///     缂撳瓨鏍圭洰褰曪紙閫氬父涓?~/.valkyrie/锛?/param>
    ///     <param name="parse">閰嶇疆鏂囨湰瑙ｆ瀽鍣?/param>
    public PackageCache(string rootDirectory, SerdeParser parse)
    {
        _root_directory = rootDirectory;
        _store_directory = Path.Combine(rootDirectory, _store_dir_name);
        _index_file_path = Path.Combine(rootDirectory, _index_file_name);
        _parse = parse;
        Directory.CreateDirectory(_store_directory);
    }

    /// <summary>
    ///     缂撳瓨鍛戒腑娆℃暟
    /// </summary>
    public long hits => _hits;

    /// <summary>
    ///     缂撳瓨鏈懡涓鏁?    ///
    /// </summary>
    public long misses => _misses;

    /// <summary>
    ///     缂撳瓨鍛戒腑鐜?    ///
    /// </summary>
    public double hit_rate => _hits + _misses > 0 ? (double)_hits / (_hits + _misses) : 0;

    /// <summary>
    ///     閫氳繃纭摼鎺ヨ妭鐪佺殑鎬诲瓧鑺傛暟
    /// </summary>
    public long bytes_saved_by_links { get; private set; }

    #region 鎵归噺骞惰涓嬭浇涓庤В鍘?

    /// <summary>
    ///     骞惰涓嬭浇澶氫釜鍖呯殑宸ヤ欢骞跺瓨鍏?Store
    /// </summary>
    /// <param name="packages">鍖呭垪琛紙鍖呭悕 鈫?涓嬭浇URL鎴栨湰鍦拌矾寰勶級</param>
    /// <param name="downloadFunc">涓嬭浇鍑芥暟锛氬寘鍚?鈫?鏈湴涓存椂璺緞</param>
    /// <param name="parallelism">骞惰搴︼紝榛樿 8</param>
    public async Task<List<(string PackageName, string Version, string StorePath, string ContentHash)>>
        download_and_store_batch(
            List<(string PackageName, string Version, string Source)> packages,
            Func<string, string, Task<string>> downloadFunc,
            int parallelism = 8)
    {
        var results = new ConcurrentBag<(string, string, string, string)>();
        var semaphore = new SemaphoreSlim(parallelism);

        var tasks = packages.Select(async pkg =>
        {
            await semaphore.WaitAsync();

            try
            {
                var localPath = await downloadFunc(pkg.PackageName, pkg.Source);
                var (storePath, hash) = await store_to_store(pkg.PackageName, pkg.Version, localPath);
                results.Add((pkg.PackageName, pkg.Version, storePath, hash));
            }
            catch
            {
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return [.. results];
    }

    #endregion

    #region 鍔犺浇涓庢寔涔呭寲

    /// <summary>
    ///     浠庣储寮曟枃浠跺姞杞界紦瀛?    ///
    /// </summary>
    public async Task load()
    {
        if (!File.Exists(_index_file_path)) return;

        await _index_lock.WaitAsync();

        try
        {
            var content = await File.ReadAllTextAsync(_index_file_path);

            if (string.IsNullOrWhiteSpace(content)) return;

            var value = _parse(content);

            if (ValkyrieValueProjector.as_utf8_map(
                    ValkyrieValueProjector.bind_literal(value, ValkyrieValueProjector.utf8_map_type)) is { } values)
                foreach (var field in values)
                {
                    var parts = field.Key.Split('/', 2);

                    if (parts.Length == 2)
                    {
                        var pkgName = parts[0];
                        var version = parts[1];
                        var localPath = field.Value;

                        if (!_index.TryGetValue(pkgName, out var entries))
                        {
                            entries = [];
                            _index[pkgName] = entries;
                        }

                        entries.Add(new CachedPackage
                        {
                            package_name = pkgName,
                            version = version,
                            local_path = localPath
                        });
                    }
                }
        }
        catch
        {
        }
        finally
        {
            _index_lock.Release();
        }
    }

    /// <summary>
    ///     淇濆瓨缂撳瓨绱㈠紩鍒版枃浠?    ///
    /// </summary>
    public async Task save()
    {
        await _index_lock.WaitAsync();

        try
        {
            var lines = new List<string>();

            foreach (var (name, entries) in _index)
            foreach (var entry in entries)
                lines.Add($"    \"{name}/{entry.version}\": \"{entry.local_path}\"");

            var content = "{\n" + string.Join(",\n", lines) + "\n}\n";
            await File.WriteAllTextAsync(_index_file_path, content);
        }
        finally
        {
            _index_lock.Release();
        }
    }

    #endregion

    #region 鍐呭瀵诲潃瀛樺偍锛圕AS锛?

    /// <summary>
    ///     灏嗗寘鏂囦欢瀛樺叆鍏ㄥ眬 Store锛岃繑鍥炲唴瀹瑰搱甯屽拰瀛樺偍璺緞
    /// </summary>
    /// <param name="packageName">鍖呭悕</param>
    /// <param name="version">
    ///     鐗堟湰鍙?/param>
    ///     <param name="sourcePath">婧愭枃浠?鐩綍璺緞</param>
    ///     <returns>瀛樺偍璺緞鍜屽唴瀹瑰搱甯?/returns>
    public async Task<(string StorePath, string ContentHash)> store_to_store(string packageName, string version,
        string sourcePath)
    {
        var contentHash = await compute_content_hash(sourcePath);
        var storePath = get_store_path(contentHash);

        if (!Directory.Exists(storePath))
        {
            Directory.CreateDirectory(storePath);

            if (File.GetAttributes(sourcePath).HasFlag(FileAttributes.Directory))
                copy_directory_recursive(sourcePath, storePath);
            else
                File.Copy(sourcePath, Path.Combine(storePath, Path.GetFileName(sourcePath)), true);
        }

        var localDir = Path.Combine(_store_directory, $"{sanitize_name(packageName)}@{version}");

        await _index_lock.WaitAsync();

        try
        {
            if (!_index.TryGetValue(packageName, out var entries))
            {
                entries = [];
                _index[packageName] = entries;
            }

            entries.RemoveAll(e => e.version == version);
            entries.Add(new CachedPackage
            {
                package_name = packageName,
                version = version,
                local_path = localDir
            });
        }
        finally
        {
            _index_lock.Release();
        }

        return (storePath, contentHash);
    }

    /// <summary>
    ///     閫氳繃纭摼鎺ュ畨瑁呭寘锛氫粠 Store 鍒涘缓涓€涓埌鐩爣鐩綍鐨勭‖閾炬帴
    /// </summary>
    /// <param name="sourceDir">
    ///     Store 涓殑婧愮洰褰?/param>
    ///     <param name="targetDir">鐩爣瀹夎鐩綍</param>
    ///     <returns>閫氳繃纭摼鎺ヨ妭鐪佺殑瀛楄妭鏁?/returns>
    public long install_with_hard_links(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir)) return 0;

        var savedBytes = 0L;

        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);
            var targetFileDir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetFileDir)) Directory.CreateDirectory(targetFileDir);

            if (File.Exists(targetFile)) File.Delete(targetFile);

            try
            {
                File.CreateSymbolicLink(targetFile, file);
                savedBytes += new FileInfo(file).Length;
            }
            catch
            {
                // 绗﹀彿閾炬帴澶辫触鏃跺洖閫€鍒板鍒?                File.Copy(file, targetFile, true);
            }
        }

        bytes_saved_by_links += savedBytes;
        return savedBytes;
    }

    /// <summary>
    ///     鑾峰彇鍐呭瀵诲潃鐨?Store 璺緞
    /// </summary>
    private string get_store_path(string contentHash)
    {
        // 涓ゅ眰鍓嶇紑鐩綍鍑忓皯鍗曠洰褰曟枃浠舵暟
        var prefix = contentHash[..2];
        return Path.Combine(_store_directory, prefix, contentHash);
    }

    /// <summary>
    ///     璁＄畻鐩綍/鏂囦欢鐨?SHA256 鍐呭鍝堝笇
    /// </summary>
    private static async Task<string> compute_content_hash(string path)
    {
        using var sha256 = SHA256.Create();

        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(path, file);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath);
                sha256.TransformBlock(pathBytes, 0, pathBytes.Length, null, 0);

                var fileBytes = await File.ReadAllBytesAsync(file);
                sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
            }
        }
        else
        {
            var fileBytes = await File.ReadAllBytesAsync(path);
            sha256.TransformBlock(fileBytes, 0, fileBytes.Length, null, 0);
        }

        sha256.TransformFinalBlock([], 0, 0);
        return Convert.ToHexStringLower(sha256.Hash!);
    }

    #endregion

    #region 鏌ヨ

    /// <summary>
    ///     妫€鏌ユ寚瀹氬寘鏄惁鍦ㄧ紦瀛樹腑
    /// </summary>
    public bool has_package(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
            if (entries.Any(e => e.version == version))
            {
                Interlocked.Increment(ref _hits);
                return true;
            }

        Interlocked.Increment(ref _misses);
        return false;
    }

    /// <summary>
    ///     灏嗗寘娉ㄥ唽鍒扮紦瀛樼储寮?    ///
    /// </summary>
    public void add_package(string packageName, string version, string localDir)
    {
        _index_lock.Wait();

        try
        {
            if (!_index.TryGetValue(packageName, out var entries))
            {
                entries = [];
                _index[packageName] = entries;
            }

            entries.RemoveAll(e => e.version == version);
            entries.Add(new CachedPackage
            {
                package_name = packageName,
                version = version,
                local_path = localDir
            });
        }
        finally
        {
            _index_lock.Release();
        }
    }

    /// <summary>
    ///     鑾峰彇鍖呭湪缂撳瓨涓殑鏈湴鐩綍
    /// </summary>
    public string? get_cache_dir(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
            return entries.FirstOrDefault(e => e.version == version)?.local_path;

        return null;
    }

    /// <summary>
    ///     鍒楀嚭缂撳瓨涓殑鎵€鏈夊寘
    /// </summary>
    public List<CachedPackage> list()
    {
        return [.. _index.SelectMany(kvp => kvp.Value)];
    }

    /// <summary>
    ///     涓哄寘鏋勫缓缂撳瓨瀛樺偍璺緞
    /// </summary>
    public string get_package_cache_path(string packageName, string version)
    {
        return Path.Combine(_store_directory, $"{sanitize_name(packageName)}@{version}");
    }

    #endregion

    #region 娓呯悊

    /// <summary>
    ///     娓呯悊杩囨湡缂撳瓨锛氬垹闄ょ鐩樹笂涓嶅瓨鍦ㄤ絾绱㈠紩涓粛鏈夌殑鏉＄洰
    /// </summary>
    public async Task clean()
    {
        await _index_lock.WaitAsync();

        try
        {
            var toRemove = new List<(string Name, string Version)>();

            foreach (var (name, entries) in _index)
            foreach (var entry in entries)
                if (!Directory.Exists(entry.local_path) && !File.Exists(entry.local_path))
                    toRemove.Add((name, entry.version));

            foreach (var (name, version) in toRemove) remove_from_index(name, version);
        }
        finally
        {
            _index_lock.Release();
        }
    }

    /// <summary>
    ///     鍏ㄩ噺娓呯┖鎵€鏈夌紦瀛橈紙Store + 绱㈠紩锛?    ///
    /// </summary>
    public async Task clear()
    {
        await _index_lock.WaitAsync();

        try
        {
            if (Directory.Exists(_store_directory))
            {
                Directory.Delete(_store_directory, true);
                Directory.CreateDirectory(_store_directory);
            }

            if (File.Exists(_index_file_path)) File.Delete(_index_file_path);

            _index.Clear();
            _hits = 0;
            _misses = 0;
            bytes_saved_by_links = 0;
        }
        finally
        {
            _index_lock.Release();
        }
    }

    /// <summary>
    ///     浠庣紦瀛樹腑绉婚櫎鎸囧畾鍖咃紙鍖呮嫭纾佺洏鏂囦欢锛?    ///
    /// </summary>
    public async Task remove_package(string packageName, string version)
    {
        await _index_lock.WaitAsync();

        try
        {
            if (_index.TryGetValue(packageName, out var entries))
            {
                var entry = entries.FirstOrDefault(e => e.version == version);

                if (entry is not null)
                    try
                    {
                        if (Directory.Exists(entry.local_path))
                            Directory.Delete(entry.local_path, true);
                        else if (File.Exists(entry.local_path)) File.Delete(entry.local_path);
                    }
                    catch
                    {
                    }

                entries.RemoveAll(e => e.version == version);

                if (entries.Count == 0) _index.Remove(packageName);
            }
        }
        finally
        {
            _index_lock.Release();
        }
    }

    private void remove_from_index(string packageName, string version)
    {
        if (_index.TryGetValue(packageName, out var entries))
        {
            entries.RemoveAll(e => e.version == version);

            if (entries.Count == 0) _index.Remove(packageName);
        }
    }

    #endregion

    #region 缁熻涓庤瘖鏂?

    /// <summary>
    ///     鑾峰彇缂撳瓨缁熻淇℃伅
    /// </summary>
    public CacheStatistics get_statistics()
    {
        var totalPackages = _index.Sum(kvp => kvp.Value.Count);
        var totalSize = 0L;

        foreach (var (_, entries) in _index)
        foreach (var entry in entries)
            if (Directory.Exists(entry.local_path))
                totalSize += get_directory_size(entry.local_path);
            else if (File.Exists(entry.local_path)) totalSize += new FileInfo(entry.local_path).Length;

        return new CacheStatistics
        {
            total_packages = totalPackages,
            total_size_bytes = totalSize,
            hits = _hits,
            misses = _misses,
            hit_rate = hit_rate,
            bytes_saved_by_links = bytes_saved_by_links
        };
    }

    /// <summary>
    ///     楠岃瘉 Store 瀹屾暣鎬?    ///
    /// </summary>
    public bool verify_store()
    {
        if (!Directory.Exists(_store_directory)) return _index.Count == 0;

        var valid = true;

        foreach (var (_, entries) in _index)
        foreach (var entry in entries)
            if (!Directory.Exists(entry.local_path) && !File.Exists(entry.local_path))
                valid = false;

        return valid;
    }

    #endregion

    #region 宸ュ叿鏂规硶

    private static void copy_directory_recursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            copy_directory_recursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
    }

    private static string sanitize_name(string name)
    {
        return name.Replace('/', '_').Replace('@', '_').Replace(':', '_');
    }

    private static long get_directory_size(string path)
    {
        if (!Directory.Exists(path)) return 0;

        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    #endregion
}
