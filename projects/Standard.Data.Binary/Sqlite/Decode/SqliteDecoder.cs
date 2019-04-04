using Std.Data.Binary.Sqlite.Data;
using Std.Data.Binary.Sqlite.Scanner;

namespace Std.Data.Binary.Sqlite.Decode;

/// <summary>
///     SQLite 数据库文件解码器，将 .sqlite 二进制格式解码为 C# 数据结构的
/// </summary>
public sealed class SqliteDecoder
{
    /// <summary>
    ///     的.sqlite 二进制数据中解码文件头信息的
    /// </summary>
    /// <param name="data">
    ///     .sqlite 二进制数据的/param>
    ///     <returns>SQLite 文件头数据的/returns>
    public SqliteFileHeader decode_header(byte[] data)
    {
        var scanner = new SqliteScanner(data);

        if (!scanner.is_sqlite()) throw new InvalidDataException("数据不是有效的.sqlite 文件格式");

        return scanner.scan_header();
    }

    /// <summary>
    ///     的.sqlite 二进制数据中解码文件头信息的
    /// </summary>
    /// <param name="data">
    ///     .sqlite 二进制数据的/param>
    ///     <returns>SQLite 文件头数据的/returns>
    public SqliteFileHeader decode_header(ReadOnlySpan<byte> data)
    {
        var scanner = new SqliteScanner(data);

        if (!scanner.is_sqlite()) throw new InvalidDataException("数据不是有效的.sqlite 文件格式");

        return scanner.scan_header();
    }
}