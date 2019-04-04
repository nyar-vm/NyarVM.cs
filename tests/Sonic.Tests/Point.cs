using System.ComponentModel.DataAnnotations.Schema;
using Std.Binary.Attributes;
using Std.DataProcess;

namespace Sonic.Testing.Data;

/// <summary>
///     二维点，用于测试 [Data] 标记的 class 类型
/// </summary>
[Data]
public class Point2D
{
    public double x { get; set; }
    public double y { get; set; }
}

/// <summary>
///     三维点，用于测试 [Data] 标记的 struct 类型
/// </summary>
[Data]
public struct Point3D
{
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
}

/// <summary>
///     用户实体，用于测试 DataContract 验证属性和 DataStorage 存储属性
/// </summary>
[Data(version = 2, description = "用户实体")]
public class User
{
    [PrimaryKey] public int id { get; set; }

    [StringLength(1, 100)] public string name { get; set; } = "";

    [Range(0, 150)] public int age { get; set; }

    [Regex(@"^[\w.-]+@[\w.-]+\.\w+$")] public string? email { get; set; }

    [Sensitivity(SensitivityLevel.Personal)]
    public string? phone { get; set; }

    [Since(2)] public string? address { get; set; }

    [Version] public int version { get; set; }
}

/// <summary>
///     配置项，用于测试 Tuple 模式和字段级属性
/// </summary>
[Data(mode = ProjectMode.Tuple, rename_all = RenameStyle.CamelCase)]
public class ConfigEntry
{
    [Field(order = 0)] public string key { get; set; } = "";

    [Field(order = 1, skip_when_null = true)]
    public string? value { get; set; }

    [Field(order = 2, default_value = false)]
    public bool is_active { get; set; }
}

/// <summary>
///     产品实体，用于测试多属性组合
/// </summary>
[Data]
public class Product
{
    [PrimaryKey] public int id { get; set; }

    [StringLength(1, 200)] public string name { get; set; } = "";

    [Column(name = "price")] public decimal price { get; set; }

    [Unique] [StringLength(1, 50)] public string sku { get; set; } = "";

    [IndexAttribute] public int category_id { get; set; }

    [Deprecated(message = "使用 category_id 替代")]
    public string? legacy_category { get; set; }

    [IgnoreStorage] public string? computed_field { get; set; }
}