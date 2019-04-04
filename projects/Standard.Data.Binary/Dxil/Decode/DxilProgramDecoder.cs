using Std.Data.Binary.Dxil.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dxil.Decode;

/// <summary>
///     DXIL 程序解码器，解析 DXIL Part 内的着色器程序数据的
/// </summary>
/// <remarks>
///     DXIL 程序的ProgramHeader的4 字节）和 LLVM Bitcode 数据组成的
///     LLVM Bitcode 的详细解码由 Nyar.Binary.Llvm 提供的
/// </remarks>
public sealed class DxilProgramDecoder
{
    /// <summary>
    ///     的DXIL Part 数据解码着色器程序的
    /// </summary>
    /// <param name="partData">
    ///     DXIL Part 的原始数据的/param>
    ///     <returns>
    ///         解码后的 DXIL 程序数据的/returns>
    ///         <exception cref="InvalidDataException">数据不是有效的DXIL 程序格式的/exception>
    public DxilProgramData decode(ReadOnlySpan<byte> partData)
    {
        var buffer = new ByteBuffer(partData);
        return decode_program(ref buffer);
    }

    private DxilProgramData decode_program(ref ByteBuffer buffer)
    {
        var header = read_program_header(ref buffer);

        if (buffer.position + header.bitcode_size > buffer.length)
            throw new InvalidDataException(
                $"DXIL Bitcode 数据不完整：期望 {header.bitcode_size} 字节，剩的{buffer.length - buffer.position} 字节");

        buffer.position = (int)header.bitcode_offset;
        var bitcodeData = buffer.read_bytes((int)header.bitcode_size).ToArray();

        return new DxilProgramData
        {
            header = header,
            bitcode_data = bitcodeData
        };
    }

    private static DxilProgramHeader read_program_header(ref ByteBuffer buffer)
    {
        if (buffer.remaining < DxilConstants.program_header_size)
            throw new InvalidDataException(
                $"DXIL Part 数据过短：期望至的{DxilConstants.program_header_size} 字节，实的{buffer.remaining} 字节");

        var majorVersion = buffer.read_u8();
        var minorVersion = buffer.read_u8();
        var shaderModelKindRaw = buffer.read_u8();
        var padding = buffer.read_u8();
        var size = buffer.read_u32_le();
        var bitcodeOffset = buffer.read_u32_le();
        var bitcodeSize = buffer.read_u32_le();

        var header = new DxilProgramHeader
        {
            major_version = majorVersion,
            minor_version = minorVersion,
            shader_model_kind_raw = shaderModelKindRaw,
            padding = padding,
            size = size,
            bitcode_offset = bitcodeOffset,
            bitcode_size = bitcodeSize
        };
        return header;
    }
}