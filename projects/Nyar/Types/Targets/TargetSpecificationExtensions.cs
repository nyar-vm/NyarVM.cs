namespace Nyar.Types.Targets;

/// <summary>
///     TargetSpecification 扩展方法
/// </summary>
public static class TargetSpecificationExtensions
{
    /// <param name="os">操作系统。</param>
    extension(TargetSpecification os)
    {
        /// <summary>
        ///     将操作系统枚举转换为目标三元组中的小写字符串
        /// </summary>
        /// <returns>目标三元组中的小写字符串。</returns>
        public string to_triple_string()
        {
            return os switch
            {
                TargetSpecification.windows => "windows",
                TargetSpecification.linux => "linux",
                TargetSpecification.mac_os => "darwin",
                TargetSpecification.web => "browser",
                TargetSpecification.android => "android",
                TargetSpecification.ios => "ios",
                TargetSpecification.wasi => "wasi",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    ///     尝试将字符串解析为操作系统
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <param name="os">解析成功时的操作系统。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string s, out TargetSpecification os)
    {
        os = default;
        var lower = s.ToLowerInvariant();

        switch (lower)
        {
            case "unknown":
            case "native":
            case "none":
                os = default;
                return true;
            case "windows":
                os = TargetSpecification.windows;
                return true;
            case "linux":
                os = TargetSpecification.linux;
                return true;
            case "darwin":
            case "macos":
                os = TargetSpecification.mac_os;
                return true;
            case "ios":
                os = TargetSpecification.ios;
                return true;
            case "android":
                os = TargetSpecification.android;
                return true;
            case "web":
            case "browser":
                os = TargetSpecification.web;
                return true;
            case "wasi":
                os = TargetSpecification.wasi;
                return true;
            default:
                return false;
        }
    }
}