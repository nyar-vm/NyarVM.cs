namespace Nyar.Types.Targets;

/// <summary>
///     运行时提供者/目标平台厂商
/// </summary>
public enum TargetVendor
{
    /// <summary>
    ///     未指定/通用，默认值
    /// </summary>
    unknown = 0,

    /// <summary>
    ///     Microsoft .NET / CoreCLR 运行时提供者
    /// </summary>
    microsoft,

    /// <summary>
    ///     Unity Mono / IL2CPP 运行时提供者
    /// </summary>
    unity,

    /// <summary>
    ///     独立 Mono 运行时提供者
    /// </summary>
    mono,

    /// <summary>
    ///     OpenJDK HotSpot JVM 运行时提供者
    /// </summary>
    open_jdk,

    /// <summary>
    ///     Android ART / Dalvik 运行时提供者
    /// </summary>
    android,

    /// <summary>
    ///     GraalVM 运行时提供者
    /// </summary>
    graal_vm,

    /// <summary>
    ///     Apple 平台运行时提供者
    /// </summary>
    apple,

    /// <summary>
    ///     标准 PC 平台，兼容 GNU 目标三元组
    /// </summary>
    pc,

    /// <summary>
    ///     Vulkan SDK 运行时提供者
    /// </summary>
    vulkan,

    /// <summary>
    ///     Node.js 运行时提供者
    /// </summary>
    node,

    /// <summary>
    ///     Deno 运行时提供者
    /// </summary>
    deno,

    /// <summary>
    ///     Bun 运行时提供者
    /// </summary>
    bun
}