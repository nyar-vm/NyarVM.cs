namespace Nyar.Types.Targets;

/// <summary>
///     后端家族枚举，描述代码生成后端所属的运行时家族
/// </summary>
public enum TargetBackendFamily
{
    /// <summary>
    ///     NyarVM 后端
    /// </summary>
    nyar_vm,

    /// <summary>
    ///     GnosisVM 后端
    /// </summary>
    gnosis_vm,

    /// <summary>
    ///     CLR / .NET 后端
    /// </summary>
    clr,

    /// <summary>
    ///     JVM 后端
    /// </summary>
    jvm,

    /// <summary>
    ///     WebAssembly 后端
    /// </summary>
    wasm,

    /// <summary>
    ///     SPIR-V 着色器后端
    /// </summary>
    spirv,

    /// <summary>
    ///     原生机器码后端
    /// </summary>
    native,

    /// <summary>
    ///     GPU 后端（CUDA / GCN / MSL）
    /// </summary>
    gpu,

    /// <summary>
    ///     未知后端
    /// </summary>
    unknown
}