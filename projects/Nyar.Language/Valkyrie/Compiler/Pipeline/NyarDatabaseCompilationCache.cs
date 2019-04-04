using System.Security.Cryptography;
using System.Text;
using Std.Data.Binary.Frame;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Binary.NyarIR.Decode;
using Std.Data.Binary.NyarIR.Encode;

namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     基于 Nyar 数据库（.nyar 格式）的编译缓存实现。
///     以 Workspace 根目录的 .cache/ 为缓存根，按 canonical-triple 分桶存储。
/// </summary>
public sealed class NyarDatabaseCompilationCache : ICompilationCache
{
    private const string _token_bucket = "_tokens";

    private readonly NyarDecoder _decoder = new();
    private readonly NyarEncoder _encoder = new();

    /// <summary>
    ///     创建编译缓存实例
    /// </summary>
    /// <param name="workspaceDir">Workspace 根目录（包含 legions.von 的目录）</param>
    public NyarDatabaseCompilationCache(string workspaceDir)
    {
        cache_root = Path.Combine(workspaceDir, ".cache");
    }

    /// <summary>
    ///     缓存根目录
    /// </summary>
    public string cache_root { get; }

    #region Token 缓存

    /// <inheritdoc />
    public bool try_get_tokens(string filePath, string contentHash, out TokenCacheEntry? tokens)
    {
        var cacheKey = compute_combined_hash(filePath, contentHash);
        var cachePath = get_cache_path(_token_bucket, cacheKey);
        var data = read_entry(cachePath, "token");
        if (data is null)
        {
            tokens = null;
            return false;
        }

        tokens = deserialize_token_entry(data);
        return tokens is not null;
    }

    /// <inheritdoc />
    public void put_tokens(string filePath, string contentHash, TokenCacheEntry entry)
    {
        var cacheKey = compute_combined_hash(filePath, contentHash);
        var cachePath = get_cache_path(_token_bucket, cacheKey);
        var data = serialize_token_entry(entry);
        write_entry(cachePath, "token", data);
    }

    #endregion

    #region Staging 缓存

    /// <inheritdoc />
    public bool try_get_staging(string filePath, string canonicalTriple, string contentHash,
        out StageCacheEntry? stagedTokens)
    {
        var cacheKey = compute_combined_hash(filePath, canonicalTriple, contentHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = read_entry(cachePath, "staging");
        if (data is null)
        {
            stagedTokens = null;
            return false;
        }

        stagedTokens = deserialize_stage_entry(data);
        return stagedTokens is not null;
    }

    /// <inheritdoc />
    public void put_staging(string filePath, string canonicalTriple, string contentHash, StageCacheEntry entry)
    {
        var cacheKey = compute_combined_hash(filePath, canonicalTriple, contentHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = serialize_stage_entry(entry);
        write_entry(cachePath, "staging", data);
    }

    #endregion

    #region 语义缓存

    /// <inheritdoc />
    public bool try_get_semantics(string filePath, string canonicalTriple, string astHash,
        out SemanticCacheEntry? semantics)
    {
        var cacheKey = compute_combined_hash(filePath, canonicalTriple, astHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = read_entry(cachePath, "semantics");
        if (data is null)
        {
            semantics = null;
            return false;
        }

        semantics = deserialize_semantic_entry(data);
        return semantics is not null;
    }

    /// <inheritdoc />
    public void put_semantics(string filePath, string canonicalTriple, string astHash, SemanticCacheEntry entry)
    {
        var cacheKey = compute_combined_hash(filePath, canonicalTriple, astHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = serialize_semantic_entry(entry);
        write_entry(cachePath, "semantics", data);
    }

    #endregion

    #region IR 缓存

    /// <inheritdoc />
    public bool try_get_ir(string moduleName, string canonicalTriple, string irHash,
        out IrCacheEntry? irEntry)
    {
        var cacheKey = compute_combined_hash(moduleName, canonicalTriple, irHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = read_entry(cachePath, "ir");
        if (data is null)
        {
            irEntry = null;
            return false;
        }

        irEntry = deserialize_ir_entry(data);
        return irEntry is not null;
    }

    /// <inheritdoc />
    public void put_ir(string moduleName, string canonicalTriple, string irHash, IrCacheEntry entry)
    {
        var cacheKey = compute_combined_hash(moduleName, canonicalTriple, irHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = serialize_ir_entry(entry);
        write_entry(cachePath, "ir", data);
    }

    #endregion

    #region Entry Slice 缓存

    /// <inheritdoc />
    public bool try_get_entry_slice(string entryName, string canonicalTriple, string callGraphHash,
        out EntrySliceCacheEntry? entrySlice)
    {
        var cacheKey = compute_combined_hash(entryName, canonicalTriple, callGraphHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = read_entry(cachePath, "entry-slice");
        if (data is null)
        {
            entrySlice = null;
            return false;
        }

        entrySlice = deserialize_entry_slice_entry(data);
        return entrySlice is not null;
    }

    /// <inheritdoc />
    public void put_entry_slice(string entryName, string canonicalTriple, string callGraphHash,
        EntrySliceCacheEntry entry)
    {
        var cacheKey = compute_combined_hash(entryName, canonicalTriple, callGraphHash);
        var cachePath = get_cache_path(canonicalTriple, cacheKey);
        var data = serialize_entry_slice_entry(entry);
        write_entry(cachePath, "entry-slice", data);
    }

    #endregion

    #region 缓存失效

    /// <inheritdoc />
    public void invalidate(string canonicalTriple)
    {
        var bucket = get_bucket_dir(canonicalTriple);
        if (Directory.Exists(bucket)) Directory.Delete(bucket, true);
    }

    /// <inheritdoc />
    public void invalidate_all()
    {
        if (Directory.Exists(cache_root)) Directory.Delete(cache_root, true);
    }

    #endregion

    #region 序列化

    private static byte[] serialize_token_entry(TokenCacheEntry entry)
    {
        var writer = new ByteBufferWriter(1024 + entry.token_data.Length);
        write_string(ref writer, entry.content_hash);
        write_bytes(ref writer, entry.token_data);
        return writer.to_array();
    }

    private static TokenCacheEntry? deserialize_token_entry(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        var contentHash = read_string(ref buffer);
        var tokenData = read_bytes(ref buffer);
        return new TokenCacheEntry
        {
            content_hash = contentHash,
            token_data = tokenData,
            created_at = DateTimeOffset.UtcNow
        };
    }

    private static byte[] serialize_stage_entry(StageCacheEntry entry)
    {
        var writer = new ByteBufferWriter(1024 + entry.staged_token_data.Length);
        write_string(ref writer, entry.content_hash);
        write_string(ref writer, entry.canonical_triple);
        write_bytes(ref writer, entry.staged_token_data);
        return writer.to_array();
    }

    private static StageCacheEntry? deserialize_stage_entry(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        var contentHash = read_string(ref buffer);
        var canonicalTriple = read_string(ref buffer);
        var stagedData = read_bytes(ref buffer);
        return new StageCacheEntry
        {
            content_hash = contentHash,
            canonical_triple = canonicalTriple,
            staged_token_data = stagedData,
            created_at = DateTimeOffset.UtcNow
        };
    }

    private static byte[] serialize_semantic_entry(SemanticCacheEntry entry)
    {
        var writer = new ByteBufferWriter(1024 + entry.semantic_data.Length);
        write_string(ref writer, entry.ast_hash);
        write_string(ref writer, entry.canonical_triple);
        write_bytes(ref writer, entry.semantic_data);
        return writer.to_array();
    }

    private static SemanticCacheEntry? deserialize_semantic_entry(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        var astHash = read_string(ref buffer);
        var canonicalTriple = read_string(ref buffer);
        var semanticData = read_bytes(ref buffer);
        return new SemanticCacheEntry
        {
            ast_hash = astHash,
            canonical_triple = canonicalTriple,
            semantic_data = semanticData,
            created_at = DateTimeOffset.UtcNow
        };
    }

    private static byte[] serialize_ir_entry(IrCacheEntry entry)
    {
        var writer = new ByteBufferWriter(1024 + entry.ir_data.Length);
        write_string(ref writer, entry.ir_kind);
        write_string(ref writer, entry.ir_hash);
        write_string(ref writer, entry.canonical_triple);
        write_bytes(ref writer, entry.ir_data);
        return writer.to_array();
    }

    private static IrCacheEntry? deserialize_ir_entry(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        var irKind = read_string(ref buffer);
        var irHash = read_string(ref buffer);
        var canonicalTriple = read_string(ref buffer);
        var irData = read_bytes(ref buffer);
        return new IrCacheEntry
        {
            ir_kind = irKind,
            ir_hash = irHash,
            canonical_triple = canonicalTriple,
            ir_data = irData,
            created_at = DateTimeOffset.UtcNow
        };
    }

    private static byte[] serialize_entry_slice_entry(EntrySliceCacheEntry entry)
    {
        var nameBytes = Encoding.UTF8.GetBytes(entry.entry_name);
        var estimatedSize = 1024 + entry.reachable_functions.Sum(f => Encoding.UTF8.GetByteCount(f) + 4);
        var writer = new ByteBufferWriter(estimatedSize);
        write_string(ref writer, entry.entry_name);
        write_string(ref writer, entry.canonical_triple);
        write_string(ref writer, entry.call_graph_hash);
        writer.write_i32_le(entry.reachable_functions.Count);
        foreach (var func in entry.reachable_functions) write_string(ref writer, func);

        return writer.to_array();
    }

    private static EntrySliceCacheEntry? deserialize_entry_slice_entry(byte[] data)
    {
        var buffer = new ByteBuffer(data);
        var entryName = read_string(ref buffer);
        var canonicalTriple = read_string(ref buffer);
        var callGraphHash = read_string(ref buffer);
        var count = buffer.read_i32_le();
        var reachableFunctions = new string[count];
        for (var i = 0; i < count; i++) reachableFunctions[i] = read_string(ref buffer);

        return new EntrySliceCacheEntry
        {
            entry_name = entryName,
            canonical_triple = canonicalTriple,
            call_graph_hash = callGraphHash,
            reachable_functions = reachableFunctions,
            created_at = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region .nyar 文件读写

    private byte[]? read_entry(string cachePath, string expectedType)
    {
        if (!File.Exists(cachePath)) return null;

        try
        {
            var rawBytes = File.ReadAllBytes(cachePath);
            var moduleData = _decoder.decode(rawBytes);
            if (!string.Equals(moduleData.name, expectedType, StringComparison.Ordinal)) return null;

            return moduleData.code_bytes;
        }
        catch
        {
            return null;
        }
    }

    private void write_entry(string cachePath, string entryType, byte[] data)
    {
        var bucketDir = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrEmpty(bucketDir)) Directory.CreateDirectory(bucketDir);

        var moduleData = new NyarModuleData
        {
            name = entryType,
            code_bytes = data
        };

        var encoded = _encoder.encode(moduleData);
        File.WriteAllBytes(cachePath, encoded);
    }

    #endregion

    #region 工具方法

    private string get_bucket_dir(string canonicalTriple)
    {
        var safeTriple = canonicalTriple
            .Replace('-', '_')
            .Replace(' ', '_');
        return Path.Combine(cache_root, safeTriple);
    }

    private string get_cache_path(string canonicalTriple, string hash)
    {
        var bucket = get_bucket_dir(canonicalTriple);
        return Path.Combine(bucket, $"{hash}.nyar");
    }

    /// <summary>
    ///     计算多个字符串的组合 SHA256 哈希
    /// </summary>
    private static string compute_combined_hash(params string[] inputs)
    {
        var combined = string.Join("\0", inputs);
        var bytes = Encoding.UTF8.GetBytes(combined);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static void write_string(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.write_i32_le(bytes.Length);
        writer.write(bytes);
    }

    private static string read_string(ref ByteBuffer buffer)
    {
        var length = buffer.read_i32_le();
        var bytes = buffer.read_bytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void write_bytes(ref ByteBufferWriter writer, byte[] data)
    {
        writer.write_i32_le(data.Length);
        writer.write(data);
    }

    private static byte[] read_bytes(ref ByteBuffer buffer)
    {
        var length = buffer.read_i32_le();
        var span = buffer.read_bytes(length);
        return [.. span];
    }

    /// <summary>
    ///     计算文件的 SHA256 内容哈希
    /// </summary>
    public static string compute_file_hash(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    ///     计算多个文件的组合内容哈希
    /// </summary>
    public static string compute_files_hash(IReadOnlyList<string> filePaths)
    {
        var orderedPaths = filePaths
            .Select(p => Path.GetFullPath(p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var combined = new StringBuilder();
        foreach (var path in orderedPaths)
        {
            combined.Append(path);
            combined.Append('\0');
            combined.Append(compute_file_hash(path));
            combined.Append('\0');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(combined.ToString())));
    }

    #endregion
}