using System.Text;
using Nyar.Types.Targets;
using Std.Data.Binary.Wasm;
using Std.Data.Binary.Wasm.Data;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler.Backends.Wasm;

/// <summary>
///     WASM 后端，基于元编译模块生成 WebAssembly 二进制格式。
/// </summary>
/// <remarks>
///     本文件为 partial 主入口，承载后端公开 API（compile/validate/convert_module）。
///     其他实现逻辑分布在同目录的 partial 文件中：
///     <list type="bullet">
///         <item><see cref="WasmBackend.Module" /> 文件 — 模块级构建</item>
///         <item><see cref="WasmBackend.CodeGen" /> 文件 — 单函数代码生成</item>
///         <item><see cref="WasmBackend.ControlFlow" /> 文件 — 结构化控制流发射</item>
///         <item><see cref="WasmBackend.Emit" /> 文件 — 单指令发射</item>
///         <item><see cref="WasmBackend.Resolve" /> 文件 — 可达性/入口/类型映射</item>
///         <item><see cref="WasmBackend.Context" /> 文件 — 嵌套上下文与记录类型</item>
///     </list>
/// </remarks>
public sealed partial class WasmBackend : IStandardBackend<WasmModuleData>
{
    /// <summary>
    ///     后端名称。
    /// </summary>
    public string name => "WASM";

    /// <summary>
    ///     后端支持的目标架构列表。
    /// </summary>
    public IReadOnlyList<TargetArch> supported_archs => [TargetArch.wasm32, TargetArch.wasm64];

    /// <summary>
    ///     编译元编译模块为 WASM 二进制，按选项附加 WAT、JS 胶水、TypeScript 声明、source map 等资产。
    /// </summary>
    public OutputSpec<WasmModuleData> compile(GenerateModule module, CompilationOptions options)
    {
        var wasmModule = build_wasm_module(module, options);

        // 入口函数名优先作为输出名（如 legion.legion -> legion），否则用模块名
        // options.entry_function_name 用于多入口模块场景，显式指定编译目标
        var outputName = compute_output_name(options.entry_function_name, module.name);

        var assets = new List<AssemblerAsset>();

        if (should_generate_js_glue(options))
            assets.Add(new AssemblerAsset
            {
                name = $"{outputName}.mjs",
                content = Encoding.UTF8.GetBytes(WasmJsGlueGenerator.generate(module, outputName)),
                media_type = "text/javascript"
            });

        if (options.generate_wat)
            assets.Add(new AssemblerAsset
            {
                name = $"{outputName}.wat",
                content = Encoding.UTF8.GetBytes(WatTextEmitter.emit(wasmModule)),
                media_type = "application/wat"
            });

        if (options.generate_type_script_decls)
            assets.Add(new AssemblerAsset
            {
                name = $"{outputName}.d.ts",
                content = Encoding.UTF8.GetBytes(WasmTypeScriptDeclGenerator.generate(module, module.name)),
                media_type = "text/typescript"
            });

        if (options.generate_source_map)
            assets.Add(new AssemblerAsset
            {
                name = $"{outputName}.wasm.map",
                content = Encoding.UTF8.GetBytes(WasmSourceMapGenerator.generate($"{outputName}.wasm", module)),
                media_type = "application/json"
            });

        return new OutputSpec<WasmModuleData>
        {
            data = wasmModule,
            file_extension = ".wasm",
            media_type = "application/wasm",
            generate_text_output = options.generate_wat,
            output_name = outputName,
            assets = assets
        };
    }

    /// <inheritdoc />
    OutputSpec ICodeGenBackend.compile(GenerateModule module, CompilationOptions options)
    {
        return compile(module, options);
    }

    /// <summary>
    ///     校验模块是否可被 WASM 后端编译（不支持 witness 分派、要求结构化控制流）。
    /// </summary>
    public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
    {
        diagnostics = [];

        if (module.has_witness_dispatch)
        {
            diagnostics.Add(new Diagnostic(
                default,
                "WASM 后端当前不支持 `trait/imply` 的 witness 分派；请改用 `NyarVM` 目标，或先完成静态单态化。",
                DiagnosticSeverity.error));
            return false;
        }

        if (!supports_structured_control_flow(module, out var controlFlowError))
        {
            diagnostics.Add(new Diagnostic(
                default,
                controlFlowError ??
                "WASM 后端当前不支持该控制流形态；请先将控制流降级为前向结构化 `block/if/loop`。",
                DiagnosticSeverity.error));
            return false;
        }

        return true;
    }

    /// <summary>
    ///     将元编译模块转换为 WASM 模块数据结构，编译失败时抛出 <see cref="InvalidOperationException" />。
    /// </summary>
    public static WasmModuleData convert_module(GenerateModule module, CompilationOptions? options = null)
    {
        var effectiveOptions = create_wasm_compilation_options(options);
        var compiler = new CodeGenCompiler([new WasmBackend()]);
        var result = compiler.compile<WasmModuleData>(module, effectiveOptions);

        if (result.output is not null) return result.output.data;

        throw new InvalidOperationException(
            $"WASM 模块转换失败: {string.Join("; ", result.diagnostics.Select(diagnostic => diagnostic.message))}");
    }

    /// <summary>
    ///     根据用户传入的选项构造 WASM 后端默认编译选项，补齐未设置的字段。
    /// </summary>
    private static CompilationOptions create_wasm_compilation_options(CompilationOptions? options)
    {
        var compilationOptions = new CompilationOptions
        {
            target = options?.target ?? CompilationTarget.wasm,
            optimization_level = options?.optimization_level ?? OptimizationLevel.basic,
            generate_wat = options?.generate_wat ?? false,
            generate_source_map = options?.generate_source_map ?? false,
            generate_type_script_decls = options?.generate_type_script_decls ?? false,
            generate_msil = options?.generate_msil ?? false
        };
        return compilationOptions;
    }

    /// <summary>
    ///     检查模块中所有函数的控制流是否可被降级为 WASM 结构化控制流（block/if/loop）。
    /// </summary>
    private static bool supports_structured_control_flow(GenerateModule module, out string? error)
    {
        foreach (var function in module.functions)
            if (!try_create_structured_control_flow_plan(function, out _, out error))
                return false;

        error = null;
        return true;
    }
}