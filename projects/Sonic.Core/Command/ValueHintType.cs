namespace Core.Command;

/// <summary>
///     补全提示类型，为 Shell 补全脚本提供参数类型信息
/// </summary>
public enum ValueHintType
{
    /// <summary>无提示</summary>
    none,

    /// <summary>文件路径</summary>
    file_path,

    /// <summary>目录路径</summary>
    directory_path,

    /// <summary>网络 URL</summary>
    url,

    /// <summary>枚举值</summary>
    enum_value,

    /// <summary>任意文本</summary>
    any
}