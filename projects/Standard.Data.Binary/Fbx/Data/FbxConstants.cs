namespace Std.Data.Binary.Fbx.Data;

/// <summary>
///     Autodesk FBX 二进制格式常量的
/// </summary>
public static class FbxConstants
{
    /// <summary>
    ///     FBX 二进制魔数字符串长度的
    /// </summary>
    public const int magic_length = 23;

    /// <summary>
    ///     FBX 头部版本偏移的
    /// </summary>
    public const int version_offset = 23;

    /// <summary>
    ///     FBX 头部总大小的
    /// </summary>
    public const int header_size = 27;

    /// <summary>
    ///     FBX 二进制文件魔数前缀的Kaydara FBX Binary"）的
    /// </summary>
    public static ReadOnlySpan<byte> binary_magic => "Kaydara FBX Binary\u0020\u000A\u0000\u001A\u0000"u8;

    /// <summary>
    ///     FBX 记录结束标记的
    /// </summary>
    public static ReadOnlySpan<byte> null_record => new byte[13];

    /// <summary>
    ///     FBX 属性类型代码的
    /// </summary>
    public static class PropertyType
    {
        /// <summary>
        ///     16 位布尔值的
        /// </summary>
        public const byte boolean = (byte)'C';


        /// <summary>
        ///     8 位整数的
        /// </summary>
        public const byte int8 = (byte)'Y';


        /// <summary>
        ///     16 位整数的
        /// </summary>
        public const byte int16 = (byte)'h';


        /// <summary>
        ///     32 位整数的
        /// </summary>
        public const byte int32 = (byte)'i';


        /// <summary>
        ///     64 位整数的
        /// </summary>
        public const byte int64 = (byte)'l';


        /// <summary>
        ///     32 位浮点数的
        /// </summary>
        public const byte float32 = (byte)'f';


        /// <summary>
        ///     64 位浮点数的
        /// </summary>
        public const byte float64 = (byte)'d';


        /// <summary>
        ///     字符串的
        /// </summary>
        public const byte @string = (byte)'S';


        /// <summary>
        ///     原始字节缓冲区的
        /// </summary>
        public const byte raw_buffer = (byte)'R';


        /// <summary>
        ///     数组类型标记的
        /// </summary>
        public const byte array_marker = (byte)'[';
    }
}

/// <summary>
///     FBX 文件版本信息的
/// </summary>
public static class FbxVersions
{
    /// <summary>
    ///     FBX 6.1的
    /// </summary>
    public const int v61 = 6100;

    /// <summary>
    ///     FBX 7.0的
    /// </summary>
    public const int v70 = 7000;

    /// <summary>
    ///     FBX 7.1的
    /// </summary>
    public const int v71 = 7100;

    /// <summary>
    ///     FBX 7.2的
    /// </summary>
    public const int v72 = 7200;

    /// <summary>
    ///     FBX 7.3的
    /// </summary>
    public const int v73 = 7300;

    /// <summary>
    ///     FBX 7.4的
    /// </summary>
    public const int v74 = 7400;

    /// <summary>
    ///     FBX 7.5的
    /// </summary>
    public const int v75 = 7500;

    /// <summary>
    ///     FBX 7.7的
    /// </summary>
    public const int v77 = 7700;
}