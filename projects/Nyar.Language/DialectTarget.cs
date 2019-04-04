namespace Nyar.Language;

/// <summary>
///     方言目标，标注语言应编译到的方言
/// </summary>
public enum DialectTarget
{
    /// <summary>
    ///     Core 方言 — 非 GC 系统级语言（C、Rust、WASM）
    /// </summary>
    Core,

    /// <summary>
    ///     Standard 方言 — GC 高级语言及脚本语言（Python、JS、Lua、Bash、PowerShell 等）
    /// </summary>
    Standard
}