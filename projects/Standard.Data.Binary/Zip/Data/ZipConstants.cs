namespace Std.Data.Binary.Zip.Data;

/// <summary>
///     ZIP 归档格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 PKWARE APPNOTE 规范，Acorn 独占二进制编解码职责的
/// </remarks>
public static class ZipConstants
{
    /// <summary>
    ///     本地文件头魔数（0x04034b50的PK\x03\x04"）的
    /// </summary>
    public const uint local_file_header_magic = 0x04034b50;

    /// <summary>
    ///     中央目录文件头魔数（0x02014b50的PK\x01\x02"）的
    /// </summary>
    public const uint central_directory_header_magic = 0x02014b50;

    /// <summary>
    ///     中央目录结束记录魔数的x06054b50的PK\x05\x06"）的
    /// </summary>
    public const uint end_of_central_directory_magic = 0x06054b50;

    /// <summary>
    ///     EOCD 记录最大搜索范围（64KB）的
    /// </summary>
    public const int max_eocd_search_size = 65536;
}