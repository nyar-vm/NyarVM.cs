using Core.Config;

namespace Sonic.Testing.Config;

/// <summary>
///     基础测试配置类，用于验证源代码生成器的 Builder、IConfigurable 实现。
/// </summary>
[Config]
public sealed class BasicConfig
{
    /// <summary>
    ///     服务器主机名。
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    ///     服务器端口号。
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    ///     是否启用调试模式。
    /// </summary>
    public bool Debug { get; set; } = false;
}

/// <summary>
///     带验证特性的测试配置类。
/// </summary>
[Config]
public sealed class ValidatedConfig
{
    /// <summary>
    ///     必填的应用名称。
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string AppName { get; set; } = "";

    /// <summary>
    ///     敏感的 API 密钥。
    /// </summary>
    [Sensitive]
    public string ApiKey { get; set; } = "";

    /// <summary>
    ///     超时秒数。
    /// </summary>
    public int Timeout { get; set; } = 30;
}

/// <summary>
///     带 [Append] 合并策略的测试配置类。
/// </summary>
[Config]
public sealed class AppendConfig
{
    /// <summary>
    ///     追加合并的服务器列表。
    /// </summary>
    [Append]
    public List<string> Servers { get; set; } = [];

    /// <summary>
    ///     超时秒数。
    /// </summary>
    public int Timeout { get; set; } = 10;
}