using Nyar.Types.Targets;
using Std.Data.Binary.Jvm.Data;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler.Backends.Jvm;

/// <summary>
///     JVM 后端，基于元编译模块生成 JVM 字节码。
///     直接使用 Nyar.Binary.Jvm.Data 数据模型和 Nyar.Binary.Jvm.Encode 编码器。
/// </summary>
/// <remarks>
///     本文件为 partial 主入口，承载后端公开 API（compile/validate）。
///     其他实现逻辑分布在同目录的 partial 文件中：
///     <list type="bullet">
///         <item>
///             <see cref="JvmBackend.ClassFile" /> 文件 — Class
///             文件级构建（build_class_file/build_method/build_java_main_wrapper）
///         </item>
///         <item><see cref="JvmBackend.Resolve" /> 文件 — 可达性/入口解析/签名匹配/分支标签验证</item>
///         <item><see cref="JvmBackend.Emit" /> 文件 — JVM 指令发射（emit_instruction 及比较/分支/常量/默认值）</item>
///         <item><see cref="JvmBackend.CodeGen" /> 文件 — 代码生成辅助（偏移/槽位/栈深度）</item>
///         <item><see cref="JvmBackend.Call" /> 文件 — 调用指令发射（emit_call / emit_jvm_external_call / 调用结果调整）</item>
///     </list>
/// </remarks>
public sealed partial class JvmBackend : IStandardBackend<JvmClassFileData>
{
    /// <summary>
    ///     后端名称
    /// </summary>
    public string name => "JVM";

    /// <summary>
    ///     后端支持的目标架构列表。
    /// </summary>
    public IReadOnlyList<TargetArch> supported_archs => [TargetArch.jvm];

    /// <summary>
    ///     编译元编译模块为 JVM Class 文件数据
    /// </summary>
    public OutputSpec<JvmClassFileData> compile(GenerateModule module, CompilationOptions options)
    {
        var outputName = compute_output_name(options.entry_function_name, module.name);
        var className = string.IsNullOrWhiteSpace(options.entry_function_name)
            ? module.name.Replace('.', '/')
            : outputName;
        var classFileData = build_class_file(module, className);

        return new OutputSpec<JvmClassFileData>
        {
            data = classFileData,
            file_extension = ".class",
            media_type = "classfile",
            output_name = outputName
        };
    }

    /// <inheritdoc />
    OutputSpec ICodeGenBackend.compile(GenerateModule module, CompilationOptions options)
    {
        return compile(module, options);
    }

    /// <summary>
    ///     验证元编译模块是否可以被 JVM 后端消费。
    /// </summary>
    public bool validate(GenerateModule module, out List<Diagnostic> diagnostics)
    {
        diagnostics = [];

        if (module.has_witness_dispatch)
        {
            diagnostics.Add(new Diagnostic(
                default,
                "JVM 后端当前不支持 `trait/imply` 的 witness 分派；请改用 `NyarVM` 目标，或先完成静态单态化。",
                DiagnosticSeverity.error));
            return false;
        }

        if (!validate_branch_labels(module, diagnostics)) return false;

        return true;
    }

    /// <summary>
    ///     计算 JVM 输出文件名。
    ///     多入口场景优先使用入口函数短名，避免不同测试入口产物互相覆盖。
    /// </summary>
    private static string compute_output_name(string? entryFunctionName, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(entryFunctionName)) return moduleName;

        var entryName = entryFunctionName;
        var lastDot = entryName.LastIndexOf('.');
        if (lastDot >= 0) entryName = entryName[(lastDot + 1)..];

        return sanitize_output_name(entryName);
    }

    /// <summary>
    ///     规范化 JVM 输出文件名。
    /// </summary>
    private static string sanitize_output_name(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Module";

        var chars = name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        var result = new string(chars);
        if (char.IsDigit(result[0])) result = $"M_{result}";

        return result;
    }
}