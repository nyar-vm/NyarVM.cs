namespace Sonic.Testing.Data.DataContract;

/// <summary>
///     TypeMeta 和 FieldMeta 测试
/// </summary>
public class TypeMetaAndFieldMetaTests
{
    /// <summary>
    ///     测试 TypeMeta 存储值
    /// </summary>
    [Fact]
    public void TypeMeta_StoresValues()
    {
        var meta = new TypeMeta("Person", "用户实体", 2);
        Assert.Equal("Person", meta.name);
        Assert.Equal("用户实体", meta.description);
        Assert.Equal(2u, meta.version);
    }

    /// <summary>
    ///     测试 TypeMeta 允许 null 名称和描述
    /// </summary>
    [Fact]
    public void TypeMeta_AllowsNullNameAndDescription()
    {
        var meta = new TypeMeta(null, null, 1);
        Assert.Null(meta.name);
        Assert.Null(meta.description);
        Assert.Equal(1u, meta.version);
    }

    /// <summary>
    ///     测试 ColumnMeta 存储值
    /// </summary>
    [Fact]
    public void ColumnMeta_StoresValues()
    {
        var meta = new ColumnMeta(
            "user_name", "user_name", StorageType.String,
            false, true, false, null);
        Assert.Equal("user_name", meta.member_name);
        Assert.Equal("user_name", meta.column_name);
        Assert.Equal(StorageType.String, meta.column_type);
        Assert.False(meta.is_nullable);
        Assert.True(meta.is_unique);
        Assert.False(meta.is_indexed);
        Assert.Null(meta.default_value);
    }

    /// <summary>
    ///     测试 ColumnMeta 所有字段
    /// </summary>
    [Fact]
    public void ColumnMeta_WithAllFields()
    {
        var meta = new ColumnMeta(
            "age", "user_age", StorageType.Int32,
            true, false, true, 0);
        Assert.Equal("age", meta.member_name);
        Assert.Equal("user_age", meta.column_name);
        Assert.Equal(StorageType.Int32, meta.column_type);
        Assert.True(meta.is_nullable);
        Assert.False(meta.is_unique);
        Assert.True(meta.is_indexed);
        Assert.Equal(0, meta.default_value);
    }

    /// <summary>
    ///     测试 FieldMeta 存储值
    /// </summary>
    [Fact]
    public void FieldMeta_StoresValues()
    {
        var rules = new List<ValidationRule>
        {
            new ValidationRule("min_length", new Dictionary<string, object> { ["value"] = 1 }),
            new ValidationRule("max_length", new Dictionary<string, object> { ["value"] = 100 })
        };
        var meta = new FieldMeta("name", typeof(string), "alias_name", true, null, rules, SensitivityLevel.Personal);

        Assert.Equal("name", meta.member_name);
        Assert.Equal(typeof(string), meta.field_type);
        Assert.Equal("alias_name", meta.alias);
        Assert.True(meta.is_required);
        Assert.Null(meta.default_value);
        Assert.Equal(2, meta.rules.Count);
        Assert.Equal(SensitivityLevel.Personal, meta.sensitivity);
    }

    /// <summary>
    ///     测试 FieldMeta 可选字段为 null
    /// </summary>
    [Fact]
    public void FieldMeta_WithNullOptionalFields()
    {
        var meta = new FieldMeta("age", typeof(int), null, false, 0, [], null);

        Assert.Equal("age", meta.member_name);
        Assert.Null(meta.alias);
        Assert.False(meta.is_required);
        Assert.Equal(0, meta.default_value);
        Assert.Empty(meta.rules);
        Assert.Null(meta.sensitivity);
    }
}