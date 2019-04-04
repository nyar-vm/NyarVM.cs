namespace Nyar.Types.Targets;

/// <summary>
///     TargetAbi 扩展方法
/// </summary>
public static class TargetAbiExtensions
{
    /// <summary>
    ///     将 ABI 枚举转换为目标三元组中的小写字符串
    /// </summary>
    /// <param name="abi">Application Binary Interface。</param>
    /// <returns>目标三元组中的小写字符串。</returns>
    public static string to_triple_string(this TargetAbi abi)
    {
        return abi switch
        {
            TargetAbi.managed => "managed",
            TargetAbi.system_v => "gnu",
            TargetAbi.microsoft_x64 => "msvc",
            TargetAbi.aapcs => "aapcs",
            TargetAbi.aapcs64 => "aapcs64",
            TargetAbi.web_assembly => "wasm",
            TargetAbi.clr => "clr",
            TargetAbi.jvm => "jvm",
            TargetAbi.wasi_p1 => "wasip1",
            TargetAbi.wasi_p2 => "wasip2",
            _ => "unknown"
        };
    }

    /// <summary>
    ///     尝试将字符串解析为 ABI
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <param name="abi">解析成功时的 ABI。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string s, out TargetAbi abi)
    {
        abi = default;
        var lower = s.ToLowerInvariant();

        switch (lower)
        {
            case "managed":
                abi = TargetAbi.managed;
                return true;
            case "gnu":
                abi = TargetAbi.system_v;
                return true;
            case "msvc":
                abi = TargetAbi.microsoft_x64;
                return true;
            case "aapcs":
                abi = TargetAbi.aapcs;
                return true;
            case "aapcs64":
                abi = TargetAbi.aapcs64;
                return true;
            case "webassembly":
            case "wasm":
                abi = TargetAbi.web_assembly;
                return true;
            case "clr":
            case "il2cpp":
            case "nativeaot":
                abi = TargetAbi.clr;
                return true;
            case "jvm":
            case "dex":
                abi = TargetAbi.jvm;
                return true;
            case "wasip1":
                abi = TargetAbi.wasi_p1;
                return true;
            case "wasip2":
                abi = TargetAbi.wasi_p2;
                return true;
            default:
                return false;
        }
    }
}
