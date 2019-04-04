using System.Text.Json;

namespace Nyar.PackageManager.Build;

/// <summary>
///     文件级构建缓存。
///     基于源码 SHA256 哈希判断缓存命中，存储构建元数据与产物信息。
/// </summary>
[Obsolete("请使用 Nyar.Language.Valkyrie.Compiler.Pipeline.NyarDatabaseCompilationCache 代替")]
public sealed class BuildCache
{
    private readonly string _cache_directory;

    /// <summary>
    ///     初始化构建缓存
    /// </summary>
    /// <param name="cacheDirectory">缓存目录</param>
    public BuildCache(string cacheDirectory)
    {
        _cache_directory = cacheDirectory;
        Directory.CreateDirectory(_cache_directory);
    }

    /// <summary>
    ///     尝试从缓存获取构建结果
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="result">构建结果（命中时输出）</param>
    /// <returns>是否命中</returns>
    public bool try_get(BuildCacheKey key, out BuildResult result)
    {
        var cacheFilePath = Path.Combine(_cache_directory, key.to_file_name());
        if (!File.Exists(cacheFilePath))
        {
            result = default!;
            return false;
        }

        try
        {
            var json = File.ReadAllText(cacheFilePath);
            var cached = JsonSerializer.Deserialize<CachedBuildEntry>(json);
            if (cached is null)
            {
                result = default!;
                return false;
            }

            result = new BuildResult
            {
                success = true,
                module_name = cached.module_name,
                canonical_triple = cached.canonical_triple,
                output_directory = cached.output_directory,
                elapsed = cached.elapsed
            };
            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }

    /// <summary>
    ///     存储构建结果到缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="result">构建结果</param>
    public void store(BuildCacheKey key, BuildResult result)
    {
        var entry = new CachedBuildEntry
        {
            module_name = result.module_name,
            canonical_triple = result.canonical_triple,
            output_directory = result.output_directory ?? string.Empty,
            elapsed = result.elapsed
        };

        var json = JsonSerializer.Serialize(entry);
        var cacheFilePath = Path.Combine(_cache_directory, key.to_file_name());
        File.WriteAllText(cacheFilePath, json);
    }

    /// <summary>
    ///     清除所有缓存
    /// </summary>
    public void clear()
    {
        if (Directory.Exists(_cache_directory))
        {
            Directory.Delete(_cache_directory, true);
            Directory.CreateDirectory(_cache_directory);
        }
    }

    /// <summary>
    ///     缓存的构建条目（仅存储元数据，不存储产物内容）
    /// </summary>
    private sealed class CachedBuildEntry
    {
        public string module_name { get; init; } = string.Empty;
        public string canonical_triple { get; init; } = string.Empty;
        public string output_directory { get; init; } = string.Empty;
        public TimeSpan elapsed { get; init; }
    }
}