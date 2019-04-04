using Acorn.Wasm.Data;
using Acorn.Wasm.Decode;
using Acorn.Wasm.Encode;

namespace Nyar.Tests.Binary.WasmTests;

public class WasmRoundTripTests
{
    [Fact]
    public void Encode_MinimalModule_HeaderBytes()
    {
        var original = new WasmModuleData();
        var bytes = WasmEncoder.encode_module(original);

        Assert.Equal(8, bytes.Length);
        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x61, bytes[1]);
        Assert.Equal(0x73, bytes[2]);
        Assert.Equal(0x6D, bytes[3]);
        Assert.Equal(1u, BitConverter.ToUInt32(bytes, 4));
    }

    [Fact]
    public void Decode_MinimalModule_FromRawBytes()
    {
        var rawWasm = new byte[] { 0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00 };
        var decoded = WasmDecoder.decode_module(rawWasm);
        Assert.Equal(1u, decoded.version);
    }

    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new WasmModuleData();

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Equal(original.version, decoded.version);
        Assert.Empty(decoded.types);
        Assert.Empty(decoded.imports);
        Assert.Empty(decoded.function_type_indices);
        Assert.Empty(decoded.tables);
        Assert.Empty(decoded.memories);
        Assert.Empty(decoded.globals);
        Assert.Empty(decoded.exports);
        Assert.Null(decoded.start_function_index);
        Assert.Empty(decoded.elements);
        Assert.Empty(decoded.codes);
        Assert.Empty(decoded.data_segments);
    }

    [Fact]
    public void Encode_Decode_ModuleWithTypesAndFunctions()
    {
        var types = new WasmFunctionType[]
        {
            new() { parameters = [], results = [] },
            new() { parameters = [WasmValueType.int32, WasmValueType.int32], results = [WasmValueType.int32] }
        };

        var imports = new WasmImport[]
        {
            new()
            {
                module = "env",
                field = "memory",
                descriptor = new WasmImportDescriptor
                {
                    kind = WasmExternalKind.memory,
                    memory_type = new WasmMemoryType { limits = new WasmLimits { minimum = 1 } }
                }
            }
        };

        var exports = new WasmExport[]
        {
            new() { name = "main", kind = WasmExternalKind.function, index = 0 }
        };

        var codes = new WasmCode[]
        {
            new() { body = [0x00, 0x0B] }
        };

        var original = new WasmModuleData
        {
            types = types,
            imports = imports,
            function_type_indices = [0],
            exports = exports,
            codes = codes
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Equal(original.version, decoded.version);
        Assert.Equal(2, decoded.types.Count);
        Assert.Empty(decoded.types[0].parameters);
        Assert.Empty(decoded.types[0].results);
        Assert.Equal(2, decoded.types[1].parameters.Count);
        Assert.Equal(WasmValueType.int32, decoded.types[1].parameters[0]);
        Assert.Single(decoded.types[1].results);
        Assert.Equal(WasmValueType.int32, decoded.types[1].results[0]);

        Assert.Single(decoded.imports);
        Assert.Equal("env", decoded.imports[0].module);
        Assert.Equal("memory", decoded.imports[0].field);

        Assert.Single(decoded.exports);
        Assert.Equal("main", decoded.exports[0].name);

        Assert.Single(decoded.codes);
        Assert.Equal(codes[0].body, decoded.codes[0].body);
    }

    [Fact]
    public void Encode_Decode_ModuleWithMemory()
    {
        var original = new WasmModuleData
        {
            memories =
            [
                new WasmMemory { type = new WasmMemoryType { limits = new WasmLimits { minimum = 1, maximum = 256 } } }
            ]
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Single(decoded.memories);
        Assert.Equal((uint)1, decoded.memories[0].type.Limits.Minimum);
        Assert.Equal((uint)256, decoded.memories[0].type.Limits.Maximum);
    }

    [Fact]
    public void Encode_Decode_ModuleWithGlobals()
    {
        var original = new WasmModuleData
        {
            globals =
            [
                new WasmGlobal
                {
                    type = new WasmGlobalType { value_type = WasmValueType.int32, mutable = true },
                    init_expression = [0x41, 0x2A, 0x0B]
                }
            ]
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Single(decoded.globals);
        Assert.Equal(WasmValueType.int32, decoded.globals[0].type.ValueType);
        Assert.True(decoded.globals[0].type.Mutable);
        Assert.Equal(new byte[] { 0x41, 0x2A, 0x0B }, decoded.globals[0].init_expression);
    }

    [Fact]
    public void Encode_Decode_ModuleWithStartFunction()
    {
        var original = new WasmModuleData
        {
            types = [new WasmFunctionType()],
            function_type_indices = [0],
            start_function_index = 0,
            codes = [new WasmCode { body = [0x0B] }]
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Equal((uint)0, decoded.start_function_index);
    }

    [Fact]
    public void Encode_Decode_ModuleWithCustomSection()
    {
        var original = new WasmModuleData
        {
            custom_sections = [new WasmCustomSection { name = "producers", data = [0x01, 0x02, 0x03] }]
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Single(decoded.custom_sections);
        Assert.Equal("producers", decoded.custom_sections[0].name);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, decoded.custom_sections[0].data);
    }

    [Fact]
    public void Encode_Decode_ModuleWithDataSegments()
    {
        var original = new WasmModuleData
        {
            memories = [new WasmMemory { type = new WasmMemoryType { limits = new WasmLimits { minimum = 1 } } }],
            data_segments =
            [
                new WasmData
                {
                    memory_index = 0,
                    offset_expression = [0x41, 0x00, 0x0B],
                    initializer = [0xDE, 0xAD, 0xBE, 0xEF]
                }
            ]
        };

        var bytes = WasmEncoder.encode_module(original);
        var decoded = WasmDecoder.decode_module(bytes);

        Assert.Single(decoded.data_segments);
        Assert.Equal((uint)0, decoded.data_segments[0].memory_index);
        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, decoded.data_segments[0].initializer);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00 };

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }

    [Fact]
    public void Decode_TruncatedData_Throws()
    {
        var data = new byte[3];

        Assert.ThrowsAny<Exception>(() => WasmDecoder.decode_module(data));
    }
}
