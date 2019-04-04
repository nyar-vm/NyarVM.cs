namespace Nyar.Types.Targets;

/// <summary>
///     目标架构定义
/// </summary>
public enum TargetArch
{
    /// <summary>
    ///     NyarVM 虚拟机
    /// </summary>
    /// <remarks>此为虚拟机架构，非物理 CPU 架构。</remarks>
    nyar_vm = 0,

    /// <summary>
    ///     x86 32 位
    /// </summary>
    x86,

    /// <summary>
    ///     x86_64 64 位
    /// </summary>
    x86_64,

    /// <summary>
    ///     ARM 32 位
    /// </summary>
    arm,

    /// <summary>
    ///     ARM64
    /// </summary>
    a_arch64,

    /// <summary>
    ///     RISC-V 32 位
    /// </summary>
    risc_v32,

    /// <summary>
    ///     RISC-V 64 位
    /// </summary>
    risc_v64,

    /// <summary>
    ///     WebAssembly 32 位
    /// </summary>
    wasm32,

    /// <summary>
    ///     WebAssembly 64 位
    /// </summary>
    wasm64,

    /// <summary>
    ///     CLR 虚拟机
    /// </summary>
    /// <remarks>此为虚拟机架构，非物理 CPU 架构。</remarks>
    clr,

    /// <summary>
    ///     JVM 虚拟机
    /// </summary>
    /// <remarks>此为虚拟机架构，非物理 CPU 架构。</remarks>
    jvm,

    /// <summary>
    ///     原生平台（根据运行时环境自动选择）
    /// </summary>
    native,

    /// <summary>
    ///     NVIDIA CUDA
    /// </summary>
    cuda,

    /// <summary>
    ///     AMD GCN
    /// </summary>
    gcn,

    /// <summary>
    ///     SPIR-V
    /// </summary>
    spirv,

    /// <summary>
    ///     Metal Shading Language
    /// </summary>
    msl
}