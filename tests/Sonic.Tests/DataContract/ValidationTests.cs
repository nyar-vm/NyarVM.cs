using System.ComponentModel.DataAnnotations;

namespace Sonic.Testing.Data.DataContract;

/// <summary>
///     验证结果测试
/// </summary>
public class ValidationResultTests
{
    /// <summary>
    ///     测试无错误时 is_valid 返回 true
    /// </summary>
    [Fact]
    public void is_valid_ReturnsTrue_WhenNoErrors()
    {
        var result = new ValidationResult();
        Assert.True(result.is_valid);
        Assert.Empty(result.errors);
        Assert.Empty(result.warnings);
    }

    /// <summary>
    ///     测试有错误时 is_valid 返回 false
    /// </summary>
    [Fact]
    public void is_valid_ReturnsFalse_WhenHasErrors()
    {
        var result = new ValidationResult();
        result.add_error("name", "REQUIRED", "name 为必填项");
        Assert.False(result.is_valid);
        Assert.Single(result.errors);
    }

    /// <summary>
    ///     测试添加警告不影响 is_valid
    /// </summary>
    [Fact]
    public void add_warning_DoesNotAffectIsValid()
    {
        var result = new ValidationResult();
        result.add_warning("age", "DEPRECATED", "age 字段已弃用");
        Assert.True(result.is_valid);
        Assert.Single(result.warnings);
    }

    /// <summary>
    ///     测试添加多个错误
    /// </summary>
    [Fact]
    public void add_error_CollectsMultipleErrors()
    {
        var result = new ValidationResult();
        result.add_error("name", "REQUIRED", "name 为必填项");
        result.add_error("email", "REGEX", "email 格式不正确");
        Assert.Equal(2, result.errors.Count);
    }
}

/// <summary>
///     验证规则测试
/// </summary>
public class ValidationRuleTests
{
    /// <summary>
    ///     测试 ValidationRule 存储类型和参数
    /// </summary>
    [Fact]
    public void ValidationRule_StoresTypeAndParameters()
    {
        var parameters = new Dictionary<string, object>
        {
            ["min"] = 0.0,
            ["max"] = 100.0
        };
        var rule = new ValidationRule("range", parameters);

        Assert.Equal("range", rule.rule_type);
        Assert.Equal(2, rule.parameters.Count);
        Assert.Equal(0.0, rule.parameters["min"]);
        Assert.Equal(100.0, rule.parameters["max"]);
    }

    /// <summary>
    ///     测试 ValidationRule 默认空参数
    /// </summary>
    [Fact]
    public void ValidationRule_DefaultsEmptyParameters()
    {
        var rule = new ValidationRule("required");

        Assert.Equal("required", rule.rule_type);
        Assert.Empty(rule.parameters);
    }

    /// <summary>
    ///     测试 ValidationRule null 参数变为空
    /// </summary>
    [Fact]
    public void ValidationRule_NullParametersBecomesEmpty()
    {
        var rule = new ValidationRule("regex", null);

        Assert.Equal("regex", rule.rule_type);
        Assert.Empty(rule.parameters);
    }
}

/// <summary>
///     验证错误测试
/// </summary>
public class ValidationErrorTests
{
    /// <summary>
    ///     测试 ValidationError 存储所有字段
    /// </summary>
    [Fact]
    public void ValidationError_StoresAllFields()
    {
        var error = new ValidationError("name", "REQUIRED", "name 为必填项");

        Assert.Equal("name", error.member_path);
        Assert.Equal("REQUIRED", error.error_code);
        Assert.Equal("name 为必填项", error.message);
    }

    /// <summary>
    ///     测试多个验证错误
    /// </summary>
    [Fact]
    public void ValidationError_MultipleErrors()
    {
        var result = new ValidationResult();
        result.add_error("name", "REQUIRED", "name 为必填项");
        result.add_error("email", "REGEX", "email 格式不正确");
        result.add_error("age", "RANGE", "age 超出范围");

        Assert.Equal(3, result.errors.Count);
        Assert.Equal("name", result.errors[0].member_path);
        Assert.Equal("REQUIRED", result.errors[0].error_code);
        Assert.Equal("email", result.errors[1].member_path);
        Assert.Equal("REGEX", result.errors[1].error_code);
        Assert.Equal("age", result.errors[2].member_path);
        Assert.Equal("RANGE", result.errors[2].error_code);
    }
}

/// <summary>
///     验证警告测试
/// </summary>
public class ValidationWarningTests
{
    /// <summary>
    ///     测试 ValidationWarning 存储所有字段
    /// </summary>
    [Fact]
    public void ValidationWarning_StoresAllFields()
    {
        var warning = new ValidationWarning("legacy_field", "DEPRECATED", "该字段已弃用");

        Assert.Equal("legacy_field", warning.member_path);
        Assert.Equal("DEPRECATED", warning.code);
        Assert.Equal("该字段已弃用", warning.message);
    }

    /// <summary>
    ///     测试警告不影响 is_valid
    /// </summary>
    [Fact]
    public void ValidationResult_WarningsDoNotAffectIsValid()
    {
        var result = new ValidationResult();
        result.add_warning("old_field", "DEPRECATED", "该字段已弃用");
        result.add_warning("backup_field", "UNUSED", "该字段未使用");

        Assert.True(result.is_valid);
        Assert.Equal(2, result.warnings.Count);
    }

    /// <summary>
    ///     测试混合错误和警告
    /// </summary>
    [Fact]
    public void ValidationResult_MixedErrorsAndWarnings()
    {
        var result = new ValidationResult();
        result.add_error("name", "REQUIRED", "name 为必填项");
        result.add_warning("nickname", "DEPRECATED", "nickname 已弃用");

        Assert.False(result.is_valid);
        Assert.Single(result.errors);
        Assert.Single(result.warnings);
    }
}