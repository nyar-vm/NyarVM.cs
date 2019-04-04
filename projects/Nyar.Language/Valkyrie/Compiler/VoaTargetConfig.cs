namespace Nyar.Language.Valkyrie.Compiler;

/// <summary>
///     VOA（Valkyrie Output Assembly）目标平台配置。
///     提供所有支持的编译目标及其元数据。
/// </summary>
public static class VoaTargetConfig
{
    private static readonly List<TargetConfig> _targets =
    [

        new TargetConfig
        {
            Id = "wasm",
            Name = "WebAssembly Browser",
            HasJsGlue = true,
            HasPwa = true,
            OutputFormat = "wasm",
            Arch = "wasm32",
            Abi = "wasm"
        },

        new TargetConfig
        {
            Id = "wasi",
            Name = "WebAssembly WASI",
            HasJsGlue = false,
            HasPwa = false,
            OutputFormat = "wasm",
            Arch = "wasm32",
            Abi = "wasip1"
        },

        new TargetConfig
        {
            Id = "clr",
            Name = "CLR / .NET",
            HasJsGlue = false,
            HasPwa = false,
            OutputFormat = "dll",
            Arch = "msil",
            Abi = "managed"
        },

        new TargetConfig
        {
            Id = "jvm",
            Name = "JVM / Java Virtual Machine",
            HasJsGlue = false,
            HasPwa = false,
            OutputFormat = "class",
            Arch = "jvm",
            Abi = "managed"
        },

        new TargetConfig
        {
            Id = "native",
            Name = "Native Binary",
            HasJsGlue = false,
            HasPwa = false,
            OutputFormat = "exe",
            Arch = "x86_64",
            Abi = "native"
        }
    ];

    /// <summary>
    ///     获取所有支持的目标平台配置列表。
    /// </summary>
    public static IReadOnlyList<TargetConfig> SupportedTargets => _targets;

    /// <summary>
    ///     根据标识符查找目标平台配置。
    /// </summary>
    /// <param name="id">目标平台标识符，不区分大小写。</param>
    /// <returns>匹配的 <see cref="TargetConfig" />，若未找到则返回 <c>null</c>。</returns>
    public static TargetConfig? FindTarget(string id)
    {
        return _targets.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}