using Std.Binary.Attributes;
using Std.Codec;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Gltf.Data;

/// <summary>
///     GLTF / GLB 二进制格式常量的
/// </summary>
/// <remarks>
///     所有常量值均来自 Khronos GLTF 2.0 规范，Acorn 独占二进制编解码职责�?
/// </remarks>
public static class GltfConstants
{
    /// <summary>
    ///     GLB 版本号（2）的
    /// </summary>
    public const uint glb_version = 2;

    /// <summary>
    ///     GLB JSON 块类型标识（"JSON" 的ASCII 小端序）�?
    /// </summary>
    public const uint chunk_type_json = 0x4E4F534A;

    /// <summary>
    ///     GLB BIN 块类型标识（"BIN\0" 的ASCII 小端序）�?
    /// </summary>
    public const uint chunk_type_bin = 0x004E4942;

    /// <summary>
    ///     GLB JSON 块填充字节（空格）的
    /// </summary>
    public const byte padding_byte = 0x20;

    /// <summary>
    ///     GLB 文件魔数的glTF"）的
    /// </summary>
    public static ReadOnlySpan<byte> glb_magic_number => "glTF"u8;
}

/// <summary>
///     GLTF 纹理过滤模式枚举�?
/// </summary>
public enum GltfFilterMode
{
    /// <summary>
    ///     最近邻采样�?
    /// </summary>
    nearest = 9728,

    /// <summary>
    ///     线性采样（默认）的
    /// </summary>
    linear = 9729,

    /// <summary>
    ///     最近邻 mipmap 最近邻采样�?
    /// </summary>
    nearest_mipmap_nearest = 9984,

    /// <summary>
    ///     线的mipmap 最近邻采样�?
    /// </summary>
    linear_mipmap_nearest = 9985,

    /// <summary>
    ///     最近邻 mipmap 线性采样的
    /// </summary>
    nearest_mipmap_linear = 9986,

    /// <summary>
    ///     线的mipmap 线性采样（默认）的
    /// </summary>
    linear_mipmap_linear = 9987
}

/// <summary>
///     GLTF 纹理环绕模式枚举�?
/// </summary>
public enum GltfWrapMode
{
    /// <summary>
    ///     钳制到边缘（默认）的
    /// </summary>
    clamp_to_edge = 33071,

    /// <summary>
    ///     镜像重复�?
    /// </summary>
    mirrored_repeat = 33648,

    /// <summary>
    ///     重复（默认）�?
    /// </summary>
    repeat = 10497
}

/// <summary>
///     GLTF 图元渲染模式枚举�?
/// </summary>
public enum GltfPrimitiveMode
{
    /// <summary>
    ///     点列表的
    /// </summary>
    points = 0,

    /// <summary>
    ///     线列表的
    /// </summary>
    lines = 1,

    /// <summary>
    ///     线环�?
    /// </summary>
    line_loop = 2,

    /// <summary>
    ///     线带�?
    /// </summary>
    line_strip = 3,

    /// <summary>
    ///     三角形列表（默认）的
    /// </summary>
    triangles = 4,

    /// <summary>
    ///     三角形带�?
    /// </summary>
    triangle_strip = 5,

    /// <summary>
    ///     三角形扇�?
    /// </summary>
    triangle_fan = 6
}

/// <summary>
///     GLB 文件头部�? 字节）的
/// </summary>
[BinarySerializable(endianness = Endianness.little_endian)]
public struct GlbHeader
{
    [Field(order = 0, length = 4)] public FixedBytes4 magic;

    [Field(order = 1)] public uint version;

    [Field(order = 2)] public uint length;

    public static bool try_read(ref ByteBuffer buffer, out GlbHeader header)
    {
        header = default;

        if (buffer.remaining < 12) return false;

        header.magic = FixedBytes4.from_span(buffer.read_bytes(4));
        header.version = buffer.read_u32_le();
        header.length = buffer.read_u32_le();
        return true;
    }

    public void write_to(ref ByteBufferWriter writer)
    {
        writer.write(magic.as_span());
        writer.write_u32_le(version);
        writer.write_u32_le(length);
    }
}

/// <summary>
///     GLB 块头部（8 字节）的
/// </summary>
[BinarySerializable]
public struct GlbChunkHeader
{
    [Field(order = 0)] public uint length;

    [Field(order = 1)] public uint type;

    public static bool try_read(ref ByteBuffer buffer, out GlbChunkHeader header)
    {
        header = default;

        if (buffer.remaining < 8) return false;

        header.length = buffer.read_u32_le();
        header.type = buffer.read_u32_le();
        return true;
    }

    public void write_to(ref ByteBufferWriter writer)
    {
        writer.write_u32_le(length);
        writer.write_u32_le(type);
    }
}