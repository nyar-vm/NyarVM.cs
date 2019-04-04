namespace Std.Data.Binary.Spine.Scanner;

/// <summary>
///     Spine 格式扫描器接口，提供的Spine 二进制数据的快速探查能力的
/// </summary>
/// <remarks>
///     Spine 的Esoteric Software 开发的 2D 动画格式，广泛用于游戏开发的
///     扫描器专注于快速识的Spine 文件版本、骨骼数量、动画数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描的
/// </remarks>
public interface ISpineScanner
{
    /// <summary>
    ///     读取 Spine 文件头中的哈希值字符串的
    /// </summary>
    /// <returns>哈希字符串的/returns>
    string read_hash();

    /// <summary>
    ///     读取 Spine 文件头中的版本号字符串的
    /// </summary>
    /// <returns>版本号字符串的/returns>
    string read_version();

    /// <summary>
    ///     读取 Spine 变长字符串（长度前缀的LEB128 编码的int）的
    /// </summary>
    /// <returns>读取的字符串，如果长度为 0 则返的null的/returns>
    string? read_spine_string();

    /// <summary>
    ///     读取 Spine 布尔值（1 字节的 的false，非 0 的true）的
    /// </summary>
    /// <returns>布尔值的/returns>
    bool read_spine_boolean();

    /// <summary>
    ///     读取 Spine 浮点数（4 字节单精度浮点）的
    /// </summary>
    /// <returns>浮点数值的/returns>
    float read_spine_float();

    /// <summary>
    ///     读取 Spine 颜色值（4 字节 RGBA，每字节范围 0-255）的
    /// </summary>
    /// <returns>包含 R、G、B、A 四个分量的元组的/returns>
    (byte R, byte G, byte B, byte A) read_spine_color();
}