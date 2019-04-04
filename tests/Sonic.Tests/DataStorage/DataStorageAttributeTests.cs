using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sonic.Testing.Data.DataStorage;

/// <summary>
///     DataStorage 属性测试
/// </summary>
public class DataStorageAttributeTests
{
    /// <summary>
    ///     测试 TableAttribute 存储名称
    /// </summary>
    [Fact]
    public void TableAttribute_StoresName()
    {
        var attr = new TableAttribute("users");
        Assert.Equal("users", attr.name);
    }

    /// <summary>
    ///     测试 PrimaryKeyAttribute 可正常创建
    /// </summary>
    [Fact]
    public void PrimaryKeyAttribute_CreatesSuccessfully()
    {
        var attr = new PrimaryKeyAttribute();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 UniqueAttribute 默认分组为 null
    /// </summary>
    [Fact]
    public void UniqueAttribute_DefaultGroupIsNull()
    {
        var attr = new UniqueAttribute();
        Assert.Null(attr.group);
    }

    /// <summary>
    ///     测试 IndexAttribute 默认值
    /// </summary>
    [Fact]
    public void IndexAttribute_Defaults()
    {
        var attr = new IndexAttribute();
        Assert.False(attr.unique);
        Assert.Null(attr.name);
    }

    /// <summary>
    ///     测试 ColumnAttribute 默认值
    /// </summary>
    [Fact]
    public void ColumnAttribute_Defaults()
    {
        var attr = new ColumnAttribute();
        Assert.Null(attr.name);
        Assert.Null(attr.storage_type);
    }

    /// <summary>
    ///     测试 StorageType 枚举值
    /// </summary>
    [Fact]
    public void StorageType_Values()
    {
        Assert.Equal(0, (int)StorageType.Int32);
        Assert.Equal(1, (int)StorageType.Int64);
        Assert.Equal(2, (int)StorageType.String);
        Assert.Equal(3, (int)StorageType.Float64);
        Assert.Equal(4, (int)StorageType.Bool);
        Assert.Equal(5, (int)StorageType.Blob);
        Assert.Equal(6, (int)StorageType.DateTime);
        Assert.Equal(7, (int)StorageType.UInt64);
        Assert.Equal(8, (int)StorageType.Float32);
    }
}

/// <summary>
///     DataStorage 扩展属性测试
/// </summary>
public class DataStorageExtendedTests
{
    /// <summary>
    ///     测试 ConcurrencyCheckAttribute 可正常创建
    /// </summary>
    [Fact]
    public void ConcurrencyCheckAttribute_CreatesSuccessfully()
    {
        var attr = new ConcurrencyCheckAttribute();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 IgnoreStorageAttribute 可正常创建
    /// </summary>
    [Fact]
    public void IgnoreStorageAttribute_CreatesSuccessfully()
    {
        var attr = new IgnoreStorageAttribute();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 TableAttribute 存储名称
    /// </summary>
    [Fact]
    public void TableAttribute_StoresName()
    {
        var attr = new TableAttribute("orders");
        Assert.Equal("orders", attr.name);
    }

    /// <summary>
    ///     测试 PrimaryKeyAttribute 可正常创建
    /// </summary>
    [Fact]
    public void PrimaryKeyAttribute_CreatesSuccessfully()
    {
        var attr = new PrimaryKeyAttribute();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 UniqueAttribute 带分组
    /// </summary>
    [Fact]
    public void UniqueAttribute_WithGroup()
    {
        var attr = new UniqueAttribute("uk_user_email");
        Assert.Equal("uk_user_email", attr.group);
    }

    /// <summary>
    ///     测试 IndexAttribute 带唯一和名称
    /// </summary>
    [Fact]
    public void IndexAttribute_WithUniqueAndName()
    {
        var attr = new IndexAttribute { unique = true, name = "idx_category" };
        Assert.True(attr.unique);
        Assert.Equal("idx_category", attr.name);
    }

    /// <summary>
    ///     测试 ColumnAttribute 带名称和存储类型
    /// </summary>
    [Fact]
    public void ColumnAttribute_WithNameAndStorageType()
    {
        var attr = new ColumnAttribute { name = "user_name", storage_type = StorageType.String };
        Assert.Equal("user_name", attr.name);
        Assert.Equal(StorageType.String, attr.storage_type);
    }

    /// <summary>
    ///     测试 StorageType 所有枚举值
    /// </summary>
    [Fact]
    public void StorageType_AllValues()
    {
        Assert.Equal(0, (int)StorageType.Int32);
        Assert.Equal(1, (int)StorageType.Int64);
        Assert.Equal(2, (int)StorageType.String);
        Assert.Equal(3, (int)StorageType.Float64);
        Assert.Equal(4, (int)StorageType.Bool);
        Assert.Equal(5, (int)StorageType.Blob);
        Assert.Equal(6, (int)StorageType.DateTime);
        Assert.Equal(7, (int)StorageType.UInt64);
        Assert.Equal(8, (int)StorageType.Float32);
    }
}