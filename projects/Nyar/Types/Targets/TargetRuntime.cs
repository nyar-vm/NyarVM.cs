namespace Nyar.Types.Targets;

/// <summary>
///     目标运行时类型
/// </summary>
public enum TargetRuntime
{
    /// <summary>
    ///     NyarVM 运行时
    /// </summary>
    nyar_vm,

    /// <summary>
    ///     WASI 运行时
    /// </summary>
    wasi,

    /// <summary>
    ///     WebAssembly 运行时
    /// </summary>
    wasm,

    /// <summary>
    ///     CLR（.NET）运行时
    /// </summary>
    clr,

    /// <summary>
    ///     JVM 运行时
    /// </summary>
    jvm,

    /// <summary>
    ///     原生运行时
    /// </summary>
    native
}