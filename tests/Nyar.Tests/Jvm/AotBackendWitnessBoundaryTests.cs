using Acorn.Jvm.Data;
using Acorn.Wasm.Data;
using Nyar.Assembler;
using Nyar.Binary.Clr;
using Nyar.Binary.Clr.Data;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;

namespace Nyar.Tests.Jvm;

public sealed class AotBackendWitnessBoundaryTests
{
    [Theory]
    [InlineData("JVM")]
    [InlineData("WASM")]
    [InlineData("CLR")]
    [InlineData("Native")]
    public void Validate_ModuleWithWitnessEntriesButNoDispatch_ShouldSucceed(string backendName)
    {
        var backend = select_backend(backendName);
        var module = create_minimal_module($"MetaOnly_{backendName}");
        module.AddWitnessEntry(new GenerateWitnessDispatchEntry(
            "Display",
            0,
            "show",
            "User",
            "Display_User_show"));

        var success = backend.Validate(module, out var diagnostics);

        Assert.True(module.HasWitnessEntries);
        Assert.False(module.HasWitnessDispatch);
        Assert.True(success);
        Assert.DoesNotContain(diagnostics, diagnostic =>
            diagnostic.Level == DiagnosticLevel.Error &&
            diagnostic.Message.Contains("witness", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("JVM")]
    [InlineData("WASM")]
    [InlineData("CLR")]
    [InlineData("Native")]
    public void Validate_ModuleWithWitnessDispatch_ShouldReject(string backendName)
    {
        var backend = select_backend(backendName);
        var module = create_witness_dispatch_module($"Dispatch_{backendName}");

        var success = backend.Validate(module, out var diagnostics);

        Assert.True(module.HasWitnessEntries);
        Assert.True(module.HasWitnessDispatch);
        Assert.False(success);
        Assert.Contains(diagnostics, diagnostic =>
            diagnostic.Level == DiagnosticLevel.Error &&
            diagnostic.Message.Contains("witness", StringComparison.OrdinalIgnoreCase));
    }

    private static ICodeGenBackend select_backend(string backendName)
    {
        var compiler = new CodeGenCompiler(create_backend_set(backendName));
        var options = new CompilationOptions
        {
            Target = resolve_target(backendName)
        };

        return backendName switch
        {
            "JVM" => compiler.SelectBackend<JvmClassFileData>(options)
                     ?? throw new InvalidOperationException("未能选中 JVM 强类型后端。"),
            "WASM" => compiler.SelectBackend<WasmModuleData>(options)
                      ?? throw new InvalidOperationException("未能选中 WASM 强类型后端。"),
            "CLR" => compiler.SelectBackend<ClrModuleData>(options)
                     ?? throw new InvalidOperationException("未能选中 CLR 强类型后端。"),
            "Native" => compiler.SelectBackend<NativeCodeInfo>(options)
                        ?? throw new InvalidOperationException("未能选中 Native 强类型后端。"),
            _ => throw new ArgumentOutOfRangeException(nameof(backendName), backendName, "未知后端。")
        };
    }

    private static IEnumerable<ICodeGenBackend> create_backend_set(string backendName)
    {
        return backendName switch
        {
            "JVM" => [new JvmBackend()],
            "WASM" => [new WasmBackend()],
            "CLR" => [new ClrBackend()],
            "Native" => [new NativeBackend()],
            _ => throw new ArgumentOutOfRangeException(nameof(backendName), backendName, "未知后端。")
        };
    }

    private static CompilationTarget resolve_target(string backendName)
    {
        return backendName switch
        {
            "JVM" => CompilationTarget.Jvm,
            "WASM" => CompilationTarget.Wasm,
            "CLR" => CompilationTarget.Clr,
            "Native" => CompilationTarget.Native,
            _ => throw new ArgumentOutOfRangeException(nameof(backendName), backendName, "未知后端。")
        };
    }

    private static GenerateModule create_minimal_module(string name)
    {
        var module = new GenerateModule(name);
        var func = new GenerateFunction("main", "void");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(func);
        return module;
    }

    private static GenerateModule create_witness_dispatch_module(string name)
    {
        var module = create_minimal_module(name);
        module.AddWitnessEntry(new GenerateWitnessDispatchEntry(
            "Display",
            0,
            "show",
            "User",
            "Display_User_show"));

        var function = module.Functions[0];
        function.Instructions.Clear();
        function.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallWitness,
            new GenerateOperand.I32(1),
            new GenerateOperand.I32(0)));
        function.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        return module;
    }
}