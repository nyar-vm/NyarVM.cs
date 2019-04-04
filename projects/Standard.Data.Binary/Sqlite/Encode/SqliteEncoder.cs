using Std.Data.Binary.Frame;
using Std.Data.Binary.Sqlite.Data;

namespace Std.Data.Binary.Sqlite.Encode;

/// <summary>
///     SQLite 数据库文件编码器，将 C# 数据结构编码的.sqlite 二进制格式的
/// </summary>
public sealed class SqliteEncoder
{
    /// <summary>
    ///     的SQLite 文件头数据编码为 .sqlite 文件头二进制数据的
    /// </summary>
    /// <param name="header">
    ///     文件头数据的/param>
    ///     <returns>100 字节的.sqlite 文件头数据的/returns>
    public byte[] encode_header(SqliteFileHeader header)
    {
        var writer = new ByteBufferWriter(SqliteConstants.header_size);

        writer.write(SqliteConstants.magic_number);
        write_int16_big_endian(ref writer, (short)header.page_size);
        writer.write_u8(header.write_version);
        writer.write_u8(header.read_version);
        writer.write_u8(header.reserved_space);
        writer.write_u8(header.max_embedded_payload_fraction);
        writer.write_u8(header.min_embedded_payload_fraction);
        writer.write_u8(header.leaf_payload_fraction);
        write_int32_big_endian(ref writer, (int)header.file_change_counter);
        write_int32_big_endian(ref writer, (int)header.total_pages);
        write_int32_big_endian(ref writer, (int)header.first_freelist_trunk_page);
        write_int32_big_endian(ref writer, (int)header.total_freelist_pages);
        write_int32_big_endian(ref writer, (int)header.schema_cookie);
        write_int32_big_endian(ref writer, (int)header.schema_format_number);
        write_int32_big_endian(ref writer, (int)header.default_page_cache_size);
        write_int32_big_endian(ref writer, (int)header.largest_root_btree_page);
        write_int32_big_endian(ref writer, (int)header.text_encoding);
        write_int32_big_endian(ref writer, (int)header.user_version);
        write_int32_big_endian(ref writer, header.incremental_vacuum_mode ? 1 : 0);
        write_int32_big_endian(ref writer, (int)header.application_id);

        writer.write(new byte[20]);

        write_int32_big_endian(ref writer, (int)header.version_valid_for_number);
        write_int32_big_endian(ref writer, (int)header.sqlite_version_number);

        return writer.to_array();
    }

    private static void write_int16_big_endian(ref ByteBufferWriter writer, short value)
    {
        writer.write_u8((byte)((value >> 8) & 0xFF));
        writer.write_u8((byte)(value & 0xFF));
    }

    private static void write_int32_big_endian(ref ByteBufferWriter writer, int value)
    {
        writer.write_u8((byte)((value >> 24) & 0xFF));
        writer.write_u8((byte)((value >> 16) & 0xFF));
        writer.write_u8((byte)((value >> 8) & 0xFF));
        writer.write_u8((byte)(value & 0xFF));
    }
}