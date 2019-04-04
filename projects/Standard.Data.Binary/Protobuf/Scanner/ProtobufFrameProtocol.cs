using Std.Data.Binary.Frame;
using AcornFrame = Std.Data.Binary.Frame.Frame;

namespace Std.Data.Binary.Protobuf.Scanner;

/// <summary>
///     Protobuf 长度前缀消息帧协议，实现 <see cref="IFrameProtocol" /> 以支持基的VarInt 长度前缀的消息帧扫描的
/// </summary>
/// <remarks>
///     Protobuf 长度前缀消息使用 VarInt 编码的消息长度作为帧头，
///     帧结构为 [VarInt 长度][载荷数据]的
/// </remarks>
public struct ProtobufFrameProtocol : IFrameProtocol
{
    /// <summary>
    ///     最小帧大小的 字节 VarInt 长度前缀）的
    /// </summary>
    public int min_frame_size => 1;

    /// <summary>
    ///     尝试从缓冲区预读 Protobuf 长度前缀帧的大小的
    /// </summary>
    /// <param name="buffer">
    ///     从当前位置开始的剩余数据的/param>
    ///     <param name="frameSize">
    ///         如果成功则输出帧大小（字节），包含帧头的/param>
    ///         <returns>如果剩余数据中有足够数据确定帧大小则返回 true的/returns>
    public bool try_peek_frame_size(ReadOnlySpan<byte> buffer, out int frameSize)
    {
        frameSize = 0;

        if (!try_read_leb128_u32(buffer, out var length, out var headerSize)) return false;

        if (length > int.MaxValue - headerSize) return false;

        frameSize = headerSize + (int)length;
        return true;
    }

    /// <summary>
    ///     尝试从缓冲区读取 Protobuf 长度前缀帧的
    /// </summary>
    /// <param name="buffer">
    ///     从当前位置开始的剩余数据的/param>
    ///     <param name="frame">
    ///         如果成功则输出帧数据的/param>
    ///         <returns>如果成功读取一个完整帧则返的true的/returns>
    public bool try_read_frame(ReadOnlySpan<byte> buffer, out AcornFrame frame)
    {
        frame = default;

        if (!try_read_leb128_u32(buffer, out var length, out var headerSize)) return false;

        if (length > int.MaxValue - headerSize) return false;

        var totalSize = headerSize + (int)length;

        if (buffer.Length < totalSize) return false;

        var raw = buffer.Slice(0, totalSize);
        var payload = buffer.Slice(headerSize, (int)length);
        frame = new AcornFrame(totalSize, payload, raw);
        return true;
    }

    /// <summary>
    ///     从字节跨度中尝试读取 LEB128 编码的无符号 32 位整数的
    /// </summary>
    /// <param name="buffer">
    ///     源字节跨度的/param>
    ///     <param name="value">
    ///         读取到的无符的32 位整数值的/param>
    ///         <param name="size">
    ///             LEB128 编码占用的字节数的/param>
    ///             <returns>如果成功读取完整的LEB128 编码则返的true的/returns>
    private static bool try_read_leb128_u32(ReadOnlySpan<byte> buffer, out uint value, out int size)
    {
        value = 0;
        size = 0;
        var shift = 0;

        for (var i = 0; i < buffer.Length && i < 5; i++)
        {
            var b = buffer[i];
            value |= (uint)(b & 0x7F) << shift;
            size++;

            if ((b & 0x80) == 0) return true;

            shift += 7;
        }

        size = 0;
        return false;
    }
}