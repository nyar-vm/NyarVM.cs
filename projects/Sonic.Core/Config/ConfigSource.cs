namespace Core.Config;

/// <summary>
///     配置值的来源类型。
/// </summary>
public enum ConfigSource
{
    /// <summary>
    ///     默认值。
    /// </summary>
    @default = 0,

    /// <summary>
    ///     配置文件。
    /// </summary>
    file = 1,

    /// <summary>
    ///     环境变量。
    /// </summary>
    environment = 2,

    /// <summary>
    ///     命令行参数。
    /// </summary>
    command_line = 3
}