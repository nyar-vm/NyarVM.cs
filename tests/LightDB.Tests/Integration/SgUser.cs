using System.ComponentModel.DataAnnotations;
using Core.Data;

namespace LightDB.Tests.Integration;

/// <summary>
///     字符串主键实体
/// </summary>
[Entity]
public sealed class SgUser
{
    [Key] public string Name { get; set; } = "";

    public int Age { get; set; }
}