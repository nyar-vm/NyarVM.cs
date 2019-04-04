using Acorn.Wasm.Data;
using Nyar.Assembler;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;
using Wasmtime;

namespace Nyar.Tests.Wasm;

/// <summary>
/// WASM 后端集成测试，验证最小可实例化闭环的
///</summary>
public class WasmIntegrationTests
{
    /// <summary>
    /// 构造一个返回常量的最小模块的    
///</summary>
    private static GenerateModule create_const_return_unit()
    {
        var unit = new GenerateModule("hello");
        var func = new GenerateFunction("main", "i32");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Const, new GenerateOperand.I32(42)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        unit.AddFunction(func);
        return unit;
    }

    /// <summary>
    /// 构造一个零参数、零返回的最小主函数的    
///</summary>
    private static GenerateModule create_empty_main_unit()
    {
        var unit = new GenerateModule("start");
        unit.AddFunction(new GenerateFunction("main", "void"));
        return unit;
    }

    /// <summary>
    /// 创建浏览的WASM 编译选项的    
///</summary>
    private static CompilationOptions create_web_options()
    {
        var options = new CompilationOptions
        {
            Target = new CompilationTarget
            {
                Runtime = TargetRuntime.Wasm,
                Arch = Arch.Wasm32,
                Abi = ABI.WebAssembly,
                OS = OS.Web,
                Api = API.Web,
                Environment = TargetEnvironment.Web
            }
        };
        return options;
    }

    /// <summary>
    /// 创建 WASI p1 编译选项的    
///</summary>
    private static CompilationOptions create_wasi_p1_options()
    {
        var options = new CompilationOptions
        {
            Target = new CompilationTarget
            {
                Runtime = TargetRuntime.Wasi,
                Arch = Arch.Wasm32,
                Abi = ABI.WasiP1,
                OS = OS.Web,
                Api = API.Web,
                Environment = TargetEnvironment.Web
            }
        };
        return options;
    }

    [Fact]
    public void Compile_ConstReturn_GeneratesWasmAsset()
    {
        var unit = create_const_return_unit();
        var result = compile(unit, create_web_options());
        var wasmFile = result.Assets.FirstOrDefault(f => f.Name.EndsWith(".wasm"));

        Assert.NotNull(wasmFile);
        Assert.True(wasmFile!.Content.Length > 0);

        var wasmBytes = wasmFile.Content;
        Assert.Equal(0x00, wasmBytes[0]);
        Assert.Equal((byte)'a', wasmBytes[1]);
        Assert.Equal((byte)'s', wasmBytes[2]);
        Assert.Equal((byte)'m', wasmBytes[3]);
    }

    [Fact]
    public void Compile_ConstReturn_Wasmtime_CanInvokeMain()
    {
        var unit = create_const_return_unit();
        var result = compile(unit, create_web_options());
        var wasmFile = result.Assets.First(f => f.Name.EndsWith(".wasm"));
        var wasmBytes = wasmFile!.Content;

        using var engine = new Engine();
        using var module = Module.FromBytes(engine, "hello", wasmBytes);
        using var linker = new Linker(engine);
        using var store = new Store(engine);
        var instance = linker.Instantiate(store, module);
        var main = instance.GetFunction("main");

        Assert.NotNull(main);
        var value = main!.Invoke();
        Assert.Equal(42, Assert.IsType<int>(value));
    }

    [Fact]
    public void Compile_WasiMain_ExportsStartAlias()
    {
        var unit = create_empty_main_unit();
        var result = compile(unit, create_wasi_p1_options());
        var wasmFile = result.Assets.First(f => f.Name.EndsWith(".wasm"));

        using var engine = new Engine();
        using var module = Module.FromBytes(engine, "start", wasmFile.Content);
        using var linker = new Linker(engine);
        using var store = new Store(engine);
        var instance = linker.Instantiate(store, module);
        var start = instance.GetFunction("_start");

        Assert.NotNull(start);
        var value = start!.Invoke();
        Assert.Null(value);
    }

    [Fact]
    public void Validate_UnstructuredJump_ShouldFailFast()
    {
        var module = new GenerateModule("jump_not_supported");
        var main = new GenerateFunction("main", "i32");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(0)));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.JumpIfFalse,
            new GenerateOperand.Label("else")));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(1)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        main.AddLabel("else");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(2)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(main);

        var backend = new WasmBackend();
        var success = backend.Validate(module, out var diagnostics);

        Assert.False(success);
        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Level == DiagnosticLevel.Error &&
            diagnostic.Message.Contains("JumpIfFalse", StringComparison.Ordinal));
    }

    private static OutputSpec<WasmModuleData> compile(GenerateModule module, CompilationOptions options)
    {
        var compiler = new CodeGenCompiler([new WasmBackend()]);
        var result = compiler.Compile<WasmModuleData>(module, options);

        Assert.NotNull(result.Output);
        Assert.False(result.HasErrors);
        return result.Output!;
    }
}