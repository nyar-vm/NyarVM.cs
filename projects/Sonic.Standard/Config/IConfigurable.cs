namespace Std.Config;

/// <summary>
///     配置对象的标准契约，提供自我验证、自我描述、深克隆和重置能力�?///
/// </summary>
public interface IConfigurable
{
    /// <summary>
    ///     验证当前配置，返回所有错误消息。空集合表示验证通过�?    ///
    /// </summary>
    /// <returns>验证错误列表，空集合表示合法�?/returns>
    IReadOnlyList<string> validate();

    /// <summary>
    ///     返回该配置对应的 JSON Schema 字符串�?    ///
    /// </summary>
    /// <returns>JSON Schema 字符串�?/returns>
    string get_schema();

    /// <summary>
    ///     创建当前配置的深克隆副本�?    ///
    /// </summary>
    /// <returns>深克隆的配置对象�?/returns>
    object clone();

    /// <summary>
    ///     将所有字段重置为编译时确定的默认值�?    ///
    /// </summary>
    void reset_to_defaults();
}