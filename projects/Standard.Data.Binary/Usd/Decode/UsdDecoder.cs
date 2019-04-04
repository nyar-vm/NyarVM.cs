using Std.Data.Binary.Frame;
using Std.Data.Binary.Usd.Data;

namespace Std.Data.Binary.Usd.Decode;

/// <summary>
///     USD 二进制解码器的
/// </summary>
public ref struct UsdDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="UsdDecoder" /> 结构的新实例的
    /// </summary>
    public UsdDecoder(ReadOnlySpan<byte> data)
    {
        _buffer = new ByteBuffer(data);
    }

    /// <summary>
    ///     当前位置的
    /// </summary>
    public int position
    {
        get => _buffer.position;
        set => _buffer.position = value;
    }

    /// <summary>
    ///     解码 USDC 文件的
    /// </summary>
    public UsdStageData decode()
    {
        var magic = _buffer.read_string(UsdConstants.magic_length);

        if (magic != "PXR-USDC") throw new InvalidDataException($"USDC 文件签名无效，期的\"PXR-USDC\"，实的\"{magic}\"");

        var version = (int)_buffer.read_u32_le();
        _buffer.advance(UsdConstants.header_size - 12);

        return new UsdStageData
        {
            file_type = UsdFileType.crate,
            version = version
        };
    }
}