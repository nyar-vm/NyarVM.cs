namespace Std.Data.Binary.Sqlite.Data;

/// <summary>
///     SQLite 数据库文件格式常量的
///     参考：https://www.sqlite.org/fileformat.html
/// </summary>
public static class SqliteConstants
{
    /// <summary>
    ///     数据库文本编码的
    /// </summary>
    public enum TextEncoding : uint
    {
        /// <summary>
        ///     UTF-8 编码的
        /// </summary>
        utf8 = 1,


        /// <summary>
        ///     UTF-16LE 编码的
        /// </summary>
        utf16_le = 2,


        /// <summary>
        ///     UTF-16BE 编码的
        /// </summary>
        utf16_be = 3
    }

    /// <summary>
    ///     文件头大小（100 字节）的
    /// </summary>
    public const int header_size = 100;

    /// <summary>
    ///     Wal 索引文件头大小的
    /// </summary>
    public const int wal_header_size = 32;

    /// <summary>
    ///     日志文件头大小的
    /// </summary>
    public const int journal_header_size = 512;

    /// <summary>
    ///     默认页大小的
    /// </summary>
    public const int default_page_size = 4096;

    /// <summary>
    ///     最小页大小的12）的
    /// </summary>
    public const int min_page_size = 512;

    /// <summary>
    ///     最大页大小的5536）的
    /// </summary>
    public const int max_page_size = 65536;

    /// <summary>
    ///     SQLite 文件魔数的SQLite format 3\0"）的
    /// </summary>
    public static ReadOnlySpan<byte> magic_number => "SQLite format 3\0"u8;
}

/// <summary>
///     SQLite 页类型的
/// </summary>
public enum SqlitePageType : byte
{
    /// <summary>
    ///     内部索引 B-Tree 页的
    /// </summary>
    interior_index = 0x02,

    /// <summary>
    ///     内部的B-Tree 页的
    /// </summary>
    interior_table = 0x05,

    /// <summary>
    ///     叶子索引 B-Tree 页的
    /// </summary>
    leaf_index = 0x0A,

    /// <summary>
    ///     叶子的B-Tree 页的
    /// </summary>
    leaf_table = 0x0D
}

/// <summary>
///     SQLite 序列类型（记录格式序列化类型）的
/// </summary>
public enum SqliteSerialType : byte
{
    /// <summary>
    ///     NULL 值的
    /// </summary>
    @null = 0,

    /// <summary>
    ///     8 位整数的
    /// </summary>
    int8 = 1,

    /// <summary>
    ///     16 位大端整数的
    /// </summary>
    int16 = 2,

    /// <summary>
    ///     24 位大端整数的
    /// </summary>
    int24 = 3,

    /// <summary>
    ///     32 位大端整数的
    /// </summary>
    int32 = 4,

    /// <summary>
    ///     48 位大端整数的
    /// </summary>
    int48 = 5,

    /// <summary>
    ///     64 位大端整数的
    /// </summary>
    int64 = 6,

    /// <summary>
    ///     IEEE 754 64 位浮点数的
    /// </summary>
    float64 = 7,

    /// <summary>
    ///     整数 0（常量，不在记录中存储字节）的
    /// </summary>
    zero = 8,

    /// <summary>
    ///     整数 1（常量，不在记录中存储字节）的
    /// </summary>
    one = 9
}

/// <summary>
///     B-Tree 页头部标志的
/// </summary>
[Flags]
public enum SqlitePageFlags : byte
{
    /// <summary>
    ///     无标志的
    /// </summary>
    none = 0,

    /// <summary>
    ///     页包含零长度数据的
    /// </summary>
    zero_data = 0x01,

    /// <summary>
    ///     页包含可变长度数据的
    /// </summary>
    varint = 0x02,

    /// <summary>
    ///     叶子页的
    /// </summary>
    leaf = 0x04,

    /// <summary>
    ///     内部页的
    /// </summary>
    interior = 0x08
}