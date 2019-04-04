using System.Diagnostics;
using System.Text;
using Nyar.Assembler;
using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Text.Valkyrie.AST;

namespace Nyar.Tests.LegionBootstrap;

/// <summary>
///     Legion 自举三端编译测试：使用真实 legion.tools + std 源码验证 CLR / JVM / WASM 三端全量编译能力
/// </summary>
public sealed class LegionBootstrapTests
{
    /// <summary>
    ///     Valkyrie 项目根目录
    /// </summary>
    private static readonly string StdRoot = Path.GetFullPath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", "valkyrie.v", "projects"));

    /// <summary>
    ///     收集 legion.tools 所有源文件及其 std 传递依赖（仅 CLR 目标所需文件）
    /// </summary>
    private static string[] GetLegionToolchainSourceFiles()
    {
        var projectRoot = Path.Combine(StdRoot, "..");
        var files = new List<string>
        {
            // legion.tools 自身
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"),
            Path.Combine(StdRoot, "legion.tools", "source", "manifest.v"),
            Path.Combine(StdRoot, "legion.tools", "source", "build_context.v"),

            // std.command
            Path.Combine(StdRoot, "std", "source", "command", "_.v"),
            Path.Combine(StdRoot, "std", "source", "command", "model.v"),
            Path.Combine(StdRoot, "std", "source", "command", "app.v"),
            Path.Combine(StdRoot, "std", "source", "command", "help.v"),
            Path.Combine(StdRoot, "std", "source", "command", "convert.v"),

            // std.io（仅 legion.tools 实际使用的文件）
            Path.Combine(StdRoot, "std", "source", "io", "print.v"),
            Path.Combine(StdRoot, "std", "source", "io", "fs.v"),

            // std.math.graph_theory
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "directed_graph.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "cycle.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "topological_sort.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "traversal.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "transitive_closure.v"),

            // std.data.text.von
            Path.Combine(StdRoot, "std.data.text.von", "source", "_.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "token.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "ast.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "lexer.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "parser.v"),

            // std.data.text.v（von 的传递依赖）
            Path.Combine(StdRoot, "std.data.text.v", "source", "_.v"),
            Path.Combine(StdRoot, "std.data.text.v", "source", "token.v"),

            // std.adaptor.clr（CLR 平台的 adaptor 层）
            Path.Combine(StdRoot, "std.adaptor.clr", "source", "console.v"),
            Path.Combine(StdRoot, "std.adaptor.clr", "source", "io.v"),
        };
        return [.. files.Where(File.Exists)];
    }

    /// <summary>
    ///     简单单文件 CLR 后端编译测试
    /// </summary>
    private const string TestSource = """
        namespace legion_self_test;

        using std.io;

        [main]
        micro main() -> unit {
            let hello: utf8 = "Hello from Legion self-bootstrap!";
            std.io.print_line(hello);
        }
        """;

    [Fact]
    public void CompileToClr_SimpleProgram_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion_self_test", "clr-microsoft-unknown-managed", "test_clr.v");

        var parseResult = compiler.parse_source(TestSource, "test_clr.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        if (semantics.has_errors)
        {
            Assert.Fail($"CLR 语义错误:\n{string.Join("\n", semantics.diagnostics.Select(d => d.message))}");
        }

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var backend = new ClrBackend();
        Assert.True(backend.validate(lir.module, out var diag),
            $"CLR 后端验证失败:\n{string.Join("\n", diag.Select(d => d.ToString()))}");

        var result = backend.compile(lir.module, new CompilationOptions());
        Assert.NotNull(result.data);
    }

    [Fact]
    public void CompileToJvm_SimpleProgram_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion_self_test", "jvm-openjdk-linux-managed", "test_jvm.v");

        var parseResult = compiler.parse_source(TestSource, "test_jvm.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        if (semantics.has_errors)
        {
            Assert.Fail($"JVM 语义错误:\n{string.Join("\n", semantics.diagnostics.Select(d => d.message))}");
        }

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var backend = new JvmBackend();
        Assert.True(backend.validate(lir.module, out var diag),
            $"JVM 后端验证失败:\n{string.Join("\n", diag.Select(d => d.ToString()))}");

        var result = backend.compile(lir.module, new CompilationOptions());
        Assert.NotNull(result.data);
    }

    [Fact]
    public void CompileToWasm_SimpleProgram_ShouldSucceed()
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion_self_test", "wasm32-unknown-browser-wasm", "test_wasm.v");

        var parseResult = compiler.parse_source(TestSource, "test_wasm.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        if (semantics.has_errors)
        {
            Assert.Fail($"WASM 语义错误:\n{string.Join("\n", semantics.diagnostics.Select(d => d.message))}");
        }

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var backend = new WasmBackend();
        Assert.True(backend.validate(lir.module, out var diag),
            $"WASM 后端验证失败:\n{string.Join("\n", diag.Select(d => d.ToString()))}");

        var result = backend.compile(lir.module, new CompilationOptions());
        Assert.NotNull(result.data);
    }

    /// <summary>
    ///     真自举：使用 Valkyrie 编译器从源码编译 legion.tools 及其所有 std 依赖到 CLR 目标
    /// </summary>
    [Fact]
    public void BootstrapLegionTools_ToClr_ShouldSucceed()
    {
        var sourceFiles = GetLegionToolchainSourceFiles();
        Assert.NotEmpty(sourceFiles);

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion.tools", "clr-microsoft-unknown-managed",
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"));

        try
        {
            var artifacts = compiler.compile_files_to_target(sourceFiles, plan);
            Assert.NotNull(artifacts);
            Assert.NotNull(artifacts.primary_artifact);

            var content = artifacts.primary_artifact.content;
            // 输出调试信息：产物名称、媒体类型、大小
            var diagMsg = $"产物: {artifacts.primary_artifact.name}, " +
                          $"类型: {artifacts.primary_artifact.media_type}, " +
                          $"大小: {content.Length} bytes, " +
                          $"sidecar: {artifacts.sidecar_artifacts.Count}";
            if (content.Length == 0)
            {
                Assert.Fail($"CLR 自举编译产出了空文件。{diagMsg}");
            }
            Assert.NotEmpty(content);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail($"CLR 自举编译失败:\n{ex.Message}");
        }
    }

    /// <summary>
    ///     显式程序集名的 CLR 绑定应保留装配信息，供宿主桥接自举使用。
    /// </summary>
    [Fact]
    public void CompileToClr_ExplicitAssemblyBinding_ShouldPreserveAssemblyName()
    {
        const string source = """
            namespace legion_self_host;

            [clr("legion", "Legion.CLI.Interop.ValkyrieBootstrapHost", "BuildProject")]
            micro host_build(project_dir: utf8, target: utf8, output: utf8, verbose: bool) -> i32

            [main]
            micro main() -> unit {
            }
            """;

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion_self_host", "clr-microsoft-unknown-managed", "host_bind.v");
        var parseResult = compiler.parse_source(source, "host_bind.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors, string.Join("\n", semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve(plan.canonical_triple);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var backend = new ClrBackend();
        var result = backend.compile(lir.module, new CompilationOptions());

        Assert.NotNull(result.data);
        var hostBinding = result.data!.external_method_refs.FirstOrDefault(item =>
            item is { method_name: "BuildProject", type_full_name: "Legion.CLI.Interop.ValkyrieBootstrapHost" });

        Assert.NotNull(hostBinding);
        Assert.Equal("legion", hostBinding!.assembly_name);
    }

    /// <summary>
    ///     收集 legion.tools 所有源文件及其 std 传递依赖（根据目标平台筛选 adaptor 文件）
    /// </summary>
    private static string[] GetLegionToolchainSourceFilesForTarget(string target)
    {
        var projectRoot = Path.Combine(StdRoot, "..");
        var files = new List<string>
        {
            // legion.tools 自身
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"),
            Path.Combine(StdRoot, "legion.tools", "source", "manifest.v"),
            Path.Combine(StdRoot, "legion.tools", "source", "build_context.v"),

            // std.command
            Path.Combine(StdRoot, "std", "source", "command", "_.v"),
            Path.Combine(StdRoot, "std", "source", "command", "model.v"),
            Path.Combine(StdRoot, "std", "source", "command", "app.v"),
            Path.Combine(StdRoot, "std", "source", "command", "help.v"),
            Path.Combine(StdRoot, "std", "source", "command", "convert.v"),

            // std.io（仅 legion.tools 实际使用的文件）
            Path.Combine(StdRoot, "std", "source", "io", "print.v"),
            Path.Combine(StdRoot, "std", "source", "io", "fs.v"),

            // std.math.graph_theory
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "directed_graph.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "cycle.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "topological_sort.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "traversal.v"),
            Path.Combine(StdRoot, "std", "source", "math", "graph_theory", "transitive_closure.v"),

            // std.data.text.von
            Path.Combine(StdRoot, "std.data.text.von", "source", "_.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "token.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "ast.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "lexer.v"),
            Path.Combine(StdRoot, "std.data.text.von", "source", "parser.v"),

            // std.data.text.v（von 的传递依赖）
            Path.Combine(StdRoot, "std.data.text.v", "source", "_.v"),
            Path.Combine(StdRoot, "std.data.text.v", "source", "token.v"),
        };

        // 根据目标平台添加对应的 adaptor 文件
        switch (target.ToLowerInvariant())
        {
            case "clr":
                files.Add(Path.Combine(StdRoot, "std.adaptor.clr", "source", "console.v"));
                files.Add(Path.Combine(StdRoot, "std.adaptor.clr", "source", "io.v"));
                break;
            case "jvm":
                files.Add(Path.Combine(StdRoot, "std.adaptor.jvm", "source", "console.v"));
                files.Add(Path.Combine(StdRoot, "std.adaptor.jvm", "source", "io.v"));
                files.Add(Path.Combine(StdRoot, "std.adaptor.jvm", "source", "system.v"));
                break;
            case "wasm":
                files.Add(Path.Combine(StdRoot, "std.adaptor.wasm", "source", "console.v"));
                break;
        }

        return [.. files.Where(File.Exists)];
    }

    /// <summary>
    ///     真自举：使用 Valkyrie 编译器从源码编译 legion.tools 及其所有 std 依赖到 JVM 目标
    /// </summary>
    [Fact]
    public void BootstrapLegionTools_ToJvm_ShouldSucceed()
    {
        var sourceFiles = GetLegionToolchainSourceFilesForTarget("jvm");
        Assert.NotEmpty(sourceFiles);

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion.tools", "jvm-openjdk-linux-managed",
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"));

        try
        {
            var artifacts = compiler.compile_files_to_target(sourceFiles, plan);
            Assert.NotNull(artifacts);
            Assert.NotNull(artifacts.primary_artifact);

            var content = artifacts.primary_artifact.content;
            var diagMsg = $"产物: {artifacts.primary_artifact.name}, " +
                          $"类型: {artifacts.primary_artifact.media_type}, " +
                          $"大小: {content.Length} bytes, " +
                          $"sidecar: {artifacts.sidecar_artifacts.Count}";
            if (content.Length == 0)
            {
                Assert.Fail($"JVM 自举编译产出了空文件。{diagMsg}");
            }
            Assert.NotEmpty(content);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail($"JVM 自举编译失败:\n{ex.Message}");
        }
    }

    /// <summary>
    ///     真自举：使用 Valkyrie 编译器从源码编译 legion.tools 及其所有 std 依赖到 WASM 目标
    /// </summary>
    [Fact]
    public void BootstrapLegionTools_ToWasm_ShouldSucceed()
    {
        var sourceFiles = GetLegionToolchainSourceFilesForTarget("wasm");
        Assert.NotEmpty(sourceFiles);

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion.tools", "wasm32-unknown-browser-wasm",
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"));

        try
        {
            var artifacts = compiler.compile_files_to_target(sourceFiles, plan);
            Assert.NotNull(artifacts);
            Assert.NotNull(artifacts.primary_artifact);

            var content = artifacts.primary_artifact.content;
            var diagMsg = $"产物: {artifacts.primary_artifact.name}, " +
                          $"类型: {artifacts.primary_artifact.media_type}, " +
                          $"大小: {content.Length} bytes, " +
                          $"sidecar: {artifacts.sidecar_artifacts.Count}";
            if (content.Length == 0)
            {
                Assert.Fail($"WASM 自举编译产出了空文件。{diagMsg}");
            }
            Assert.NotEmpty(content);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail($"WASM 自举编译失败:\n{ex.Message}");
        }
    }

    /// <summary>
    ///     运行时验证：编译 legion.tools 到 WASM，在 Node.js 中运行 --version，
    ///     确保 WASM 模块可实例化且输出正确版本信息。
    /// </summary>
    [Fact]
    public void RunLegionTools_Wasm_InNodeJs_Version_ShouldOutputVersion()
    {
        var sourceFiles = GetLegionToolchainSourceFilesForTarget("wasm");
        Assert.NotEmpty(sourceFiles);

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion.tools", "wasm32-unknown-browser-wasm",
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"));

        ArtifactSet artifacts;
        try
        {
            artifacts = compiler.compile_files_to_target(sourceFiles, plan);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail($"WASM 自举编译失败:\n{ex.Message}");
            return;
        }

        Assert.NotNull(artifacts.primary_artifact);
        var wasmBytes = artifacts.primary_artifact.content;
        Assert.NotEmpty(wasmBytes);

        var moduleName = "legion_tools";
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_legion_wasm_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var wasmPath = Path.Combine(tempDir, $"{moduleName}.wasm");
            File.WriteAllBytes(wasmPath, wasmBytes);

            var mjsPath = Path.Combine(tempDir, $"{moduleName}.mjs");
            var mjsContent = build_wasm_runner_mjs(moduleName);
            File.WriteAllText(mjsPath, mjsContent, Encoding.UTF8);

            var result = run_process("node", mjsPath, tempDir);

            // 基本验证：WASM 模块能成功实例化，不应该抛出异常
            if (result.exit_code != 0)
            {
                Assert.Fail(
                    $"WASM 模块实例化失败 (exit={result.exit_code}):\nstdout:\n{result.standard_output}\nstderr:\n{result.standard_error}");
            }

            // --version 应输出版本号
            Assert.Contains("Legion", result.standard_output, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // 忽略 Windows 文件锁导致的临时目录清理失败
            }
        }
    }

    /// <summary>
    ///     运行时验证：编译 legion.tools 到 JVM，在 Java 中运行 --version，
    ///     确保 JVM Class 文件可执行且输出正确版本信息。
    /// </summary>
    [Fact]
    public void RunLegionTools_Jvm_Version_ShouldOutputVersion()
    {
        var sourceFiles = GetLegionToolchainSourceFilesForTarget("jvm");
        Assert.NotEmpty(sourceFiles);

        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("legion.tools", "jvm-openjdk-linux-managed",
            Path.Combine(StdRoot, "legion.tools", "source", "_.v"));

        ArtifactSet artifacts;
        try
        {
            artifacts = compiler.compile_files_to_target(sourceFiles, plan);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Fail($"JVM 自举编译失败:\n{ex.Message}");
            return;
        }

        Assert.NotNull(artifacts.primary_artifact);
        var classBytes = artifacts.primary_artifact.content;
        Assert.NotEmpty(classBytes);

        const string debugClassPath = @"C:\Temp\legion_tools_jvm_debug.class";
        File.WriteAllBytes(debugClassPath, classBytes);

        var className = "legion.tools";
        var tempDir = Path.Combine(Path.GetTempPath(), $"nyar_legion_jvm_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // JVM class 文件需要放在对应的包目录结构下
            var classSubDir = Path.Combine(tempDir, "legion");
            Directory.CreateDirectory(classSubDir);
            var classPath = Path.Combine(classSubDir, "tools.class");
            File.WriteAllBytes(classPath, classBytes);

            // 调试：用 javap 反编译查看常量池和方法签名
            var javapInfo = run_javap(classPath, tempDir, className);

            var result = run_jvm_process(className, tempDir, "--version");

            if (result.exit_code != 0)
            {
                Assert.Fail(
                    $"JVM Class 执行失败 (exit={result.exit_code}):\nstdout:\n{result.standard_output}\nstderr:\n{result.standard_error}\n\njavap:\n{javapInfo}");
            }

            // --version 应输出版本号
            Assert.Contains("Legion", result.standard_output, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // 忽略 Windows 文件锁导致的临时目录清理失败
            }
        }
    }

    /// <summary>
    ///     构建用于测试 legion.tools WASM 的 Node.js .mjs 运行器。
    ///     提供 env.console_log 和 env.memory 导入，调用 main(["--version"])。
    /// </summary>
    private static string build_wasm_runner_mjs(string moduleName)
    {
        return $$"""
                 import { readFile } from "node:fs/promises";

                 const decoder = new TextDecoder("utf-8");
                 const encoder = new TextEncoder();

                 const capturedOutput = [];

                 function createImports(memory) {
                     return {
                         env: {
                             memory,
                             console_log(ptr) {
                                 const memoryView = new Uint8Array(memory.buffer);
                                 let end = ptr;
                                 while (end < memoryView.length && memoryView[end] !== 0) {
                                     end++;
                                 }
                                 const bytes = memoryView.subarray(ptr, end);
                                 const msg = decoder.decode(bytes);
                                 capturedOutput.push(msg);
                                 console.log(msg);
                             }
                         }
                     };
                 }

                 async function main() {
                     const moduleUrl = new URL("./{{moduleName}}.wasm", import.meta.url);
                     const wasmBytes = await readFile(moduleUrl);

                     const memory = new WebAssembly.Memory({ initial: 16 });
                     const imports = createImports(memory);
                     const { instance } = await WebAssembly.instantiate(wasmBytes, imports);

                     const entryFn = instance.exports.main ?? instance.exports._start;
                     if (typeof entryFn !== "function") {
                         console.log("exported functions:", Object.keys(instance.exports));
                         throw new Error("找不到入口函数 main");
                     }

                     const args = ["--version"];
                     const argvPtr = encodeArgs(memory, args);
                     entryFn(argvPtr, args.length);

                     if (capturedOutput.length > 0) {
                         console.log("CAPTURED:", capturedOutput.join("\nCAPTURED: "));
                     }
                 }

                 function encodeArgs(memory, args) {
                     const mem = new Uint8Array(memory.buffer);
                     const ptrSize = 8;
                     const arrayByteSize = args.length * ptrSize;
                     let offset = 1024;

                     const argvPtr = offset;
                     offset += arrayByteSize;

                     const dataView = new DataView(memory.buffer);
                     for (let i = 0; i < args.length; i++) {
                         const argBytes = encoder.encode(args[i] + "\0");
                         const argPtr = offset;
                         mem.set(argBytes, offset);
                         offset += argBytes.length;

                         dataView.setInt32(argvPtr + i * ptrSize, argPtr, true);
                         dataView.setInt32(argvPtr + i * ptrSize + 4, argBytes.length - 1, true);
                     }

                     return argvPtr;
                 }

                 await main();
                 """;
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

    private static ProcessResult run_jvm_process(string className, string classPath, string args)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "java",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = classPath
        };
        process.StartInfo.ArgumentList.Add("-cp");
        process.StartInfo.ArgumentList.Add(classPath);
        process.StartInfo.ArgumentList.Add(className);
        process.StartInfo.ArgumentList.Add(args);

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    /// <summary>
    ///     运行 javap 反编译 class 文件获取常量池和方法签名，用于调试。
    /// </summary>
    private static string run_javap(string classPath, string classDir, string className)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "javap",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = classDir
        };
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add("-verbose");
        process.StartInfo.ArgumentList.Add("-p");
        process.StartInfo.ArgumentList.Add(classPath);

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        return standardOutput.Length > 0 ? standardOutput : standardError;
    }

    private readonly record struct ProcessResult(int exit_code, string standard_output, string standard_error);
}
