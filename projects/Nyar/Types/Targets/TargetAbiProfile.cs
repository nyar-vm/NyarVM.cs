namespace Nyar.Types.Targets;

/// <summary>
///     ABI 配置枚举，描述目标平台的 ABI 规范
/// </summary>
public enum TargetAbiProfile
{
    /// <summary>
    ///     原生 ABI
    /// </summary>
    native,

    /// <summary>
    ///     SPIR-V 着色器 ABI
    /// </summary>
    spir_v,

    /// <summary>
    ///     托管运行时 ABI（CLR / JVM）
    /// </summary>
    managed,

    /// <summary>
    ///     WebAssembly ABI
    /// </summary>
    wasm,

    /// <summary>
    ///     WASI Preview 1
    /// </summary>
    wasi_p1,

    /// <summary>
    ///     WASI Preview 2
    /// </summary>
    wasi_p2,

    /// <summary>
    ///     Microsoft Visual C++ ABI
    /// </summary>
    msvc,

    /// <summary>
    ///     macOS / Darwin ABI
    /// </summary>
    darwin,

    /// <summary>
    ///     GNU / System V ABI
    /// </summary>
    gnu
}