using System;

namespace Core.Data;

/// <summary>
///     标记共享的数据字段名。
///     `serde` 与 `config` 都可复用这份键名元信息；`config` 如需路径覆盖，再叠加 `ConfigPath`。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class KeyAttribute : Attribute
{
    public KeyAttribute(string name)
    {
        this.name = name;
    }

    public string name { get; }
}
