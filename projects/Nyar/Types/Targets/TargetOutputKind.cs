namespace Nyar.Types.Targets;

/// <summary>
///     产出类型枚举，描述编译产物的链接形态
/// </summary>
public enum TargetOutputKind
{
    /// <summary>
    ///     可执行文件
    /// </summary>
    executable,

    /// <summary>
    ///     可加载模块（如 .wasm、.nyar、.spv）
    /// </summary>
    module,

    /// <summary>
    ///     WASM Component 模型（WASI Preview 2）
    /// </summary>
    component
}