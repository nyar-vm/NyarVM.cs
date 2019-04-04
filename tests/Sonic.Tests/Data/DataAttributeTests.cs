using Std.Binary.Attributes;
using Std.DataProcess;
using Std.DataStorage;

namespace Sonic.Testing.Data.Data;

/// <summary>
///     Data 属性测试
/// </summary>
public class DataAttributeTests
{
    /// <summary>
    ///     测试 DataAttribute 默认值
    /// </summary>
    [Fact]
    public void DataAttribute_Defaults()
    {
        var attr = new DataAttribute();
        Assert.Equal(ProjectMode.Map, attr.mode);
        Assert.Equal(RenameStyle.None, attr.rename_all);
        Assert.Equal(1u, attr.version);
        Assert.Null(attr.description);
        Assert.True(attr.generate_meta);
        Assert.Null(attr.storage);
    }

    /// <summary>
    ///     测试 DataAttribute 自定义值
    /// </summary>
    [Fact]
    public void DataAttribute_WithCustomValues()
    {
        var attr = new DataAttribute
        {
            mode = ProjectMode.Tuple,
            rename_all = RenameStyle.CamelCase,
            version = 3,
            description = "测试类型",
            generate_meta = false
        };
        Assert.Equal(ProjectMode.Tuple, attr.mode);
        Assert.Equal(RenameStyle.CamelCase, attr.rename_all);
        Assert.Equal(3u, attr.version);
        Assert.Equal("测试类型", attr.description);
        Assert.False(attr.generate_meta);
    }

    /// <summary>
    ///     测试 FieldAttribute 默认值
    /// </summary>
    [Fact]
    public void FieldAttribute_Defaults()
    {
        var attr = new FieldAttribute();
        Assert.Null(attr.name);
        Assert.Equal(-1, attr.order);
        Assert.Null(attr.default_value);
        Assert.False(attr.required);
        Assert.False(attr.skip_when_null);
        Assert.False(attr.skip_when_default);
    }

    /// <summary>
    ///     测试 ProjectMode 枚举值
    /// </summary>
    [Fact]
    public void ProjectMode_Values()
    {
        Assert.Equal(0, (int)ProjectMode.Map);
        Assert.Equal(1, (int)ProjectMode.Tuple);
    }

    /// <summary>
    ///     测试 RenameStyle 枚举值
    /// </summary>
    [Fact]
    public void RenameStyle_Values()
    {
        Assert.Equal(0, (int)RenameStyle.None);
        Assert.Equal(1, (int)RenameStyle.CamelCase);
        Assert.Equal(2, (int)RenameStyle.SnakeCase);
        Assert.Equal(3, (int)RenameStyle.KebabCase);
        Assert.Equal(4, (int)RenameStyle.UpperCamelCase);
        Assert.Equal(5, (int)RenameStyle.LowerCase);
        Assert.Equal(6, (int)RenameStyle.UpperCase);
    }

    /// <summary>
    ///     测试 StorageConfig 存储后端
    /// </summary>
    [Fact]
    public void StorageConfig_StoresBackend()
    {
        var config = new StorageConfig("relational", "users");
        Assert.Equal("relational", config.backend);
        Assert.Equal("users", config.table_name);
    }
}

/// <summary>
///     Data 属性扩展测试
/// </summary>
public class DataAttributeExtendedTests
{
    /// <summary>
    ///     测试 StorageConfig 无表名
    /// </summary>
    [Fact]
    public void StorageConfig_WithoutTableName()
    {
        var config = new StorageConfig("kv");
        Assert.Equal("kv", config.backend);
        Assert.Null(config.table_name);
    }

    /// <summary>
    ///     测试 StorageConfig 带表名
    /// </summary>
    [Fact]
    public void StorageConfig_WithTableName()
    {
        var config = new StorageConfig("relational", "users");
        Assert.Equal("relational", config.backend);
        Assert.Equal("users", config.table_name);
    }

    /// <summary>
    ///     测试 FieldAttribute 自定义值
    /// </summary>
    [Fact]
    public void FieldAttribute_WithCustomValues()
    {
        var attr = new FieldAttribute
        {
            name = "user_name",
            order = 3,
            default_value = "unknown",
            required = true,
            skip_when_null = true,
            skip_when_default = true
        };
        Assert.Equal("user_name", attr.name);
        Assert.Equal(3, attr.order);
        Assert.Equal("unknown", attr.default_value);
        Assert.True(attr.required);
        Assert.True(attr.skip_when_null);
        Assert.True(attr.skip_when_default);
    }

    /// <summary>
    ///     测试 DataAttribute 带存储配置
    /// </summary>
    [Fact]
    public void DataAttribute_WithStorage()
    {
        var attr = new DataAttribute
        {
            storage = new StorageConfig("file")
        };
        Assert.NotNull(attr.storage);
        Assert.Equal("file", attr.storage.backend);
    }
}