namespace Std.App.Server.Systems;

/// <summary>
///     自定义配置节名。
///     默认情况下，系统类名为 OrderSystem 时，自动寻找 OrderSystemOptions 配置类，
///     绑定配置节 "OrderSystem"。使用此属性可以自定义配置节名。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AtlasConfigSectionAttribute : Attribute
{
    /// <summary>
    ///     初始化配置节属性
    /// </summary>
    /// <param name="section">配置节名称</param>
    public AtlasConfigSectionAttribute(string section)
    {
        Section = section;
    }

    /// <summary>
    ///     配置节名称
    /// </summary>
    public string Section { get; }
}