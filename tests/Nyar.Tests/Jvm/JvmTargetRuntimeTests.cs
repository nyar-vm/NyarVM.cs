using System.Diagnostics;
using Acorn.Jvm.Data;
using Acorn.Jvm.Encode;
using Nyar.Assembler;
using Nyar.Binary.Nyar.Data;
using Nyar.Types;

namespace Nyar.Tests.Jvm;

public sealed class JvmTargetRuntimeTests
{
    [Fact]
    public void Compile_JvmTarget_CanRunUnderJava()
    {
        var module = create_printable_module();
        var classBytes = compile_jvm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_jvm_java_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var classPath = Path.Combine(tempDir, "HostJvmSmoke.class");
            File.WriteAllBytes(classPath, classBytes);

            var result = run_java(tempDir, "HostJvmSmoke");

            Assert.True(
                result.exit_code == 0,
                $"java exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
            Assert.Contains("hello from jvm target", result.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", result.standard_error, StringComparison.Ordinal);
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
    public void Compile_JvmTarget_I32Arithmetic_CanRunUnderJava()
    {
        var module = create_arithmetic_module();
        var classBytes = compile_jvm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_jvm_arith_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generatedClassPath = Path.Combine(tempDir, "HostJvmArithmetic.class");
            File.WriteAllBytes(generatedClassPath, classBytes);

            var runnerSourcePath = Path.Combine(tempDir, "Runner.java");
            File.WriteAllText(
                runnerSourcePath,
                """
                public final class Runner {
                    public static void main(String[] args) {
                        System.out.println(HostJvmArithmetic.calc());
                    }
                }
                """);

            var javac = run_process("javac", ["Runner.java"], tempDir);
            Assert.True(
                javac.exit_code == 0,
                $"javac exit code: {javac.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{javac.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{javac.standard_error}");

            var java = run_process("java", ["-cp", tempDir, "Runner"], tempDir);
            Assert.True(
                java.exit_code == 0,
                $"java exit code: {java.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{java.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{java.standard_error}");
            Assert.Contains("32", java.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", java.standard_error, StringComparison.Ordinal);
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
    public void Compile_JvmTarget_I64Constant_CanRunUnderJava()
    {
        var module = create_long_constant_module();
        var classBytes = compile_jvm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_jvm_i64_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generatedClassPath = Path.Combine(tempDir, "HostJvmLongConst.class");
            File.WriteAllBytes(generatedClassPath, classBytes);

            var runnerSourcePath = Path.Combine(tempDir, "Runner.java");
            File.WriteAllText(
                runnerSourcePath,
                """
                public final class Runner {
                    public static void main(String[] args) {
                        System.out.println(HostJvmLongConst.value());
                    }
                }
                """);

            var javac = run_process("javac", ["Runner.java"], tempDir);
            Assert.True(
                javac.exit_code == 0,
                $"javac exit code: {javac.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{javac.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{javac.standard_error}");

            var java = run_process("java", ["-cp", tempDir, "Runner"], tempDir);
            Assert.True(
                java.exit_code == 0,
                $"java exit code: {java.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{java.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{java.standard_error}");
            Assert.Contains("42", java.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("ClassFormatError", java.standard_error, StringComparison.Ordinal);
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
    public void Compile_JvmTarget_ConditionalBranch_CanRunUnderJava()
    {
        var module = create_conditional_branch_module();
        var classBytes = compile_jvm_module(module);

        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_jvm_branch_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generatedClassPath = Path.Combine(tempDir, "HostJvmBranch.class");
            File.WriteAllBytes(generatedClassPath, classBytes);

            var runnerSourcePath = Path.Combine(tempDir, "Runner.java");
            File.WriteAllText(
                runnerSourcePath,
                """
                public final class Runner {
                    public static void main(String[] args) {
                        System.out.println(HostJvmBranch.choose());
                    }
                }
                """);

            var javac = run_process("javac", ["Runner.java"], tempDir);
            Assert.True(
                javac.exit_code == 0,
                $"javac exit code: {javac.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{javac.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{javac.standard_error}");

            var java = run_process("java", ["-cp", tempDir, "Runner"], tempDir);
            Assert.True(
                java.exit_code == 0,
                $"java exit code: {java.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{java.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{java.standard_error}");
            Assert.Contains("2", java.standard_output, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", java.standard_error, StringComparison.Ordinal);
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

    private static GenerateModule create_printable_module()
    {
        var module = new GenerateModule("HostJvmSmoke");
        var main = new GenerateFunction("main", "void");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello from jvm target")));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef(
                "jvm_println",
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
        var module = new GenerateModule("HostJvmArithmetic");
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

    private static GenerateModule create_long_constant_module()
    {
        var module = new GenerateModule("HostJvmLongConst");
        var value = new GenerateFunction("value", "i64");
        value.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I64(42)));
        value.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(value);
        module.AddExport(new GenerateModuleExport("value", GenerateExportKind.Function, 0));
        return module;
    }

    private static GenerateModule create_conditional_branch_module()
    {
        var module = new GenerateModule("HostJvmBranch");
        var choose = new GenerateFunction("choose", "i32");
        choose.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(0)));
        choose.AddInstruction(new GenerateInstruction(
            NyarHeadCode.JumpIfFalse,
            new GenerateOperand.Label("else")));
        choose.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(1)));
        choose.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        choose.AddLabel("else");
        choose.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(2)));
        choose.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(choose);
        module.AddExport(new GenerateModuleExport("choose", GenerateExportKind.Function, 0));
        return module;
    }

    private static byte[] compile_jvm_module(GenerateModule module)
    {
        var compiler = new CodeGenCompiler([new JvmBackend()]);
        var options = new CompilationOptions
        {
            Target = CompilationTarget.Jvm
        };
        var compilation = compiler.Compile<JvmClassFileData>(module, options);

        Assert.NotNull(compilation.Output);
        Assert.False(compilation.HasErrors);

        var encoder = new JvmEncoder();
        return encoder.encode(compilation.Output!.Data);
    }

    private static ProcessResult run_java(string classPath, string mainClass)
    {
        return run_process("java", ["-cp", classPath, mainClass], classPath);
    }

    private static ProcessResult run_process(string fileName, IEnumerable<string> arguments, string workingDirectory)
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
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private readonly record struct ProcessResult(int exit_code, string standard_output, string standard_error);
}