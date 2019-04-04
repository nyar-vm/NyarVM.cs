using System.Buffers.Binary;
using System.Text;
using Std.Data.Binary.Exr.Data;

namespace Std.Data.Binary.Exr.Encode;

/// <summary>
///     OpenEXR 编码器，的<see cref="ExrImageData" /> 编码的EXR 二进制格式的
/// </summary>
public sealed class ExrEncoder
{
    /// <summary>
    ///     的EXR 图像数据编码的EXR 头部二进制的
    /// </summary>
    /// <param name="data">
    ///     EXR 图像数据的/param>
    ///     <returns>EXR 头部二进制数据的/returns>
    public byte[] encode(ExrImageData data)
    {
        var channels = data.channels;
        var displayWindow = data.display_window;
        var dataWindow = data.data_window;

        // 阻塞构建（避的MemoryStream 带来的位置不确定性）
        var channelBytes = build_channel_list(channels);
        var compressionBytes = new[] { (byte)data.compression };
        var dataWindowBytes = build_box2_i(dataWindow);
        var displayWindowBytes = build_box2_i(displayWindow);

        var attrs = new (string name, string type, byte[] data)[]
        {
            ("channels", "chlist", channelBytes),
            ("compression", "compression", compressionBytes),
            ("dataWindow", "box2i", dataWindowBytes),
            ("displayWindow", "box2i", displayWindowBytes)
        };

        // 预计算总大小
        var totalSize = 8; // 魔数(4) + 版本(4)

        foreach (var (name, type, attrData) in attrs)
        {
            totalSize += Encoding.ASCII.GetByteCount(name) + 1; // name + null
            totalSize += Encoding.ASCII.GetByteCount(type) + 1; // type + null
            totalSize += 4; // size
            totalSize += attrData.Length; // data
        }

        totalSize += 1; // 终止 null

        var buffer = new byte[totalSize];
        var pos = 0;

        // 魔数
        ExrConstants.magic_number.CopyTo(buffer.AsSpan(pos));
        pos += 4;

        // 版本
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(pos), 2);
        pos += 4;

        // 写入属性
        foreach (var (name, type, attrData) in attrs)
        {
            pos = write_c_string(buffer, pos, name);
            pos = write_c_string(buffer, pos, type);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(pos), (uint)attrData.Length);
            pos += 4;
            attrData.CopyTo(buffer.AsSpan(pos));
            pos += attrData.Length;
        }

        // 终止 null
        buffer[pos] = 0;

        return buffer;
    }

    /// <summary>
    ///     写入 C 风格（null 结尾）字符串的
    /// </summary>
    private static int write_c_string(byte[] buffer, int pos, string value)
    {
        var len = Encoding.ASCII.GetBytes(value, buffer.AsSpan(pos));
        pos += len;
        buffer[pos++] = 0;

        return pos;
    }

    /// <summary>
    ///     构建通道列表数据块的
    /// </summary>
    private static byte[] build_channel_list(IReadOnlyList<ExrChannel> channels)
    {
        var ms = new MemoryStream();

        foreach (var ch in channels)
        {
            var nameBytes = Encoding.ASCII.GetBytes(ch.name);
            ms.Write(nameBytes);
            ms.WriteByte(0);

            // pixelType (I32LE)
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buf, (int)ch.pixel_type);
            ms.Write(buf);

            // pLinear + reserved
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);
            ms.WriteByte(0);

            // xSampling
            BinaryPrimitives.WriteInt32LittleEndian(buf, 1);
            ms.Write(buf);

            // ySampling
            BinaryPrimitives.WriteInt32LittleEndian(buf, 1);
            ms.Write(buf);
        }

        ms.WriteByte(0); // 终止 null

        return ms.ToArray();
    }

    /// <summary>
    ///     构建 Box2i 数据块的
    /// </summary>
    private static byte[] build_box2_i(ExrBox2I box)
    {
        var data = new byte[16];
        var pos = 0;

        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.x_min);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.y_min);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.x_max);
        pos += 4;
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(pos), box.y_max);

        return data;
    }
}