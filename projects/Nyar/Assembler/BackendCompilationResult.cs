using Nyar.Types.Targets;
using Std.Data.Text.Diagnostics;

namespace Nyar.Assembler;

/// <summary>
///     强类型后端编译结果。
///     将后端选择、验证诊断与最终输出收口到同一个返回对象中，避免上层丢失上下文。
/// </summary>
/// <typeparam name="TOutput">目标输出类型。</typeparam>
public sealed record BackendCompilationResult<TOutput>
{
    /// <summary>
    ///     实际参与选择的目标架构。
    /// </summary>
    public required TargetArch target_arch { get; init; }

    /// <summary>
    ///     最终选中的强类型后端。
    ///     如果没有匹配后端或验证失败，则可能为空。
    /// </summary>
    public ICodeGenBackend<TOutput>? backend { get; init; }

    /// <summary>
    ///     编译输出。
    ///     仅当成功通过验证并完成编译时存在。
    /// </summary>
    public OutputSpec<TOutput>? output { get; init; }

    /// <summary>
    ///     后端选择与验证阶段累计的诊断信息。
    /// </summary>
    public IReadOnlyList<Diagnostic> diagnostics { get; init; } = [];

    /// <summary>
    ///     是否存在错误级诊断。
    /// </summary>
    public bool has_errors
    {
        get
        {
            foreach (var diagnostic in diagnostics)
                if (diagnostic.severity.is_error_level())
                    return true;

            return false;
        }
    }

    /// <summary>
    ///     是否成功得到强类型输出。
    /// </summary>
    public bool succeeded => backend is not null && output is not null && !has_errors;
}