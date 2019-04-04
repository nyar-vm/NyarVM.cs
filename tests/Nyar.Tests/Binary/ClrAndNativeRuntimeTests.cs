using System.ComponentModel;
using System.Diagnostics;
using Nyar.Assembler;
using Nyar.Binary.Clr;
using Nyar.Binary.Clr.Data;
using Nyar.Binary.Clr.Encode;
using Nyar.Binary.Nyar.Data;
using Nyar.Binary.Pe.Scanner;
using Nyar.Types;

namespace Nyar.Tests.Binary;

public sealed class ClrAndNativeRuntimeTests
{
    [Fact]
    public void Compile_ClrTarget_CanRunGeneratedExe()
    {
        var module = create_clr_console_module();
        var executableBytes = compile_clr_module(module);
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_clr_runtime_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exePath = Path.Combine(tempDir, "ClrHostSmoke.exe");
            File.WriteAllBytes(exePath, executableBytes);

            var result = run_process(exePath, tempDir);

            Assert.True(
                result.exit_code == 0,
                $"clr exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
            Assert.Contains("hello from clr target", result.standard_output, StringComparison.Ordinal);
        }
        finally
        {
            try_delete_directory(tempDir);
        }
    }

    [Fact]
    public void Compile_ClrTarget_I32Arithmetic_CanReturnExitCode()
    {
        var module = create_clr_arithmetic_module();
        var executableBytes = compile_clr_module(module);
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_clr_arith_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exePath = Path.Combine(tempDir, "ClrArithmeticSmoke.exe");
            File.WriteAllBytes(exePath, executableBytes);

            var result = run_process(exePath, tempDir);

            Assert.True(
                result.exit_code == 32,
                $"clr exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
            Assert.Equal(string.Empty, result.standard_output);
            Assert.Equal(string.Empty, result.standard_error);
        }
        finally
        {
            try_delete_directory(tempDir);
        }
    }

    [Fact]
    public void Compile_ClrTarget_QualifiedExportedEntry_CanRunGeneratedExe()
    {
        var module = create_clr_qualified_entry_module();
        var executableBytes = compile_clr_module(module);
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_clr_qualified_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exePath = Path.Combine(tempDir, "ClrQualifiedEntry.exe");
            File.WriteAllBytes(exePath, executableBytes);

            var result = run_process(exePath, tempDir);

            Assert.True(
                result.exit_code == 0,
                $"clr exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
            Assert.Contains("hello from qualified clr entry", result.standard_output, StringComparison.Ordinal);
        }
        finally
        {
            try_delete_directory(tempDir);
        }
    }

    [Fact]
    public void Compile_ClrTarget_ConditionalBranch_CanReturnElseExitCode()
    {
        var module = create_clr_conditional_branch_module();
        var executableBytes = compile_clr_module(module);
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_clr_branch_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var exePath = Path.Combine(tempDir, "ClrBranchSmoke.exe");
            File.WriteAllBytes(exePath, executableBytes);

            var result = run_process(exePath, tempDir);

            Assert.True(
                result.exit_code == 2,
                $"clr exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
            Assert.Equal(string.Empty, result.standard_output);
            Assert.Equal(string.Empty, result.standard_error);
        }
        finally
        {
            try_delete_directory(tempDir);
        }
    }

    [Fact]
    public void Compile_WindowsNativeTarget_CanRunGeneratedExe()
    {
        var module = create_native_console_module();
        var result = compile_and_run_native_exe(module);

        Assert.True(
            result.exit_code == 7,
            $"native exe: {result.exe_path}{Environment.NewLine}native exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
        Assert.Contains("hello from windows native", result.standard_output, StringComparison.Ordinal);
        result.Dispose();
    }

    [Fact]
    public void Compile_WindowsNativeTarget_MinimalExeCanRun()
    {
        var module = CreateNativeExitOnlyModule();
        var result = compile_and_run_native_exe(module);

        Assert.True(
            result.exit_code == 0,
            $"native exe: {result.exe_path}{Environment.NewLine}native exit code: {result.exit_code}{Environment.NewLine}stdout:{Environment.NewLine}{result.standard_output}{Environment.NewLine}stderr:{Environment.NewLine}{result.standard_error}");
        Assert.Equal(string.Empty, result.standard_output);
        Assert.Equal(string.Empty, result.standard_error);
        result.Dispose();
    }

    private static GenerateModule create_clr_console_module()
    {
        var module = new GenerateModule("ClrHostSmoke");
        var main = new GenerateFunction("main", "void");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello from clr target")));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef(
                "print_line",
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

    private static GenerateModule create_clr_arithmetic_module()
    {
        var module = new GenerateModule("ClrArithmeticSmoke");
        var main = new GenerateFunction("main", "i32");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(7)));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(5)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Add));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(3)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Mul));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(4)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.I32Sub));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(main);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        return module;
    }

    private static GenerateModule create_native_console_module()
    {
        var module = new GenerateModule("NativeHostSmoke");
        var main = new GenerateFunction("main", "i32");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello from windows native")));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef(
                "print",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = []
                })));
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(7)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(main);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        return module;
    }

    private static GenerateModule create_clr_qualified_entry_module()
    {
        var module = new GenerateModule("ClrQualifiedEntry");

        var helper = new GenerateFunction("helper", "void");
        helper.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(helper);

        var qualifiedEntry = new GenerateFunction("hello_world.main", "void");
        qualifiedEntry.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.Str("hello from qualified clr entry")));
        qualifiedEntry.AddInstruction(new GenerateInstruction(
            NyarHeadCode.CallStatic,
            new GenerateOperand.FuncRef(
                "print_line",
                new GenerateFunctionType
                {
                    Parameters = [GenerateValueType.String],
                    Results = []
                })));
        qualifiedEntry.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(qualifiedEntry);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 1));
        return module;
    }

    private static GenerateModule create_clr_conditional_branch_module()
    {
        var module = new GenerateModule("ClrBranchSmoke");
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
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        return module;
    }

    private static GenerateModule CreateNativeExitOnlyModule()
    {
        var module = new GenerateModule("NativeExitSmoke");
        var main = new GenerateFunction("main", "i32");
        main.AddInstruction(new GenerateInstruction(
            NyarHeadCode.Const,
            new GenerateOperand.I32(0)));
        main.AddInstruction(new GenerateInstruction(NyarHeadCode.Return));
        module.AddFunction(main);
        module.AddExport(new GenerateModuleExport("main", GenerateExportKind.Function, 0));
        return module;
    }

    private static NativeRunResult compile_and_run_native_exe(GenerateModule module)
    {
        var compiler = new CodeGenCompiler([new NativeBackend()]);
        var options = new CompilationOptions
        {
            Target = CompilationTarget.Native
        };
        var compilation = compiler.Compile<NativeCodeInfo>(module, options);

        Assert.NotNull(compilation.Output);
        Assert.False(compilation.HasErrors);

        var executableAsset = compilation.Output!.Assets.Single(asset =>
            asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_native_runtime_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var exePath = Path.Combine(tempDir, executableAsset.Name);
        File.WriteAllBytes(exePath, executableAsset.Content);

        try
        {
            var result = RunProcess(exePath, tempDir, executableAsset.Content);
            return new NativeRunResult(tempDir, exePath, result.ExitCode, result.StandardOutput, result.StandardError);
        }
        catch
        {
            throw;
        }
    }

    private static byte[] compile_clr_module(GenerateModule module)
    {
        var compiler = new CodeGenCompiler([new ClrBackend()]);
        var options = new CompilationOptions
        {
            Target = CompilationTarget.Clr
        };
        var compilation = compiler.Compile<ClrModuleData>(module, options);

        Assert.NotNull(compilation.Output);
        Assert.False(compilation.HasErrors);

        var encoder = new ClrEncoder();
        return encoder.encode(compilation.Output!.Data);
    }

    private static ProcessResult run_process(string filePath, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = filePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static ProcessResult run_process(string filePath, string workingDirectory, byte[] executableBytes)
    {
        try
        {
            return run_process(filePath, workingDirectory);
        }
        catch (Win32Exception ex)
        {
            var peScan = PeScanner.Scan(executableBytes);
            var dumpbinHeaders = try_dumpbin_headers(filePath, workingDirectory);
            var dumpbinImports = try_dumpbin_imports(filePath, workingDirectory);
            var dumpbinUnwind = try_dumpbin_unwind_info(filePath, workingDirectory);
            throw new Xunit.Sdk.XunitException(
                $"failed to launch native executable: {ex.Message}{Environment.NewLine}{peScan}{Environment.NewLine}{dumpbinHeaders}{Environment.NewLine}{dumpbinImports}{Environment.NewLine}{dumpbinUnwind}");
        }
    }

    private static string try_dumpbin_headers(string filePath, string workingDirectory)
    {
        try
        {
            var result = run_process(
                "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\VC\\Tools\\MSVC\\14.44.35207\\bin\\Hostx64\\x64\\dumpbin.exe",
                $"/headers \"{filePath}\"",
                workingDirectory);
            return $"dumpbin /headers:{Environment.NewLine}{result.standard_output}{result.standard_error}";
        }
        catch (Exception ex)
        {
            return $"dumpbin /headers failed: {ex.Message}";
        }
    }

    private static string try_dumpbin_imports(string filePath, string workingDirectory)
    {
        try
        {
            var result = run_process(
                "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\VC\\Tools\\MSVC\\14.44.35207\\bin\\Hostx64\\x64\\dumpbin.exe",
                $"/imports \"{filePath}\"",
                workingDirectory);
            return $"dumpbin /imports:{Environment.NewLine}{result.standard_output}{result.standard_error}";
        }
        catch (Exception ex)
        {
            return $"dumpbin /imports failed: {ex.Message}";
        }
    }

    private static string try_dumpbin_unwind_info(string filePath, string workingDirectory)
    {
        try
        {
            var result = run_process(
                "C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\VC\\Tools\\MSVC\\14.44.35207\\bin\\Hostx64\\x64\\dumpbin.exe",
                $"/unwindinfo \"{filePath}\"",
                workingDirectory);
            return $"dumpbin /unwindinfo:{Environment.NewLine}{result.standard_output}{result.standard_error}";
        }
        catch (Exception ex)
        {
            return $"dumpbin /unwindinfo failed: {ex.Message}";
        }
    }

    private static ProcessResult run_process(string fileName, string arguments, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static void try_delete_directory(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // Ignore temp cleanup failures on Windows file locks.
        }
    }

    private readonly record struct ProcessResult(int exit_code, string standard_output, string standard_error);

    private sealed class NativeRunResult : IDisposable
    {
        public NativeRunResult(string tempDir, string exePath, int exitCode, string standardOutput, string standardError)
        {
            temp_dir = tempDir;
            exe_path = exePath;
            exit_code = exitCode;
            standard_output = standardOutput;
            standard_error = standardError;
        }

        public string temp_dir { get; }
        public string exe_path { get; }
        public int exit_code { get; }
        public string standard_output { get; }
        public string standard_error { get; }

        public void Dispose()
        {
            try_delete_directory(temp_dir);
        }
    }
}