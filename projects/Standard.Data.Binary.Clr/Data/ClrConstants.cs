namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CLR 二进制格式常量（ECMA-335 标准）的
/// </summary>
public static class ClrConstants
{
    /// <summary>
    ///     CLR 元数据签名（"BSJB" = 0x424A5342）的
    /// </summary>
    public const uint metadata_signature = 0x424A5342;

    /// <summary>
    ///     CLR 目录头大小（72 字节）的
    /// </summary>
    public const int clr_directory_size = 72;

    /// <summary>
    ///     CLR 元数据头最小大小的
    /// </summary>
    public const int metadata_header_min_size = 16;

    /// <summary>
    ///     方法的Tiny 格式标志（第 0-1 的= 0x02）的
    /// </summary>
    public const byte method_header_tiny_flag = 0x02;

    /// <summary>
    ///     方法的Fat 格式标志（第 0-1 的= 0x03）的
    /// </summary>
    public const byte method_header_fat_flag = 0x03;

    /// <summary>
    ///     Fat 方法的 InitLocals 标志（第 4 位 = 0x10），
    ///     设置时 CLR 自动将局部变量初始化为零值，可验证方法必须设置此标志。
    /// </summary>
    public const byte method_header_init_locals = 0x10;

    /// <summary>
    ///     方法头格式掩码的
    /// </summary>
    public const byte method_header_format_mask = 0x03;

    /// <summary>
    ///     Fat 方法的MoreSects 标志的
    /// </summary>
    public const byte method_header_more_sects = 0x08;

    /// <summary>
    ///     异常处理的EHTable 标志的
    /// </summary>
    public const byte exception_handler_table_flag = 0x01;

    /// <summary>
    ///     异常处理的Fat 格式标志的
    /// </summary>
    public const byte exception_handler_fat_flag = 0x40;

    /// <summary>
    ///     元数据表最大数量的
    /// </summary>
    public const int table_count = 64;

    /// <summary>
    ///     双字节操作码前缀字节（所的>= 0xFE00 的操作码以此字节开头）的
    /// </summary>
    public const byte two_byte_opcode_prefix = 0xFE;

    /// <summary>
    ///     双字节操作码基值（操作的>= 此值使用双字节编码）的
    /// </summary>
    public const ushort two_byte_opcode_base = 0xFE00;

    /// <summary>
    ///     #Strings 流名称的
    /// </summary>
    public const string strings_stream_name = "#Strings";

    /// <summary>
    ///     #Blob 流名称的
    /// </summary>
    public const string blob_stream_name = "#Blob";

    /// <summary>
    ///     #GUID 流名称的
    /// </summary>
    public const string guid_stream_name = "#GUID";

    /// <summary>
    ///     #US 流名称（用户字符串堆）的
    /// </summary>
    public const string user_string_stream_name = "#US";

    /// <summary>
    ///     #~ 流名称（表流）的
    /// </summary>
    public const string table_stream_name = "#~";

    /// <summary>
    ///     #- 流名称（未优化的表流）的
    /// </summary>
    public const string unoptimized_table_stream_name = "#-";
}