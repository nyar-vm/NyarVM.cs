using Nyar.Assembler;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;
using Olymp.Diagnostics;

namespace Nyar.Tests.Jvm;

public class CodeGenCompilerTests
{
    [Fact]
    public void Compile_ShouldSelectTypedBackendAndReturnTypedOutput()
    {
        var compiler = new CodeGenCompiler(
        [
            new FakeBackend<byte[]>("generic-wasm", [Arch.Wasm32, Arch.Wasm64], [1, 2]),
            new FakeBackend<byte[]>("exact-wasm32", [Arch.Wasm32], [3, 4]),
            new FakeBackend<string>("text-wasm32", [Arch.Wasm32], "wat")
        ]);

        var result = compiler.Compile<byte[]>(
            new GenerateModule("hello"),
            new CompilationOptions { Target = CompilationTarget.Wasm });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Backend);
        Assert.NotNull(result.Output);
        Assert.Equal(Arch.Wasm32, result.TargetArch);
        Assert.Equal("exact-wasm32", result.Backend!.Name);
        Assert.Equal([3, 4], result.Output!.Data);
    }

    [Fact]
    public void Compile_WhenValidationFails_ShouldReturnDiagnosticsWithoutOutput()
    {
        var compiler = new CodeGenCompiler(
        [
            new FakeBackend<byte[]>(
                "invalid-wasm32",
                [Arch.Wasm32],
                [9],
                false,
                [
                    new Diagnostic(DiagnosticLevel.Error, "validation failed")
                ])
        ]);

        var result = compiler.Compile<byte[]>(
            new GenerateModule("invalid"),
            new CompilationOptions { Target = CompilationTarget.Wasm });

        Assert.False(result.Succeeded);
        Assert.True(result.HasErrors);
        Assert.NotNull(result.Backend);
        Assert.Null(result.Output);
        Assert.Single(result.Diagnostics);
        Assert.Equal("validation failed", result.Diagnostics[0].Message);
    }

    [Fact]
    public void Compile_WithoutExplicitTarget_ShouldDefaultToNyarVmArch()
    {
        var compiler = new CodeGenCompiler(
        [
            new FakeBackend<byte[]>("nyarvm", [Arch.NyarVm], [7])
        ]);

        var result = compiler.Compile<byte[]>(
            new GenerateModule("module"),
            new CompilationOptions());

        Assert.True(result.Succeeded);
        Assert.Equal(Arch.NyarVm, result.TargetArch);
        Assert.Equal("nyarvm", result.Backend!.Name);
    }

    [Fact]
    public void TryCompile_WhenOutputTypeDoesNotMatch_ShouldReturnFalse()
    {
        var compiler = new CodeGenCompiler(
        [
            new FakeBackend<string>("text-wasm32", [Arch.Wasm32], "wat")
        ]);

        var success = compiler.TryCompile<byte[]>(
            new GenerateModule("module"),
            new CompilationOptions { Target = CompilationTarget.Wasm },
            out var result);

        Assert.False(success);
        Assert.False(result.Succeeded);
        Assert.True(result.HasErrors);
        Assert.Null(result.Backend);
        Assert.Null(result.Output);
        Assert.Contains("Byte[]", result.Diagnostics[0].Message);
    }

    [Fact]
    public void WasmConvertModule_WithoutExplicitTarget_ShouldStillUseWasmCompilerPath()
    {
        var module = new GenerateModule("wasm");
        var func = new GenerateFunction("main", "i32");
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Const, new GenerateOperand.I32(42)));
        func.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(func);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));

        var wasmModule = WasmBackend.ConvertModule(module, new CompilationOptions());

        Assert.Equal(1u, wasmModule.Version);
        Assert.NotEmpty(wasmModule.Types);
        Assert.NotEmpty(wasmModule.Codes);
        Assert.Contains(wasmModule.Exports, export => export.Name == "main");
    }

    private sealed class FakeBackend<TOutput> : ICodeGenBackend<TOutput>
    {
        private readonly IReadOnlyList<Diagnostic> _diagnostics;
        private readonly TOutput _output;
        private readonly bool _validate_result;

        public FakeBackend(string name, IReadOnlyList<Arch> supportedArchs, TOutput output,
            bool validateResult = true, IReadOnlyList<Diagnostic>? diagnostics = null)
        {
            this.name = name;
            supported_archs = supportedArchs;
            _output = output;
            _validate_result = validateResult;
            _diagnostics = diagnostics ?? [];
        }

        public string name { get; }

        public IReadOnlyList<Arch> supported_archs { get; }

        public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
        {
            diagnostics = _diagnostics.ToList();
            return _validate_result;
        }

        public OutputSpec<TOutput> compile(GenerateModule module, CompilationOptions options)
        {
            return new OutputSpec<TOutput>
            {
                Data = _output,
                FileExtension = ".bin",
                MediaType = "application/octet-stream"
            };
        }

        OutputSpec ICodeGenBackend.Compile(GenerateModule module, CompilationOptions options)
        {
            return compile(module, options);
        }
    }
}