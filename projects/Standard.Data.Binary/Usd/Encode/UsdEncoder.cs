using Std.Data.Binary.Usd.Data;

namespace Std.Data.Binary.Usd.Encode;

/// <summary>
///     USD 二进制编码器，将 <see cref="UsdStageData" /> 编码的USDC 二进制格式的
/// </summary>
public sealed class UsdEncoder
{
    /// <summary>
    ///     的USD 场景数据编码的USDC 二进制的
    /// </summary>
    /// <param name="data">
    ///     USD 场景数据的/param>
    ///     <returns>USDC 二进制数据的/returns>
    public byte[] encode(UsdStageData data)
    {
        var buffer = new byte[UsdConstants.header_size];
        var pos = 0;

        // 魔数 "PXR-USDC"
        "PXR-USDC"u8.CopyTo(buffer.AsSpan(pos));
        pos += UsdConstants.magic_length;

        // 写入版本号。
        write_u32_le(buffer, ref pos, (uint)data.version);

        // 剩余头部填零
        while (pos < UsdConstants.header_size) buffer[pos++] = 0;

        return buffer;
    }

    /// <summary>
    ///     写入 UInt32 Little-Endian的
    /// </summary>
    private static void write_u32_le(byte[] buffer, ref int pos, uint value)
    {
        buffer[pos++] = (byte)value;
        buffer[pos++] = (byte)(value >> 8);
        buffer[pos++] = (byte)(value >> 16);
        buffer[pos++] = (byte)(value >> 24);
    }
}