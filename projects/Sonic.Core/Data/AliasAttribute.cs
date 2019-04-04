using System;

namespace Core.Data;

/// <summary>
///     为共享数据字段声明兼容别名。
///     `config` 和 `serde` 都可以按需消费这些别名元信息。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class AliasAttribute : Attribute
{
    public AliasAttribute(string name)
    {
        this.name = name;
    }

    public string name { get; }
}
