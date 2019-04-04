using System.ComponentModel.DataAnnotations.Schema;
using Core.Data;
using Std.DataProcess;
using FieldAttribute = Std.Binary.Attributes.FieldAttribute;

namespace Sonic.Testing.Data.Data;

/// <summary>
///     [Data] 标记类型的属性与构造测试，验证 Point.cs 中定义的类型
///     具有正确的特性标注和属性读写能力
/// </summary>
public class GeneratedDataTests
{
    /// <summary>
    ///     测试 Point2D 可构造且属性可读写
    /// </summary>
    [Fact]
    public void Point2D_CanConstructAndPropertiesWork()
    {
        var point = new Point2D { x = 1.5, y = 2.5 };
        Assert.Equal(1.5, point.x);
        Assert.Equal(2.5, point.y);
    }

    /// <summary>
    ///     测试 Point2D 标记了 Data 特性
    /// </summary>
    [Fact]
    public void Point2D_HasDataAttribute()
    {
        var attr = (DataAttribute?)Attribute.GetCustomAttribute(
            typeof(Point2D), typeof(DataAttribute));
        Assert.NotNull(attr);
        Assert.Equal(ProjectMode.map, attr.mode);
        Assert.Equal(1u, attr.version);
    }

    /// <summary>
    ///     测试 Point3D 可构造且属性可读写
    /// </summary>
    [Fact]
    public void Point3D_CanConstructAndPropertiesWork()
    {
        var point = new Point3D { x = 1.0, y = 2.0, z = 3.0 };
        Assert.Equal(1.0, point.x);
        Assert.Equal(2.0, point.y);
        Assert.Equal(3.0, point.z);
    }

    /// <summary>
    ///     测试 Point3D 标记了 Data 特性
    /// </summary>
    [Fact]
    public void Point3D_HasDataAttribute()
    {
        var attr = (DataAttribute?)Attribute.GetCustomAttribute(
            typeof(Point3D), typeof(DataAttribute));
        Assert.NotNull(attr);
        Assert.Equal(ProjectMode.map, attr.mode);
    }

    /// <summary>
    ///     测试 User 可构造且属性可读写
    /// </summary>
    [Fact]
    public void User_CanConstructAndPropertiesWork()
    {
        var user = new User
        {
            id = 1,
            name = "张三",
            age = 25,
            email = "test@example.com",
            phone = "13800138000",
            address = "北京市",
            version = 1
        };
        Assert.Equal(1, user.id);
        Assert.Equal("张三", user.name);
        Assert.Equal(25, user.age);
        Assert.Equal("test@example.com", user.email);
        Assert.Equal("13800138000", user.phone);
        Assert.Equal("北京市", user.address);
        Assert.Equal(1, user.version);
    }

    /// <summary>
    ///     测试 User 的 Data 特性包含正确版本和描述
    /// </summary>
    [Fact]
    public void User_DataAttribute_HasCorrectVersionAndDescription()
    {
        var attr = (DataAttribute?)Attribute.GetCustomAttribute(
            typeof(User), typeof(DataAttribute));
        Assert.NotNull(attr);
        Assert.Equal(2u, attr.version);
        Assert.Equal("用户实体", attr.description);
    }

    /// <summary>
    ///     测试 User 的 name 属性具有 StringLength 验证特性
    /// </summary>
    [Fact]
    public void User_NameHasStringLengthAttribute()
    {
        var prop = typeof(User).GetProperty("name");
        Assert.NotNull(prop);
        var attr = (StringLengthAttribute?)Attribute.GetCustomAttribute(prop, typeof(StringLengthAttribute));
        Assert.NotNull(attr);
        Assert.Equal(1, attr.min);
        Assert.Equal(100, attr.max);
    }

    /// <summary>
    ///     测试 User 的 age 属性具有 Range 验证特性
    /// </summary>
    [Fact]
    public void User_AgeHasRangeAttribute()
    {
        var prop = typeof(User).GetProperty("age");
        Assert.NotNull(prop);
        var attr = (RangeAttribute?)Attribute.GetCustomAttribute(prop, typeof(RangeAttribute));
        Assert.NotNull(attr);
        Assert.Equal(0.0, attr.min);
        Assert.Equal(150.0, attr.max);
    }

    /// <summary>
    ///     测试 User 的 email 属性具有 Regex 验证特性
    /// </summary>
    [Fact]
    public void User_EmailHasRegexAttribute()
    {
        var prop = typeof(User).GetProperty("email");
        Assert.NotNull(prop);
        var attr = (RegexAttribute?)Attribute.GetCustomAttribute(prop, typeof(RegexAttribute));
        Assert.NotNull(attr);
        Assert.Equal(@"^[\w.-]+@[\w.-]+\.\w+$", attr.pattern);
    }

    /// <summary>
    ///     测试 User 的 phone 属性具有 Sensitivity 特性
    /// </summary>
    [Fact]
    public void User_PhoneHasSensitivityAttribute()
    {
        var prop = typeof(User).GetProperty("phone");
        Assert.NotNull(prop);
        var attr = (SensitivityAttribute?)Attribute.GetCustomAttribute(prop, typeof(SensitivityAttribute));
        Assert.NotNull(attr);
        Assert.Equal(SensitivityLevel.personal, attr.level);
    }

    /// <summary>
    ///     测试 User 的 address 属性具有 Since 特性
    /// </summary>
    [Fact]
    public void User_AddressHasSinceAttribute()
    {
        var prop = typeof(User).GetProperty("address");
        Assert.NotNull(prop);
        var attr = (SinceAttribute?)Attribute.GetCustomAttribute(prop, typeof(SinceAttribute));
        Assert.NotNull(attr);
        Assert.Equal(2u, attr.version);
    }

    /// <summary>
    ///     测试 User 的 id 属性具有 PrimaryKey 特性
    /// </summary>
    [Fact]
    public void User_IdHasPrimaryKeyAttribute()
    {
        var prop = typeof(User).GetProperty("id");
        Assert.NotNull(prop);
        var attr = (PrimaryKeyAttribute?)Attribute.GetCustomAttribute(prop, typeof(PrimaryKeyAttribute));
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 ConfigEntry 可构造且属性可读写
    /// </summary>
    [Fact]
    public void ConfigEntry_CanConstructAndPropertiesWork()
    {
        var entry = new ConfigEntry
        {
            key = "host",
            value = "localhost",
            is_active = true
        };
        Assert.Equal("host", entry.key);
        Assert.Equal("localhost", entry.value);
        Assert.True(entry.is_active);
    }

    /// <summary>
    ///     测试 ConfigEntry 使用 Tuple 模式和 CamelCase 重命名
    /// </summary>
    [Fact]
    public void ConfigEntry_UsesTupleMode()
    {
        var attr = (DataAttribute?)Attribute.GetCustomAttribute(
            typeof(ConfigEntry), typeof(DataAttribute));
        Assert.NotNull(attr);
        Assert.Equal(ProjectMode.tuple, attr.mode);
        Assert.Equal(RenameStyle.camel_case, attr.rename_all);
    }

    /// <summary>
    ///     测试 ConfigEntry 字段包含 Field 特性
    /// </summary>
    [Fact]
    public void ConfigEntry_FieldsHaveFieldAttribute()
    {
        var keyProp = typeof(ConfigEntry).GetProperty("key");
        Assert.NotNull(keyProp);
        var fieldAttr = (FieldAttribute?)Attribute.GetCustomAttribute(keyProp, typeof(FieldAttribute));
        Assert.NotNull(fieldAttr);
        Assert.Equal(0, fieldAttr.order);

        var valueProp = typeof(ConfigEntry).GetProperty("value");
        Assert.NotNull(valueProp);
        var valueFieldAttr = (FieldAttribute?)Attribute.GetCustomAttribute(valueProp, typeof(FieldAttribute));
        Assert.NotNull(valueFieldAttr);
        Assert.Equal(1, valueFieldAttr.order);
        Assert.True(valueFieldAttr.skip_when_null);

        var isActiveProp = typeof(ConfigEntry).GetProperty("is_active");
        Assert.NotNull(isActiveProp);
        var activeFieldAttr = (FieldAttribute?)Attribute.GetCustomAttribute(isActiveProp, typeof(FieldAttribute));
        Assert.NotNull(activeFieldAttr);
        Assert.Equal(2, activeFieldAttr.order);
        Assert.Equal(false, activeFieldAttr.default_value);
    }

    /// <summary>
    ///     测试 Product 可构造且属性可读写
    /// </summary>
    [Fact]
    public void Product_CanConstructAndPropertiesWork()
    {
        var product = new Product
        {
            id = 1,
            name = "商品A",
            price = 99.99m,
            sku = "SKU001",
            category_id = 10,
            legacy_category = "旧分类",
            computed_field = "计算字段"
        };
        Assert.Equal(1, product.id);
        Assert.Equal("商品A", product.name);
        Assert.Equal(99.99m, product.price);
        Assert.Equal("SKU001", product.sku);
        Assert.Equal(10, product.category_id);
        Assert.Equal("旧分类", product.legacy_category);
        Assert.Equal("计算字段", product.computed_field);
    }

    /// <summary>
    ///     测试 Product 的 price 属性具有 Column 特性
    /// </summary>
    [Fact]
    public void Product_PriceHasColumnAttribute()
    {
        var priceProp = typeof(Product).GetProperty("price");
        Assert.NotNull(priceProp);
        var columnAttr = (ColumnAttribute?)Attribute.GetCustomAttribute(priceProp, typeof(ColumnAttribute));
        Assert.NotNull(columnAttr);
        Assert.Equal("price", columnAttr.name);
    }

    /// <summary>
    ///     测试 Product 的 sku 属性具有 Unique 特性
    /// </summary>
    [Fact]
    public void Product_SkuHasUniqueAttribute()
    {
        var skuProp = typeof(Product).GetProperty("sku");
        Assert.NotNull(skuProp);
        var uniqueAttr = (UniqueAttribute?)Attribute.GetCustomAttribute(skuProp, typeof(UniqueAttribute));
        Assert.NotNull(uniqueAttr);
    }

    /// <summary>
    ///     测试 Product 的 sku 属性同时具有 StringLength 特性
    /// </summary>
    [Fact]
    public void Product_SkuHasStringLengthAttribute()
    {
        var skuProp = typeof(Product).GetProperty("sku");
        Assert.NotNull(skuProp);
        var attr = (StringLengthAttribute?)Attribute.GetCustomAttribute(skuProp, typeof(StringLengthAttribute));
        Assert.NotNull(attr);
        Assert.Equal(1, attr.min);
        Assert.Equal(50, attr.max);
    }

    /// <summary>
    ///     测试 Product 的 category_id 属性具有 IndexAttribute 特性
    /// </summary>
    [Fact]
    public void Product_CategoryIdHasIndexAttribute()
    {
        var categoryProp = typeof(Product).GetProperty("category_id");
        Assert.NotNull(categoryProp);
        var indexAttr = (IndexAttribute?)Attribute.GetCustomAttribute(categoryProp, typeof(IndexAttribute));
        Assert.NotNull(indexAttr);
    }

    /// <summary>
    ///     测试 Product 的 legacy_category 属性具有 Deprecated 特性
    /// </summary>
    [Fact]
    public void Product_LegacyCategoryHasDeprecatedAttribute()
    {
        var legacyProp = typeof(Product).GetProperty("legacy_category");
        Assert.NotNull(legacyProp);
        var deprecatedAttr =
            (DeprecatedAttribute?)Attribute.GetCustomAttribute(legacyProp, typeof(DeprecatedAttribute));
        Assert.NotNull(deprecatedAttr);
        Assert.Equal("使用 category_id 替代", deprecatedAttr.message);
    }

    /// <summary>
    ///     测试 Product 的 computed_field 属性具有 IgnoreStorage 特性
    /// </summary>
    [Fact]
    public void Product_ComputedFieldHasIgnoreStorageAttribute()
    {
        var computedProp = typeof(Product).GetProperty("computed_field");
        Assert.NotNull(computedProp);
        var ignoreAttr =
            (IgnoreStorageAttribute?)Attribute.GetCustomAttribute(computedProp, typeof(IgnoreStorageAttribute));
        Assert.NotNull(ignoreAttr);
    }

    /// <summary>
    ///     测试 Product 的 id 属性具有 PrimaryKey 特性
    /// </summary>
    [Fact]
    public void Product_IdHasPrimaryKeyAttribute()
    {
        var idProp = typeof(Product).GetProperty("id");
        Assert.NotNull(idProp);
        var pkAttr = (PrimaryKeyAttribute?)Attribute.GetCustomAttribute(idProp, typeof(PrimaryKeyAttribute));
        Assert.NotNull(pkAttr);
    }

    /// <summary>
    ///     测试 Product 的 name 属性具有 StringLength 特性
    /// </summary>
    [Fact]
    public void Product_NameHasStringLengthAttribute()
    {
        var nameProp = typeof(Product).GetProperty("name");
        Assert.NotNull(nameProp);
        var attr = (StringLengthAttribute?)Attribute.GetCustomAttribute(nameProp, typeof(StringLengthAttribute));
        Assert.NotNull(attr);
        Assert.Equal(1, attr.min);
        Assert.Equal(200, attr.max);
    }
}