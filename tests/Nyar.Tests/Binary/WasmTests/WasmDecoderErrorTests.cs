using Acorn.Wasm.Decode;

namespace Nyar.Tests.Binary.WasmTests;

/// <summary>
/// Wasm 解码器异常路径测试，验证解码器在异常输入下的行为�?///</summary>
public class WasmDecoderErrorTests
{
    // -region-

    [Fact]
    public void Decode_InvalidMagic_ThrowsInvalidDataException()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("魔数", ex.Message);
    }

    [Fact]
    public void Decode_PartialMagic_ThrowsInvalidDataException()
    {
        var data = new byte[] { 0x00, 0x61, 0x73 };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    [Fact]
    public void Decode_EmptyData_ThrowsException()
    {
        var data = Array.Empty<byte>();

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Decode_UnsupportedVersion_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D,
            0x02, 0x00, 0x00, 0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("版本", ex.Message);
    }

    [Fact]
    public void Decode_VersionZero_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D,
            0x00, 0x00, 0x00, 0x00
        };

        Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Decode_TruncatedHeader_ThrowsException()
    {
        var data = new byte[] { 0x00, 0x61, 0x73, 0x6D };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    [Fact]
    public void Decode_TruncatedSectionSize_ThrowsException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x01
        };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    [Fact]
    public void Decode_TruncatedSectionData_ThrowsException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x01, 0x10,
            0x01,
            0x60,
            0x00
        };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    [Fact]
    public void Decode_TruncatedTypeSection_ThrowsException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x01, 0x04,
            0x01,
            0x60,
            0x01,
            0x7F
        };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Decode_InvalidFunctionTypeForm_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x01, 0x04,
            0x01,
            0xFF,
            0x01, 0x00, 0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("函数类型标记", ex.Message);
    }

    [Fact]
    public void Decode_InvalidValueType_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x01, 0x04,
            0x01,
            0x60,
            0x01,
            0xFF,
            0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("值类�?, ex.Message);
    }

    [Fact]
    public void Decode_InvalidImportKind_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x02, 0x0A,
            0x01,
            0x01, 0x6D,
            0x01, 0x66,
            0xFF,
            0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("导入种类", ex.Message);
    }

    [Fact]
    public void Decode_InvalidDataSegmentFlags_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x0B, 0x03,
            0x01,
            0xFF,
            0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("数据段标�?, ex.Message);
    }

    [Fact]
    public void Decode_InvalidElementFlags_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x09, 0x03,
            0x01,
            0xFF,
            0x00
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("元素段标�?, ex.Message);
    }

    [Fact]
    public void Decode_InvalidInitExpressionOpcode_ThrowsInvalidDataException()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00,
            0x06, 0x04,
            0x01,
            0x7F,
            0x00,
            0xFF
        };

        var ex = Assert.Throws<InvalidDataException>(() => WasmDecoder.decode_module(data));
        Assert.Contains("初始化表达式", ex.Message);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Decode_MinimalValidModule_Succeeds()
    {
        var data = new byte[]
        {
            0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00
        };

        var result = WasmDecoder.decode_module(data);

        Assert.NotNull(result);
        Assert.Equal((uint)1, result.version);
        Assert.Empty(result.types);
        Assert.Empty(result.imports);
        Assert.Empty(result.function_type_indices);
        Assert.Empty(result.tables);
        Assert.Empty(result.memories);
        Assert.Empty(result.globals);
        Assert.Empty(result.exports);
        Assert.Null(result.start_function_index);
        Assert.Empty(result.elements);
        Assert.Empty(result.codes);
        Assert.Empty(result.data_segments);
    }

    // [/section renamed - encoding fix]
}

