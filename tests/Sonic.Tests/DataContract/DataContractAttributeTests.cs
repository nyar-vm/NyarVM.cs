using System.ComponentModel.DataAnnotations;

namespace Sonic.Testing.Data.DataContract;

/// <summary>
///     DataContract 属性测试
/// </summary>
public class DataContractAttributeTests
{
    /// <summary>
    ///     测试 StringLengthAttribute 存储最小最大值
    /// </summary>
    [Fact]
    public void StringLengthAttribute_StoresMinMax()
    {
        var attr = new StringLengthAttribute(1, 100);
        Assert.Equal(1, attr.min);
        Assert.Equal(100, attr.max);
    }

    /// <summary>
    ///     测试 RangeAttribute 存储最小最大值
    /// </summary>
    [Fact]
    public void RangeAttribute_StoresMinMax()
    {
        var attr = new RangeAttribute(0.0, 100.0);
        Assert.Equal(0.0, attr.min);
        Assert.Equal(100.0, attr.max);
    }

    /// <summary>
    ///     测试 RegexAttribute 存储正则模式
    /// </summary>
    [Fact]
    public void RegexAttribute_StoresPattern()
    {
        var attr = new RegexAttribute(@"^[\w.-]+@[\w.-]+\.\w+$");
        Assert.Equal(@"^[\w.-]+@[\w.-]+\.\w+$", attr.pattern);
    }

    /// <summary>
    ///     测试 RequiredWhenAttribute 存储依赖字段
    /// </summary>
    [Fact]
    public void RequiredWhenAttribute_StoresDependentField()
    {
        var attr = new RequiredWhenAttribute("country", "US");
        Assert.Equal("country", attr.dependent_field);
        Assert.Equal("US", attr.expected_value);
    }

    /// <summary>
    ///     测试 SensitivityAttribute 存储敏感级别
    /// </summary>
    [Fact]
    public void SensitivityAttribute_StoresLevel()
    {
        var attr = new SensitivityAttribute(SensitivityLevel.Personal);
        Assert.Equal(SensitivityLevel.Personal, attr.level);
    }

    /// <summary>
    ///     测试 SinceAttribute 存储版本号
    /// </summary>
    [Fact]
    public void SinceAttribute_StoresVersion()
    {
        var attr = new SinceAttribute(2);
        Assert.Equal(2u, attr.version);
    }

    /// <summary>
    ///     测试 DeprecatedAttribute 默认值为 null
    /// </summary>
    [Fact]
    public void DeprecatedAttribute_DefaultsToNull()
    {
        var attr = new DeprecatedAttribute();
        Assert.Null(attr.since_version);
        Assert.Null(attr.message);
    }

    /// <summary>
    ///     测试 SensitivityLevel 枚举值
    /// </summary>
    [Fact]
    public void SensitivityLevel_Values()
    {
        Assert.Equal(0, (int)SensitivityLevel.Normal);
        Assert.Equal(1, (int)SensitivityLevel.Personal);
        Assert.Equal(2, (int)SensitivityLevel.Sensitive);
        Assert.Equal(3, (int)SensitivityLevel.Secret);
    }
}

/// <summary>
///     DataContract 扩展属性测试
/// </summary>
public class DataContractExtendedAttributeTests
{
    /// <summary>
    ///     测试 EnumCheckAttribute 默认错误消息为 null
    /// </summary>
    [Fact]
    public void EnumCheckAttribute_DefaultErrorMessageIsNull()
    {
        var attr = new EnumCheckAttribute();
        Assert.Null(attr.error_message);
    }

    /// <summary>
    ///     测试 EnumCheckAttribute 存储错误消息
    /// </summary>
    [Fact]
    public void EnumCheckAttribute_StoresErrorMessage()
    {
        var attr = new EnumCheckAttribute { error_message = "枚举值无效" };
        Assert.Equal("枚举值无效", attr.error_message);
    }

    /// <summary>
    ///     测试 CustomValidationAttribute 存储错误码和消息
    /// </summary>
    [Fact]
    public void CustomValidationAttribute_StoresErrorCodeAndMessage()
    {
        var attr = new CustomValidationAttribute("CUSTOM_001", "自定义验证失败");
        Assert.Equal("CUSTOM_001", attr.error_code);
        Assert.Equal("自定义验证失败", attr.message);
    }

    /// <summary>
    ///     测试 VersionAttribute 可正常创建
    /// </summary>
    [Fact]
    public void VersionAttribute_CreatesSuccessfully()
    {
        var attr = new VersionAttribute();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     测试 DeprecatedAttribute 存储版本和消息
    /// </summary>
    [Fact]
    public void DeprecatedAttribute_WithVersionAndMessage()
    {
        var attr = new DeprecatedAttribute { since_version = 3, message = "请使用 new_field" };
        Assert.Equal(3u, attr.since_version);
        Assert.Equal("请使用 new_field", attr.message);
    }

    /// <summary>
    ///     测试 SinceAttribute 存储版本号
    /// </summary>
    [Fact]
    public void SinceAttribute_StoresVersion()
    {
        var attr = new SinceAttribute(5);
        Assert.Equal(5u, attr.version);
    }

    /// <summary>
    ///     测试 StringLengthAttribute 存储最小最大值
    /// </summary>
    [Fact]
    public void StringLengthAttribute_StoresMinMax()
    {
        var attr = new StringLengthAttribute(5, 200);
        Assert.Equal(5, attr.min);
        Assert.Equal(200, attr.max);
    }

    /// <summary>
    ///     测试 RangeAttribute 存储最小最大值
    /// </summary>
    [Fact]
    public void RangeAttribute_StoresMinMax()
    {
        var attr = new RangeAttribute(-10.0, 10.0);
        Assert.Equal(-10.0, attr.min);
        Assert.Equal(10.0, attr.max);
    }

    /// <summary>
    ///     测试 RegexAttribute 存储正则模式
    /// </summary>
    [Fact]
    public void RegexAttribute_StoresPattern()
    {
        var attr = new RegexAttribute(@"^\d+$");
        Assert.Equal(@"^\d+$", attr.pattern);
    }

    /// <summary>
    ///     测试 RequiredWhenAttribute 存储依赖字段和值
    /// </summary>
    [Fact]
    public void RequiredWhenAttribute_StoresDependentFieldAndValue()
    {
        var attr = new RequiredWhenAttribute("status", "active");
        Assert.Equal("status", attr.dependent_field);
        Assert.Equal("active", attr.expected_value);
    }

    /// <summary>
    ///     测试 SensitivityAttribute 存储敏感级别
    /// </summary>
    [Fact]
    public void SensitivityAttribute_StoresLevel()
    {
        var attr = new SensitivityAttribute(SensitivityLevel.Secret);
        Assert.Equal(SensitivityLevel.Secret, attr.level);
    }

    /// <summary>
    ///     测试 SensitivityLevel 所有枚举值
    /// </summary>
    [Fact]
    public void SensitivityLevel_AllValues()
    {
        Assert.Equal(0, (int)SensitivityLevel.Normal);
        Assert.Equal(1, (int)SensitivityLevel.Personal);
        Assert.Equal(2, (int)SensitivityLevel.Sensitive);
        Assert.Equal(3, (int)SensitivityLevel.Secret);
    }
}