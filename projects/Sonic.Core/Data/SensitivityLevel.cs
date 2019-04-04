namespace Core.Data;

/// <summary>
///     敏感数据的脱敏级别。
/// </summary>
public enum SensitivityLevel
{
    /// <summary>
    ///     无脱敏，数据原样输出。
    /// </summary>
    none = 0,

    /// <summary>
    ///     部分遮蔽，保留首尾字符。
    /// </summary>
    partial = 1,

    /// <summary>
    ///     完全遮蔽，输出固定掩码。
    /// </summary>
    full = 2
}