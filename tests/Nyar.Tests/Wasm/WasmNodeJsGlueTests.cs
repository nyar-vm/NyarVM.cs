using System.Diagnostics;
using System.Text;
using Acorn.Wasm.Data;
using Nyar.Assembler;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;

namespace Nyar.Tests.Wasm;

public sealed class WasmNodeJsGlueTests
{
    [Fact]
    public void Compile_NodeJsGlue_CanInstantiateAndRunUnderNode()
    {
        var module = create_console_print_module();
        var (wasmAsset, glueAsset) = compile_wasm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_wasm_node_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, wasmAsset.Name), wasmAsset.Content);
            File.WriteAllBytes(Path.Combine(tempDir, glueAsset.Name), glueAsset.Content);

            var runnerPath = Path.Combine(tempDir, "runner.mjs");
            File.WriteAllText(
                runnerPath,
                """
                import instantiate from "./node_glue_smoke.mjs";

                const instance = await instantiate();
                instance.exports.main();
                """,
                Encoding.UTF8);

            var result = run_process("node", runnerPath, tempDir);

            Assert.Equal(0, result.exit_code);
            Assert.Contains("hello from wasm node glue", result.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed to load WASM module", result.standard_error, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore temp cleanup failures on Windows file locks.
            }
        }
    }

    [Fact]
    public void Compile_NodeJsGlue_I32Arithmetic_CanRunUnderNode()
    {
        var module = create_arithmetic_module();
        var (wasmAsset, glueAsset) = compile_wasm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_wasm_arith_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, wasmAsset.Name), wasmAsset.Content);
            File.WriteAllBytes(Path.Combine(tempDir, glueAsset.Name), glueAsset.Content);

            var runnerPath = Path.Combine(tempDir, "runner.mjs");
            File.WriteAllText(
                runnerPath,
                """
                import instantiate from "./node_arith_smoke.mjs";

                const instance = await instantiate();
                console.log(instance.exports.calc());
                """,
                Encoding.UTF8);

            var result = run_process("node", runnerPath, tempDir);

            Assert.Equal(0, result.exit_code);
            Assert.Contains("32", result.standard_output, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore temp cleanup failures on Windows file locks.
            }
        }
    }

    [Fact]
    public void Compile_NodeJsGlue_CanRunGlueFileDirectlyUnderNode()
    {
        var module = create_console_print_module();
        var (wasmAsset, glueAsset) = compile_wasm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_wasm_direct_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllBytes(Path.Combine(tempDir, wasmAsset.Name), wasmAsset.Content);
            File.WriteAllBytes(Path.Combine(tempDir, glueAsset.Name), glueAsset.Content);

            var result = run_process("node", Path.Combine(tempDir, glueAsset.Name), tempDir);

            Assert.Equal(0, result.exit_code);
            Assert.Contains("hello from wasm node glue", result.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("Failed to load WASM module", result.standard_error, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Ignore temp cleanup failures on Windows file locks.
            }
        }
    }

    private static GenerateModule create_console_print_module()
    {
        var module = new GenerateModule("node_glue_smoke");
        var main = new GenerateFunction("main", "void");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello from wasm node glue")));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef(
                "print",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = []
                })));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(main);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        return module;
    }

    private static GenerateModule create_arithmetic_module()
    {
        var module = new GenerateModule("node_arith_smoke");
        var calc = new GenerateFunction("calc", "i32");
        calc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(7)));
        calc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(5)));
        calc.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Add));
        calc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(3)));
        calc.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Mul));
        calc.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(4)));
        calc.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Sub));
        calc.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(calc);
        module.AddExport(new GenerateModuleExport("calc", GenerateExportKind.Function, 0));
        return module;
    }

    private static (AssemblerAsset WasmAsset, AssemblerAsset GlueAsset) compile_wasm_module(GenerateModule module)
    {
        var compiler = new CodeGenCompiler([new WasmBackend()]);
        var options = new CompilationOptions
        {
            Target = CompilationTarget.Wasm
        };
        var compilation = compiler.Compile<WasmModuleData>(module, options);

        Assert.NotNull(compilation.Output);
        Assert.False(compilation.HasErrors);

        var output = compilation.Output!;
        var wasmAsset = output.Assets.Single(asset => asset.Name.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase));
        var glueAsset = output.Assets.Single(asset => asset.Name.EndsWith(".mjs", StringComparison.OrdinalIgnoreCase));
        return (wasmAsset, glueAsset);
    }

    private static ProcessResult run_process(string fileName, string argument, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };
        process.StartInfo.ArgumentList.Add(argument);

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private readonly record struct ProcessResult(int exit_code, string standard_output, string standard_error);
}