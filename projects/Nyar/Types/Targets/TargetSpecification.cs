namespace Nyar.Types.Targets;

/// <summary>
///     TargetSpecification
/// </summary>
public enum TargetSpecification
{
    /// <summary>
    ///     未知 / 不适用（虚拟机 / GPU 等没有标准 OS 概念的平台）
    /// </summary>
    unknown,

    /// <summary>
    ///     Web
    /// </summary>
    web,

    /// <summary>
    ///     Windows
    /// </summary>
    windows,

    /// <summary>
    ///     Linux
    /// </summary>
    linux,

    /// <summary>
    ///     macOS
    /// </summary>
    mac_os,

    /// <summary>
    ///     Android
    /// </summary>
    android,

    /// <summary>
    ///     iOS
    /// </summary>
    ios,

    /// <summary>
    ///     WASI（WebAssembly System Interface）
    /// </summary>
    wasi
}