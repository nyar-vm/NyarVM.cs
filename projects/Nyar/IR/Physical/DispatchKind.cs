namespace Nyar.IR.Physical;

/// <summary>
///     对象模型分派策略
/// </summary>
public enum DispatchKind
{
    /// <summary>静态分派（编译期确定目标，无间接开销）。</summary>
    @static,

    /// <summary>见证表分派（通过 Witness Table 间接调用，支持热更新）。</summary>
    witness,

    /// <summary>动态分派（运行时方法查找 + 内联缓存，最灵活但开销最大）。</summary>
    dynamic
}