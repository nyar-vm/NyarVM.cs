using Std.Data.Binary.Basis.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Basis.Decode;

/// <summary>
///     Basis/KTX2 解码器的
/// </summary>
public ref struct BasisDecoder
{
    private ByteBuffer _buffer;

    /// <summary>
    ///     初始的<see cref="BasisDecoder" /> 结构的新实例的
    /// </summary>
    public BasisDecoder(ReadOnlySpan<byte> data)
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
    ///     解码 Basis 文件的
    /// </summary>
    public BasisFileData decode()
    {
        if (_buffer.match_magic(BasisConstants.ktx2_magic)) return decode_ktx2();

        return decode_basis();
    }

    private BasisFileData decode_ktx2()
    {
        _buffer.consume_magic(BasisConstants.ktx2_magic);

        var vkFormat = _buffer.read_u32_le();
        var typeSize = _buffer.read_u32_le();
        var pixelWidth = _buffer.read_u32_le();
        var pixelHeight = _buffer.read_u32_le();
        _buffer.advance(4);
        var layerCount = _buffer.read_u32_le();
        var faceCount = _buffer.read_u32_le();
        var levelCount = _buffer.read_u32_le();
        _buffer.advance(36);

        return new BasisFileData
        {
            width = (int)pixelWidth,
            height = (int)pixelHeight,
            mip_levels = (int)levelCount,
            image_count = (int)(layerCount * faceCount)
        };
    }

    private BasisFileData decode_basis()
    {
        _buffer.consume_magic(BasisConstants.basis_magic);

        var version = _buffer.read_u32_le();
        var headerSize = _buffer.read_u32_le();
        var headerCrc16 = _buffer.read_u16_le();
        _buffer.advance(6);
        var imageCount = _buffer.read_u32_le();
        var format = (BasisTextureFormat)_buffer.read_u32_le();
        var flags = _buffer.read_u16_le();
        _buffer.advance(8);
        var pixelWidth = _buffer.read_u32_le();
        var pixelHeight = _buffer.read_u32_le();
        _buffer.advance(8);
        var totalImages = _buffer.read_u32_le();
        var mipLevels = _buffer.read_u32_le();

        return new BasisFileData
        {
            width = (int)pixelWidth,
            height = (int)pixelHeight,
            mip_levels = (int)mipLevels,
            format = format,
            is_srgb = (flags & 0x01) != 0,
            image_count = (int)imageCount
        };
    }
}