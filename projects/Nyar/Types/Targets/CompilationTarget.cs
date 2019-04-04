namespace Nyar.Types.Targets;

/// <summary>
///     兼容旧接口的编译目标视图。
///     主模型已经收敛到 `CanonicalTarget + TargetProfile`，这里只保留最小必要字段，
///     其余概念通过派生属性兼容旧代码。
/// </summary>
public sealed class CompilationTarget
{
    /// <summary>
    ///     目标架构
    /// </summary>
    public TargetArch arch { get; set; }

    /// <summary>
    ///     运行时提供者
    /// </summary>
    public TargetVendor vendor { get; set; }

    /// <summary>
    ///     目标操作系统
    /// </summary>
    public TargetSpecification os { get; set; }

    /// <summary>
    ///     Application Binary Interface
    /// </summary>
    public TargetAbi abi { get; set; }

    /// <summary>
    ///     兼容旧接口的 API 投影。
    ///     新代码应从 `CanonicalTarget` / `TargetProfile` 直接推导宿主能力。
    /// </summary>
    [Obsolete("TargetApi 是旧的近义层，请改用 CanonicalTarget + TargetProfile。")]
    public TargetApi api => infer_api();

    /// <summary>
    ///     兼容旧接口的环境投影
    /// </summary>
    [Obsolete("TargetEnvironment 是旧的近义层，请改用 TargetSpecification 或 TargetHostKind。")]
    public TargetEnvironment environment => infer_environment();

    /// <summary>
    ///     兼容旧接口的运行时投影
    /// </summary>
    [Obsolete("TargetRuntime 是旧的近义层，请改用 TargetHostKind 或 TargetBackendFamily。")]
    public TargetRuntime runtime => infer_runtime();

    /// <summary>
    ///     WebAssembly 目标预设
    /// </summary>
    public static CompilationTarget wasm => new()
    {
        arch = TargetArch.wasm32,
        vendor = TargetVendor.unknown,
        os = TargetSpecification.web,
        abi = TargetAbi.web_assembly
    };

    private TargetApi infer_api()
    {
        if (arch == TargetArch.clr) return TargetApi.net;
        if (arch == TargetArch.jvm) return TargetApi.java;
        if (os == TargetSpecification.windows) return TargetApi.windows;
        if (os == TargetSpecification.web) return TargetApi.web;
        if (vendor is TargetVendor.node or TargetVendor.deno or TargetVendor.bun) return TargetApi.node;
        if (os is TargetSpecification.linux or TargetSpecification.mac_os or TargetSpecification.android)
            return TargetApi.posix;
        return TargetApi.none;
    }

    private TargetEnvironment infer_environment()
    {
        return os switch
        {
            TargetSpecification.web => TargetEnvironment.web,
            TargetSpecification.android or TargetSpecification.ios => TargetEnvironment.mobile,
            _ => TargetEnvironment.native
        };
    }

    private TargetRuntime infer_runtime()
    {
        if (arch == TargetArch.nyar_vm) return TargetRuntime.nyar_vm;
        if (abi is TargetAbi.wasi_p1 or TargetAbi.wasi_p2) return TargetRuntime.wasi;
        if (arch is TargetArch.wasm32 or TargetArch.wasm64) return TargetRuntime.wasm;
        if (arch == TargetArch.clr) return TargetRuntime.clr;
        if (arch == TargetArch.jvm) return TargetRuntime.jvm;
        return TargetRuntime.native;
    }
}