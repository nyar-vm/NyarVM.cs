using System.Linq;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Types;
using Nyar.Types.Targets;
using Nyar.VM.NyarVM;
using Nyar.VM.NyarVM.Bytecode;
using ValkyrieCompiler = Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler;
using ValueType = Nyar.Types.ValueType;

namespace Valkyrie.CLI.Compiler;

/// <summary>
///     VCC 编译器，编译阶段直接调用 Valkyrie.Compiler，运行阶段调用 NyarVm。
/// </summary>
public sealed class VccCompiler
{
    private static readonly string[] _supported_targets =
        ["nyar", "wasm", "wasip1", "wasip2", "clr", "jvm", "native"];

    private readonly CanonicalTargetRegistry _canonical_target_registry;
    private readonly ValkyrieCompiler _compiler;

    private readonly NyarVm _vm;

    public VccCompiler()
    {
        _vm = new NyarVm();
        _compiler = new ValkyrieCompiler();
        _canonical_target_registry = new CanonicalTargetRegistry();
    }

    public NyarVm vm => _vm;

    #region 公开编译 API

    public VccBuildResult compile(string file, string target, string outputDir, bool verbose)
    {
        if (!File.Exists(file))
            return new VccBuildResult
            {
                success = false,
                error = $"源文件不存在 '{file}'"
            };

        if (!try_resolve_target(target, out var targetInfo))
            return new VccBuildResult
            {
                success = false,
                error = $"不支持的编译目标 '{target}'，可选：{string.Join(" / ", _supported_targets)}"
            };

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        var moduleName = Path.GetFileNameWithoutExtension(file);

        if (verbose) Console.WriteLine($"  编译：{Path.GetFileName(file)} -> {target}");

        var source = File.ReadAllText(file);
        try
        {
            var artifactSet = compile_to_artifacts(source, moduleName, file, targetInfo);
            return write_output_files(artifactSet, outputDir, verbose);
        }
        catch (InvalidOperationException ex)
        {
            return new VccBuildResult
            {
                success = false,
                error = $"编译失败：{ex.Message}"
            };
        }
    }

    public Value compile_and_run(string file, string target, bool verbose)
    {
        if (!File.Exists(file)) throw new FileNotFoundException($"源文件不存在 '{file}'");

        if (!try_resolve_target(target, out var targetInfo))
            throw new ArgumentException($"不支持的编译目标 '{target}'，可选：{string.Join(" / ", _supported_targets)}");

        if (targetInfo.target.arch != TargetArch.nyar_vm)
            throw new InvalidOperationException("CompileAndRun 仅支持 nyar 目标，请使用 vcc build 生成其他目标产物。");

        var moduleName = Path.GetFileNameWithoutExtension(file);
        if (verbose) Console.WriteLine($"  编译并运行：{Path.GetFileName(file)}");

        var source = File.ReadAllText(file);
        var plan = new BuildPlan(moduleName, targetInfo.canonical_triple, file);

        _compiler.diagnostics.clear();
        var tokens = _compiler.lex(source);
        if (_compiler.diagnostics.has_errors) throw new InvalidOperationException("词法分析失败。");

        var ast = _compiler.parse(tokens);
        if (_compiler.diagnostics.has_errors) throw new InvalidOperationException("语法分析失败。");

        var semantics = _compiler.analyze(ast, plan);
        if (semantics.has_errors)
        {
            var message = string.Join("; ", semantics.diagnostics.Select(d => $"{d.message} @ {d.file_path}"));
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "语义分析失败。" : message);
        }

        var hir = _compiler.build_hir(ast, semantics, plan);
        var targetProfile = _canonical_target_registry.resolve(plan.canonical_triple);
        var mir = _compiler.build_mir(hir, plan, targetProfile);
        var lir = _compiler.build_lir(mir, plan);

        _vm.load(CompilationUnitSerializer.serialize(lir.module));

        var artifacts = _compiler.compile_to_target(source, plan);
        var logicalEntry = artifacts.run_contract?.logical_entry ?? "main";
        if (verbose) Console.WriteLine($"  入口函数: {logicalEntry}");
        var runResult = _vm.run(moduleName, logicalEntry, []);

        if (verbose)
        {
            Console.WriteLine($"  原始值 type={runResult.type} bits=0x{runResult.i32:X}");
            Console.WriteLine($"  结果：{format_value(runResult)}");
        }
        else if (runResult.type != ValueType.@null)
        {
            Console.WriteLine(format_value(runResult));
        }

        return runResult;
    }

    public Value run_nyar_file(string file, string entryFunction, bool verbose)
    {
        if (!File.Exists(file)) throw new FileNotFoundException($".nyar 文件不存在 '{file}'");

        var bytecode = File.ReadAllBytes(file);
        var moduleName = Path.GetFileNameWithoutExtension(file);

        if (verbose) Console.WriteLine($"  加载模块：{moduleName}（{bytecode.Length} 字节）");

        _vm.load(bytecode);
        var result = _vm.run(moduleName, entryFunction, []);

        if (verbose && result.type != ValueType.@null) Console.WriteLine($"  结果：{format_value(result)}");

        return result;
    }

    #endregion

    #region 编译主链

    private ArtifactSet compile_to_artifacts(
        string source,
        string moduleName,
        string filePath,
        CompileTargetInfo targetInfo)
    {
        var plan = new BuildPlan(moduleName, targetInfo.canonical_triple, filePath);
        return _compiler.compile_to_target(source, plan);
    }

    private LirModule compile_to_lir(
        string source,
        string moduleName,
        string filePath,
        string canonicalTriple)
    {
        var plan = new BuildPlan(moduleName, canonicalTriple, filePath);
        var targetContract = _canonical_target_registry.resolve(canonicalTriple);

        _compiler.diagnostics.clear();
        var tokens = _compiler.lex(source);
        if (_compiler.diagnostics.has_errors) throw new InvalidOperationException("词法分析失败。");

        var ast = _compiler.parse(tokens);
        if (_compiler.diagnostics.has_errors) throw new InvalidOperationException("语法分析失败。");

        var semantics = _compiler.analyze(ast, plan);
        if (semantics.has_errors)
        {
            var message = string.Join("; ", semantics.diagnostics.Select(d => $"{d.message} @ {d.file_path}"));
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "语义分析失败。" : message);
        }

        var hir = _compiler.build_hir(ast, semantics, plan);
        var mir = _compiler.build_mir(hir, plan, targetContract);
        return _compiler.build_lir(mir, plan);
    }

    #endregion

    #region 辅助方法

    private static bool try_resolve_target(string target, out CompileTargetInfo targetInfo)
    {
        targetInfo = default;
        if (!CanonicalTarget.try_parse(target, out var triple)) return false;

        var profile = triple.to_profile();
        targetInfo = new CompileTargetInfo(triple.to_compilation_target(), triple.ToString(), profile.host_kind);
        return true;
    }

    private static VccBuildResult write_output_files(
        ArtifactSet artifactSet,
        string outputDir,
        bool verbose)
    {
        var artifacts = artifactSet.enumerate_artifacts().ToList();
        var fileNames = new List<string>(artifacts.Count);
        var mainArtifact = string.Empty;

        foreach (var artifact in artifacts)
        {
            var filePath = Path.Combine(outputDir, artifact.name);
            File.WriteAllBytes(filePath, artifact.content);
            fileNames.Add(artifact.name);

            if (verbose) Console.WriteLine($"  生成：{artifact.name}（{artifact.content.Length} 字节）");
        }

        if (fileNames.Count > 0) mainArtifact = Path.Combine(outputDir, fileNames[0]);

        return new VccBuildResult
        {
            success = true,
            output_directory = outputDir,
            output_files = fileNames,
            main_artifact = mainArtifact
        };
    }

    private static string format_value(Value value)
    {
        return value.type switch
        {
            ValueType.i32 => value.i32.ToString(),
            ValueType.i64 => value.i64.ToString(),
            ValueType.f64 => value.f64.ToString(),
            ValueType.@bool => value.@bool.ToString(),
            ValueType.utf8 => value.utf8?.ToString() ?? "null",
            ValueType.@null => "null",
            _ => $"<{value.type}>"
        };
    }

    #endregion
}
