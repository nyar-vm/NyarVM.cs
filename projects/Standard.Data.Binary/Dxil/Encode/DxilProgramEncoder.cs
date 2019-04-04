using Std.Data.Binary.Dxil.Data;
using Std.Data.Binary.Frame;

namespace Std.Data.Binary.Dxil.Encode;

/// <summary>
///     DXIL 程序编码器，将着色器程序数据编码的DXIL Part 二进制格式的
/// </summary>
/// <remarks>
///     DXIL 程序的ProgramHeader的4 字节）和 LLVM Bitcode 数据组成的
///     LLVM Bitcode 的编码由 Nyar.Binary.Llvm 的编码器提供的
/// </remarks>
public sealed class DxilProgramEncoder
{
    /// <summary>
    ///     的DXIL 程序数据编码的DXIL Part 数据的
    /// </summary>
    /// <param name="program">
    ///     DXIL 程序数据的/param>
    ///     <returns>DXIL Part 数据（ProgramHeader + Bitcode）的/returns>
    public byte[] encode(DxilProgramData program)
    {
        var bitcodeOffset = (uint)DxilConstants.program_header_size;
        var totalSize = bitcodeOffset + (uint)program.bitcode_data.Length;

        var buffer = new byte[totalSize];
        var writer = new ByteBufferWriter(buffer);

        write_program_header(ref writer, program.header, totalSize, bitcodeOffset,
            (uint)program.bitcode_data.Length);
        writer.write(program.bitcode_data);

        return buffer[..writer.position];
    }

    /// <summary>
    ///     从着色器模型的LLVM Bitcode 创建并编的DXIL 程序的
    /// </summary>
    /// <param name="shaderModel">
    ///     着色器模型类型的/param>
    ///     <param name="dxilMajorVersion">
    ///         DXIL 主版本号的/param>
    ///         <param name="dxilMinorVersion">
    ///             DXIL 次版本号的/param>
    ///             <param name="bitcodeData">
    ///                 LLVM Bitcode 数据的/param>
    ///                 <returns>DXIL Part 数据的/returns>
    public byte[] encode_from_bitcode(DxilShaderModelKind shaderModel,
        byte dxilMajorVersion, byte dxilMinorVersion, byte[] bitcodeData)
    {
        var header = new DxilProgramHeader
        {
            shader_model_kind = shaderModel,
            major_version = dxilMajorVersion,
            minor_version = dxilMinorVersion
        };

        var program = new DxilProgramData
        {
            header = header,
            bitcode_data = bitcodeData
        };

        return encode(program);
    }

    private static void write_program_header(ref ByteBufferWriter writer,
        DxilProgramHeader header, uint totalSize, uint bitcodeOffset, uint bitcodeSize)
    {
        writer.write_u8(header.major_version);
        writer.write_u8(header.minor_version);
        writer.write_u8(header.shader_model_kind_raw);
        writer.write_u8(header.padding);
        writer.write_u32_le(totalSize);
        writer.write_u32_le(bitcodeOffset);
        writer.write_u32_le(bitcodeSize);
    }
}