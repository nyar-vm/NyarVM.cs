using Core.Data;
using Std.Config;

namespace Nyar.PackageManager.Config;

/// <summary>
///     鏉′欢鏂囦欢鍖归厤妯″紡
/// </summary>
[Data]
public class ConditionalFilePattern
{
    /// <summary>
    ///     鏂囦欢鍖归厤妯″紡锛坓lob 鏍煎紡锛?    ///
    /// </summary>
    [ConfigPath(".")]
    public string pattern { get; set; } = string.Empty;

    /// <summary>
    ///     骞冲彴鏉′欢琛ㄨ揪寮忥紝濡?"windows"銆?!linux"銆?arch==wasm"
    ///     绌哄垯濮嬬粓鐢熸晥
    /// </summary>
    public string? condition { get; set; }
}
