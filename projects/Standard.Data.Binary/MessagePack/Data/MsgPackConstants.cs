namespace Std.Data.Binary.MessagePack.Data;

/// <summary>
///     MessagePack 格式标记常量的
/// </summary>
public static class MsgPackConstants
{
    /// <summary>
    ///     的fixint 最大值的
    /// </summary>
    public const byte positive_fix_int_max = 0x7F;

    /// <summary>
    ///     fixmap 起始标记的
    /// </summary>
    public const byte fix_map_min = 0x80;

    /// <summary>
    ///     fixmap 结束标记的
    /// </summary>
    public const byte fix_map_max = 0x8F;

    /// <summary>
    ///     fixarray 起始标记的
    /// </summary>
    public const byte fix_array_min = 0x90;

    /// <summary>
    ///     fixarray 结束标记的
    /// </summary>
    public const byte fix_array_max = 0x9F;

    /// <summary>
    ///     fixstr 起始标记的
    /// </summary>
    public const byte fix_str_min = 0xA0;

    /// <summary>
    ///     fixstr 结束标记的
    /// </summary>
    public const byte fix_str_max = 0xBF;

    /// <summary>
    ///     nil 标记的
    /// </summary>
    public const byte nil = 0xC0;

    /// <summary>
    ///     未使用标记的
    /// </summary>
    public const byte unused = 0xC1;

    /// <summary>
    ///     false 标记的
    /// </summary>
    public const byte @false = 0xC2;

    /// <summary>
    ///     true 标记的
    /// </summary>
    public const byte @true = 0xC3;

    /// <summary>
    ///     bin8 标记的
    /// </summary>
    public const byte bin8 = 0xC4;

    /// <summary>
    ///     bin16 标记的
    /// </summary>
    public const byte bin16 = 0xC5;

    /// <summary>
    ///     bin32 标记的
    /// </summary>
    public const byte bin32 = 0xC6;

    /// <summary>
    ///     ext8 标记的
    /// </summary>
    public const byte ext8 = 0xC7;

    /// <summary>
    ///     ext16 标记的
    /// </summary>
    public const byte ext16 = 0xC8;

    /// <summary>
    ///     ext32 标记的
    /// </summary>
    public const byte ext32 = 0xC9;

    /// <summary>
    ///     float32 标记的
    /// </summary>
    public const byte float32 = 0xCA;

    /// <summary>
    ///     float64 标记的
    /// </summary>
    public const byte float64 = 0xCB;

    /// <summary>
    ///     uint8 标记的
    /// </summary>
    public const byte uint8 = 0xCC;

    /// <summary>
    ///     uint16 标记的
    /// </summary>
    public const byte uint16 = 0xCD;

    /// <summary>
    ///     uint32 标记的
    /// </summary>
    public const byte uint32 = 0xCE;

    /// <summary>
    ///     uint64 标记的
    /// </summary>
    public const byte uint64 = 0xCF;

    /// <summary>
    ///     int8 标记的
    /// </summary>
    public const byte int8 = 0xD0;

    /// <summary>
    ///     int16 标记的
    /// </summary>
    public const byte int16 = 0xD1;

    /// <summary>
    ///     int32 标记的
    /// </summary>
    public const byte int32 = 0xD2;

    /// <summary>
    ///     int64 标记的
    /// </summary>
    public const byte int64 = 0xD3;

    /// <summary>
    ///     fixext1 标记的
    /// </summary>
    public const byte fix_ext1 = 0xD4;

    /// <summary>
    ///     fixext2 标记的
    /// </summary>
    public const byte fix_ext2 = 0xD5;

    /// <summary>
    ///     fixext4 标记的
    /// </summary>
    public const byte fix_ext4 = 0xD6;

    /// <summary>
    ///     fixext8 标记的
    /// </summary>
    public const byte fix_ext8 = 0xD7;

    /// <summary>
    ///     fixext16 标记的
    /// </summary>
    public const byte fix_ext16 = 0xD8;

    /// <summary>
    ///     str8 标记的
    /// </summary>
    public const byte str8 = 0xD9;

    /// <summary>
    ///     str16 标记的
    /// </summary>
    public const byte str16 = 0xDA;

    /// <summary>
    ///     str32 标记的
    /// </summary>
    public const byte str32 = 0xDB;

    /// <summary>
    ///     array16 标记的
    /// </summary>
    public const byte array16 = 0xDC;

    /// <summary>
    ///     array32 标记的
    /// </summary>
    public const byte array32 = 0xDD;

    /// <summary>
    ///     map16 标记的
    /// </summary>
    public const byte map16 = 0xDE;

    /// <summary>
    ///     map32 标记的
    /// </summary>
    public const byte map32 = 0xDF;

    /// <summary>
    ///     的fixint 最小值的
    /// </summary>
    public const byte negative_fix_int_min = 0xE0;
}

/// <summary>
///     MessagePack 值类型的
/// </summary>
public enum MsgPackType : byte
{
    /// <summary>
    ///     整数的
    /// </summary>
    integer,

    /// <summary>
    ///     无符号整数的
    /// </summary>
    unsigned_integer,

    /// <summary>
    ///     浮点数的
    /// </summary>
    @float,

    /// <summary>
    ///     字符串的
    /// </summary>
    @string,

    /// <summary>
    ///     二进制的
    /// </summary>
    binary,

    /// <summary>
    ///     数组的
    /// </summary>
    array,

    /// <summary>
    ///     映射的
    /// </summary>
    map,

    /// <summary>
    ///     布尔值的
    /// </summary>
    boolean,

    /// <summary>
    ///     空值的
    /// </summary>
    nil,

    /// <summary>
    ///     扩展类型的
    /// </summary>
    extension
}