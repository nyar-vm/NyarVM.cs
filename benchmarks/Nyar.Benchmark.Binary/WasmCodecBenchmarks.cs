using Nyar.Binary.Wasm.Data;
using Nyar.Binary.Wasm.Encode;
using BenchmarkDotNet.Attributes;

namespace Nyar.Binary.Benchmarks;

#region Wasm 格式编解码基的
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
public class WasmCodecBenchmarks
{
    private WasmModuleData _smallModule = null!;
    private WasmModuleData _mediumModule = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallModule = CreateModule(functions: 1, types: 1);
        _mediumModule = CreateModule(functions: 100, types: 20);
    }

    [Benchmark]
    public byte[] EncodeSmall()
    {
        var buf = new byte[512];

        WasmEncoder.EncodeModule(buf, _smallModule);

        return buf;
    }

    [Benchmark]
    public byte[] EncodeMedium()
    {
        var buf = new byte[65536];

        WasmEncoder.EncodeModule(buf, _mediumModule);

        return buf;
    }

    private static WasmModuleData CreateModule(int functions, int types)
    {
        var typeList = new WasmFunctionType[types];

        for (var i = 0; i < types; i++)
        {
            typeList[i] = new WasmFunctionType
            {
                Parameters = [],
                Results = [WasmValueType.Int32]
            };
        }

        var fnIndices = new uint[functions];

        for (var i = 0; i < functions; i++)
        {
            fnIndices[i] = (uint)(i % types);
        }

        var codes = new WasmCode[functions];

        for (var i = 0; i < functions; i++)
        {
            codes[i] = new WasmCode
            {
                Locals = [],
                Body = [0x41, 0x2A, 0x0B]
            };
        }

        var exports = new WasmExport[functions];

        for (var i = 0; i < functions; i++)
        {
            exports[i] = new WasmExport
            {
                Name = $"fn_{i}",
                Kind = WasmExternalKind.Function,
                Index = (uint)i
            };
        }

        return new WasmModuleData
        {
            Version = 1,
            Types = typeList,
            FunctionTypeIndices = fnIndices,
            Exports = exports,
            Codes = codes,
            Tables = [],
            Memories = [],
            Globals = [],
            Elements = [],
            DataSegments = [],
            Imports = [],
            Tags = [],
            CustomSections = []
        };
    }
}

#endregion



