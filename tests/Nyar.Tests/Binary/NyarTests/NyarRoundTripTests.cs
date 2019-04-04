namespace Nyar.Tests.Binary.NyarTests;

public class NyarRoundTripTests
{
    [Fact]
    public void Encode_Decode_MinimalModule()
    {
        var original = new NyarModuleData
        {
            Version = 1,
            Name = "test_module",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(original.Name, decoded.Name);
        Assert.Empty(decoded.Constants);
        Assert.Empty(decoded.Functions);
        Assert.Empty(decoded.Imports);
        Assert.Empty(decoded.Exports);
    }

    [Fact]
    public void Encode_Decode_FullModule()
    {
        var original = new NyarModuleData
        {
            Version = 1,
            Name = "full_module",
            Constants =
            [
                new NyarConstant(NyarConstantKind.Integer32, 42),
                new NyarConstant(NyarConstantKind.Float64, 3.14),
                new NyarConstant(NyarConstantKind.Boolean, true),
                new NyarConstant(NyarConstantKind.Null, null),
                new NyarConstant(NyarConstantKind.String, "hello"),
                new NyarConstant(NyarConstantKind.BigInt, new byte[] { 0, 0xFF, 0x01 })
            ],
            Functions =
            [
                new NyarFunction(name: "add", arity: 2, localCount: 0, codeOffset: 0, codeLength: 8),
                new NyarFunction(name: "main", arity: 0, localCount: 3, codeOffset: 8, codeLength: 16)
            ],
            Imports =
            [
                new NyarImport(NyarImportKind.Function, "math", "sqrt"),
                new NyarImport(NyarImportKind.Global, "config", "version")
            ],
            Exports =
            [
                new NyarExport(NyarExportKind.Function, "add", 0),
                new NyarExport(NyarExportKind.Global, "count", -1)
            ],
            WitnessEntries =
            [
                new NyarWitnessDispatchEntry
                {
                    MethodId = 1001,
                    TypeId = 2001,
                    MethodName = "insert",
                    FunctionIndex = 1,
                    InterfaceId = 3001,
                    InterfaceMethodIndex = 7
                }
            ]
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Version, decoded.Version);
        Assert.Equal(original.Name, decoded.Name);

        Assert.Equal(6, decoded.Constants.Count);
        Assert.Equal(NyarConstantKind.Integer32, decoded.Constants[0].Kind);
        Assert.Equal(42, decoded.Constants[0].Payload);
        Assert.Equal(NyarConstantKind.Float64, decoded.Constants[1].Kind);
        Assert.Equal(3.14, decoded.Constants[1].Payload);
        Assert.Equal(NyarConstantKind.Boolean, decoded.Constants[2].Kind);
        Assert.Equal(true, decoded.Constants[2].Payload);
        Assert.Equal(NyarConstantKind.Null, decoded.Constants[3].Kind);
        Assert.Null(decoded.Constants[3].Payload);
        Assert.Equal(NyarConstantKind.String, decoded.Constants[4].Kind);
        Assert.Equal("hello", decoded.Constants[4].Payload);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[5].Kind);
        Assert.Equal(new byte[] { 0, 0xFF, 0x01 }, decoded.Constants[5].Payload);

        Assert.Equal(2, decoded.Functions.Count);
        Assert.Equal("add", decoded.Functions[0].Name);
        Assert.Equal(2, decoded.Functions[0].Arity);
        Assert.Equal(0, decoded.Functions[0].LocalCount);
        Assert.Equal(0, decoded.Functions[0].CodeOffset);
        Assert.Equal(8, decoded.Functions[0].CodeLength);
        Assert.Equal("main", decoded.Functions[1].Name);
        Assert.Equal(0, decoded.Functions[1].Arity);
        Assert.Equal(3, decoded.Functions[1].LocalCount);
        Assert.Equal(8, decoded.Functions[1].CodeOffset);
        Assert.Equal(16, decoded.Functions[1].CodeLength);

        Assert.Equal(2, decoded.Imports.Count);
        Assert.Equal(NyarImportKind.Function, decoded.Imports[0].Kind);
        Assert.Equal("math", decoded.Imports[0].ModuleName);
        Assert.Equal("sqrt", decoded.Imports[0].SymbolName);
        Assert.Equal(NyarImportKind.Global, decoded.Imports[1].Kind);
        Assert.Equal("config", decoded.Imports[1].ModuleName);
        Assert.Equal("version", decoded.Imports[1].SymbolName);

        Assert.Equal(2, decoded.Exports.Count);
        Assert.Equal(NyarExportKind.Function, decoded.Exports[0].Kind);
        Assert.Equal("add", decoded.Exports[0].SymbolName);
        Assert.Equal(0, decoded.Exports[0].FunctionIndex);
        Assert.Equal(NyarExportKind.Global, decoded.Exports[1].Kind);
        Assert.Equal("count", decoded.Exports[1].SymbolName);
        Assert.Equal(-1, decoded.Exports[1].FunctionIndex);

        Assert.Single(decoded.WitnessEntries);
        Assert.Equal(1001, decoded.WitnessEntries[0].MethodId);
        Assert.Equal(2001, decoded.WitnessEntries[0].TypeId);
        Assert.Equal("insert", decoded.WitnessEntries[0].MethodName);
        Assert.Equal(1, decoded.WitnessEntries[0].FunctionIndex);
        Assert.Equal(3001, decoded.WitnessEntries[0].InterfaceId);
        Assert.Equal(7, decoded.WitnessEntries[0].InterfaceMethodIndex);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        var data = new byte[16];
        var decoder = new NyarDecoder();

        Assert.Throws<InvalidNyarDataException>(() => decoder.Decode(data));
    }

    [Fact]
    public void Decode_TooShort_Throws()
    {
        var data = new byte[4];
        var decoder = new NyarDecoder();

        Assert.ThrowsAny<Exception>(() => decoder.Decode(data));
    }

    [Fact]
    public void Encode_Decode_Int64BigIntConstants()
    {
        var maxBytes = BitConverter.GetBytes(long.MaxValue);
        var minBytes = BitConverter.GetBytes(long.MinValue);

        var original = new NyarModuleData
        {
            Name = "int64_test",
            Constants =
            [
                new NyarConstant(NyarConstantKind.BigInt, maxBytes),
                new NyarConstant(NyarConstantKind.BigInt, minBytes)
            ],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.Constants.Count);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[0].Kind);
        Assert.Equal(NyarConstantKind.BigInt, decoded.Constants[1].Kind);

        var decodedMax = (byte[])decoded.Constants[0].Payload!;
        var decodedMin = (byte[])decoded.Constants[1].Payload!;
        Assert.Equal(long.MaxValue, BitConverter.ToInt64(decodedMax, 0));
        Assert.Equal(long.MinValue, BitConverter.ToInt64(decodedMin, 0));
    }

    [Fact]
    public void Encode_Decode_LongModuleName()
    {
        var longName = new string('N', 256);

        var original = new NyarModuleData
        {
            Name = longName,
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Name, decoded.Name);
        Assert.Equal(256, decoded.Name.Length);
    }

    [Fact]
    public void Encode_Decode_ManyFunctions()
    {
        var functions = new List<NyarFunction>(100);
        for (var i = 0; i < 100; i++)
        {
            functions.Add(new NyarFunction(name: $"func_{i:D3}", arity: i % 4, localCount: i % 8, codeOffset: i * 10,
                codeLength: 10));
        }

        var original = new NyarModuleData
        {
            Name = "many_funcs",
            Constants = [],
            Functions = functions,
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(100, decoded.Functions.Count);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal($"func_{i:D3}", decoded.Functions[i].Name);
            Assert.Equal(i % 4, decoded.Functions[i].Arity);
            Assert.Equal(i % 8, decoded.Functions[i].LocalCount);
            Assert.Equal(i * 10, decoded.Functions[i].CodeOffset);
            Assert.Equal(10, decoded.Functions[i].CodeLength);
        }
    }

    [Fact]
    public void Encode_Decode_EmptyModuleVariants()
    {
        var emptyModule = new NyarModuleData
        {
            Name = "all_empty",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(emptyModule);

        var decoder = new NyarDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal("all_empty", decoded.Name);
        Assert.Empty(decoded.Constants);
        Assert.Empty(decoded.Functions);
        Assert.Empty(decoded.Imports);
        Assert.Empty(decoded.Exports);
        
        var namelessModule = new NyarModuleData
        {
            Name = "",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        bytes = encoder.Encode(namelessModule);
        decoded = decoder.Decode(bytes);

        Assert.Equal(string.Empty, decoded.Name);

        var constantOnlyModule = new NyarModuleData
        {
            Name = "only_const",
            Constants = [new NyarConstant(NyarConstantKind.Integer32, 99)],
            Functions = [],
            Imports = [],
            Exports = []
        };

        bytes = encoder.Encode(constantOnlyModule);
        decoded = decoder.Decode(bytes);

        Assert.Single(decoded.Constants);
        Assert.Equal(NyarConstantKind.Integer32, decoded.Constants[0].Kind);
        Assert.Equal(99, decoded.Constants[0].Payload);
        Assert.Empty(decoded.Functions);
        Assert.Empty(decoded.Imports);
        Assert.Empty(decoded.Exports);

        var importExportOnlyModule = new NyarModuleData
        {
            Name = "only_import_export",
            Constants = [],
            Functions = [],
            Imports =
            [
                new NyarImport(NyarImportKind.Function, "lib", "fn")
            ],
            Exports =
            [
                new NyarExport(NyarExportKind.Global, "VERSION", -1)
            ]
        };

        bytes = encoder.Encode(importExportOnlyModule);
        decoded = decoder.Decode(bytes);

        Assert.Empty(decoded.Constants);
        Assert.Empty(decoded.Functions);
        Assert.Single(decoded.Imports);
        Assert.Single(decoded.Exports);
        Assert.Equal("lib", decoded.Imports[0].ModuleName);
        Assert.Equal("fn", decoded.Imports[0].SymbolName);
        Assert.Equal("VERSION", decoded.Exports[0].SymbolName);
    }

    [Fact]
    public void Decode_WrongMagic_ThrowsInvalidNyarDataException()
    {
        var original = new NyarModuleData
        {
            Name = "magic_test",
            Constants = [],
            Functions = [],
            Imports = [],
            Exports = []
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        bytes[0] = 0xDE;
        bytes[1] = 0xAD;
        bytes[2] = 0xBE;
        bytes[3] = 0xEF;

        var decoder = new NyarDecoder();
        var ex = Assert.Throws<InvalidNyarDataException>(() => decoder.Decode(bytes));
        Assert.Contains("无效")],
            Exports = [new NyarExport(NyarExportKind.Function, "e", 0)]
        };

        var encoder = new NyarEncoder();
        var bytes = encoder.Encode(original);

        var scanner = new NyarScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.Equal(1u, header.Version);
        Assert.Equal("scan_header_test", header.ModuleName);
        Assert.Equal(4, header.SectionCount);
        Assert.True(header.HasConstantsSection);
        Assert.True(header.HasFunctionsSection);
        Assert.True(header.HasImportsSection);
        Assert.True(header.HasExportsSection);
    }
}

