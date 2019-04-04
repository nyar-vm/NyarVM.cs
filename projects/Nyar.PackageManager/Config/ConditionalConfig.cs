using Core.Data;
using Std.Config;

namespace Nyar.PackageManager.Config;

/// <summary>
///     鏉′欢缂栬瘧閰嶇疆
/// </summary>
[Data]
public class ConditionalConfig
{
    /// <summary>
    ///     鍏ㄥ眬瀹忓畾涔夊父閲忥紙key = 瀹忓悕, value = 鍊兼垨 null锛?    ///
    /// </summary>
    [Key("defines")]
    public Dictionary<string, string?> define_constants { get; set; } = new();

    /// <summary>
    ///     鏉′欢瀹忓畾涔夛紙key = 瀹忓悕, value = 鏉′欢琛ㄨ揪寮忓 "platform==windows"锛?    ///
    /// </summary>
    public Dictionary<string, string> condition_defines { get; set; } = new();

    /// <summary>
    ///     鏉′欢鎺掗櫎鐨勬枃浠跺垪琛?    ///
    /// </summary>
    public List<ConditionalFilePattern> exclude_files { get; set; } = [];

    /// <summary>
    ///     鏉′欢鎺掗櫎鐨勭洰褰曞垪琛?    ///
    /// </summary>
    public List<ConditionalFilePattern> exclude_directories { get; set; } = [];
}
