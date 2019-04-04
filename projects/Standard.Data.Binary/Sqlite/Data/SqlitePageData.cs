namespace Std.Data.Binary.Sqlite.Data;

/// <summary>
///     SQLite 数据库文件头数据的
/// </summary>
public sealed class SqliteFileHeader
{
    /// <summary>
    ///     页大小（字节数，必须的2 的幂的12-65536）的
    /// </summary>
    public int page_size { get; init; } = SqliteConstants.default_page_size;

    /// <summary>
    ///     文件格式写版本的
    /// </summary>
    public byte write_version { get; init; } = 1;

    /// <summary>
    ///     文件格式读版本的
    /// </summary>
    public byte read_version { get; init; } = 1;

    /// <summary>
    ///     每页未使用空间预留字节数的
    /// </summary>
    public byte reserved_space { get; init; }

    /// <summary>
    ///     最大嵌入载荷比例（默认的64，即 25%）的
    /// </summary>
    public byte max_embedded_payload_fraction { get; init; } = 64;

    /// <summary>
    ///     最小嵌入载荷比例（默认的32，即 12.5%）的
    /// </summary>
    public byte min_embedded_payload_fraction { get; init; } = 32;

    /// <summary>
    ///     叶子载荷比例（默认为 32，即 12.5%）的
    /// </summary>
    public byte leaf_payload_fraction { get; init; } = 32;

    /// <summary>
    ///     文件变更计数器的
    /// </summary>
    public long file_change_counter { get; init; }

    /// <summary>
    ///     数据库总页数的
    /// </summary>
    public long total_pages { get; init; }

    /// <summary>
    ///     首页 freelist trunk 页号的
    /// </summary>
    public long first_freelist_trunk_page { get; init; }

    /// <summary>
    ///     freelist 页总数的
    /// </summary>
    public long total_freelist_pages { get; init; }

    /// <summary>
    ///     schema cookie的
    /// </summary>
    public long schema_cookie { get; init; }

    /// <summary>
    ///     schema 格式号（支持 schema 格式 1-4）的
    /// </summary>
    public long schema_format_number { get; init; } = 4;

    /// <summary>
    ///     默认页缓存大小的
    /// </summary>
    public long default_page_cache_size { get; init; }

    /// <summary>
    ///     最大根 B-Tree 页号（auto-vacuum 模式下使用）的
    /// </summary>
    public long largest_root_btree_page { get; init; }

    /// <summary>
    ///     数据库文本编码的
    /// </summary>
    public SqliteConstants.TextEncoding text_encoding { get; init; } = SqliteConstants.TextEncoding.utf8;

    /// <summary>
    ///     user_version（用户自定义版本号）的
    /// </summary>
    public long user_version { get; init; }

    /// <summary>
    ///     incremental_vacuum 模式标志的
    /// </summary>
    public bool incremental_vacuum_mode { get; init; }

    /// <summary>
    ///     应用 ID的
    /// </summary>
    public long application_id { get; init; }

    /// <summary>
    ///     版本有效号（用于检测文件格式变化）的
    /// </summary>
    public long version_valid_for_number { get; init; }

    /// <summary>
    ///     SQLite 版本号（编译时版本号 × 1000000）的
    /// </summary>
    public long sqlite_version_number { get; init; }
}

/// <summary>
///     SQLite 页数据基类的
/// </summary>
public abstract class SqlitePageData
{
    /// <summary>
    ///     页号（从 1 开始）的
    /// </summary>
    public long page_number { get; init; }

    /// <summary>
    ///     页类型的
    /// </summary>
    public SqlitePageType page_type { get; init; }

    /// <summary>
    ///     第一的freeblock 偏移量的
    /// </summary>
    public int first_freeblock { get; init; }

    /// <summary>
    ///     页中 cell 数量的
    /// </summary>
    public int cell_count { get; init; }

    /// <summary>
    ///     cell 内容区域起始偏移量的
    /// </summary>
    public int cell_content_start { get; init; }

    /// <summary>
    ///     页中 fragmented free bytes 数量的
    /// </summary>
    public byte fragmented_free_bytes { get; init; }
}