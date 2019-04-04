namespace Nyar.Types.Targets;

/// <summary>
///     Application Binary Interface
/// </summary>
public enum TargetAbi
{
    /// <summary>
    ///     通用托管运行时
    /// </summary>
    managed,

    /// <summary>
    ///     System V AMD64
    /// </summary>
    system_v,

    /// <summary>
    ///     Microsoft x64
    /// </summary>
    microsoft_x64,

    /// <summary>
    ///     ARM AAPCS
    /// </summary>
    aapcs,

    /// <summary>
    ///     ARM64 AAPCS64
    /// </summary>
    aapcs64,

    /// <summary>
    ///     Common Language Runtime
    /// </summary>
    clr,

    /// <summary>
    ///     Java Virtual Machine
    /// </summary>
    jvm,

    /// <summary>
    ///     WebAssembly
    /// </summary>
    web_assembly,

    /// <summary>
    ///     WASI Preview 1
    /// </summary>
    wasi_p1,

    /// <summary>
    ///     WASI Preview 2（Component Model）
    /// </summary>
    wasi_p2
}
