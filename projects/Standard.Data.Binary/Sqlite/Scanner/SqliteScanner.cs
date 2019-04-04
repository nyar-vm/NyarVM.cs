using Std.Data.Binary.Sqlite.Data;

namespace Std.Data.Binary.Sqlite.Scanner;

/// <summary>
///     SQLite 数据库文件扫描器，提供对 .sqlite 文件的快速元信息扫描的
/// </summary>
/// <remarks>
///     .sqlite 文件格式头部的100 字节，包含魔数、页大小、编码等元信息的
///     扫描器只读取头部元信息，不做完整的页遍历的
/// </remarks>
public ref struct SqliteScanner
{
    private ReadOnlySpan<byte> _data;

    /// <summary>
    ///     初始的<see cref="SqliteScanner" /> 结构的新实例的
    /// </summary>
    /// <param name="data">要扫描的 .sqlite 字节数据的/param>
    public SqliteScanner(ReadOnlySpan<byte> data)
    {
        _data = data;
    }

    /// <summary>
    ///     快速判断数据是否为 .sqlite 格式的
    /// </summary>
    public bool is_sqlite()
    {
        if (_data.Length < SqliteConstants.header_size) return false;

        return _data[..SqliteConstants.magic_number.Length].SequenceEqual(SqliteConstants.magic_number);
    }

    /// <summary>
    ///     扫描 .sqlite 文件头，提取数据库元信息的
    /// </summary>
    /// <returns>SQLite 文件头信息的/returns>
    public SqliteFileHeader scan_header()
    {
        if (_data.Length < SqliteConstants.header_size)
            throw new InvalidDataException($".sqlite 文件数据过短，期望至的{SqliteConstants.header_size} 字节");

        if (!is_sqlite()) throw new InvalidDataException(".sqlite 文件魔数不匹配，期望 \"SQLite format 3\\0\"");

        var offset = SqliteConstants.magic_number.Length;

        var pageSize = read_int16_big_endian(offset);
        offset += 2;

        var writeVersion = _data[offset++];
        var readVersion = _data[offset++];
        var reservedSpace = _data[offset++];
        var maxEmbeddedPayloadFraction = _data[offset++];
        var minEmbeddedPayloadFraction = _data[offset++];
        var leafPayloadFraction = _data[offset++];

        var fileChangeCounter = read_int32_big_endian(offset);
        offset += 4;

        var totalPages = read_int32_big_endian(offset);
        offset += 4;

        var firstFreelistTrunkPage = read_int32_big_endian(offset);
        offset += 4;

        var totalFreelistPages = read_int32_big_endian(offset);
        offset += 4;

        var schemaCookie = read_int32_big_endian(offset);
        offset += 4;

        var schemaFormatNumber = read_int32_big_endian(offset);
        offset += 4;

        var defaultPageCacheSize = read_int32_big_endian(offset);
        offset += 4;

        var largestRootBtreePage = read_int32_big_endian(offset);
        offset += 4;

        var textEncoding = (SqliteConstants.TextEncoding)read_int32_big_endian(offset);
        offset += 4;

        var userVersion = read_int32_big_endian(offset);
        offset += 4;

        var incrementalVacuumMode = read_int32_big_endian(offset) != 0;
        offset += 4;

        var applicationId = read_int32_big_endian(offset);
        offset += 4;

        offset += 20;

        var versionValidForNumber = read_int32_big_endian(offset);
        offset += 4;

        var sqliteVersionNumber = read_int32_big_endian(offset);

        return new SqliteFileHeader
        {
            page_size = pageSize,
            write_version = writeVersion,
            read_version = readVersion,
            reserved_space = reservedSpace,
            max_embedded_payload_fraction = maxEmbeddedPayloadFraction,
            min_embedded_payload_fraction = minEmbeddedPayloadFraction,
            leaf_payload_fraction = leafPayloadFraction,
            file_change_counter = fileChangeCounter,
            total_pages = totalPages,
            first_freelist_trunk_page = firstFreelistTrunkPage,
            total_freelist_pages = totalFreelistPages,
            schema_cookie = schemaCookie,
            schema_format_number = schemaFormatNumber,
            default_page_cache_size = defaultPageCacheSize,
            largest_root_btree_page = largestRootBtreePage,
            text_encoding = textEncoding,
            user_version = userVersion,
            incremental_vacuum_mode = incrementalVacuumMode,
            application_id = applicationId,
            version_valid_for_number = versionValidForNumber,
            sqlite_version_number = sqliteVersionNumber
        };
    }

    private short read_int16_big_endian(int offset)
    {
        return (short)((_data[offset] << 8) | _data[offset + 1]);
    }

    private int read_int32_big_endian(int offset)
    {
        return (_data[offset] << 24) | (_data[offset + 1] << 16) | (_data[offset + 2] << 8) | _data[offset + 3];
    }
}