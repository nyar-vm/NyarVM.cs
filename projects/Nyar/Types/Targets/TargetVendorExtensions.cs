namespace Nyar.Types.Targets;

/// <summary>
///     TargetVendor 扩展方法
/// </summary>
public static class TargetVendorExtensions
{
    /// <param name="vendor">运行时提供者。</param>
    extension(TargetVendor vendor)
    {
        /// <summary>
        ///     将运行时提供者枚举转换为目标三元组中的小写字符串
        /// </summary>
        /// <returns>目标三元组中的小写字符串。</returns>
        public string to_triple_string()
        {
            return vendor switch
            {
                TargetVendor.unknown => "unknown",
                TargetVendor.microsoft => "microsoft",
                TargetVendor.unity => "unity",
                TargetVendor.mono => "mono",
                TargetVendor.open_jdk => "openjdk",
                TargetVendor.android => "android",
                TargetVendor.graal_vm => "graalvm",
                TargetVendor.apple => "apple",
                TargetVendor.pc => "pc",
                TargetVendor.vulkan => "vulkan",
                TargetVendor.node => "node",
                TargetVendor.deno => "deno",
                TargetVendor.bun => "bun",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    ///     尝试将字符串解析为运行时提供者（不区分大小写）
    /// </summary>
    /// <param name="s">要解析的字符串。</param>
    /// <param name="vendor">解析成功时的运行时提供者。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string s, out TargetVendor vendor)
    {
        vendor = TargetVendor.unknown;

        if (string.IsNullOrWhiteSpace(s)) return false;

        var lower = s.ToLowerInvariant();

        foreach (var value in Enum.GetValues<TargetVendor>())
            if (value.to_triple_string() == lower)
            {
                vendor = value;
                return true;
            }

        return false;
    }
}