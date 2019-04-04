namespace Nyar.Types.Targets;

/// <summary>
///     宿主类型枚举，描述代码最终由哪类宿主承载。
///     这里刻意保持粗粒度，避免把 OS / vendor / storefront 再拆成一套重复概念。
/// </summary>
public enum TargetHostKind
{
    /// <summary>
    ///     未知宿主
    /// </summary>
    unknown,

    /// <summary>
    ///     NyarVM 运行时
    /// </summary>
    nyar_vm,

    /// <summary>
    ///     GnosisVM 运行时
    /// </summary>
    gnosis_vm,

    /// <summary>
    ///     JVM 运行时
    /// </summary>
    jvm,

    /// <summary>
    ///     .NET 运行时
    /// </summary>
    dotnet,

    /// <summary>
    ///     JavaScript 宿主（Node / Deno / Bun / VS Code Extension Host）
    /// </summary>
    java_script,

    /// <summary>
    ///     原生应用宿主（Windows / Linux / macOS / Android / iOS）
    /// </summary>
    native,

    /// <summary>
    ///     浏览器宿主
    /// </summary>
    browser,

    /// <summary>
    ///     WASI 宿主
    /// </summary>
    wasi,

    /// <summary>
    ///     GPU / shader 宿主
    /// </summary>
    gpu
}