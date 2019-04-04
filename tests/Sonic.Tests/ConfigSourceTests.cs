using Core.Config;
using Std.Config;

namespace Sonic.Testing.Config;

/// <summary>
///     ConfigSource 工厂和 ConfigValidationException 的单元测试。
/// </summary>
public class ConfigSourceTests
{
    #region ConfigSource 工厂方法测试

    /// <summary>
    ///     测试 ConfigSource.FromMemory 创建 MemoryConfigProvider 实例。
    /// </summary>
    [Fact]
    public void FromMemory_CreatesMemoryConfigProvider()
    {
        var data = new Dictionary<string, string>
        {
            ["key"] = "value"
        };

        var provider = ConfigSource.FromMemory(data);

        Assert.IsType<MemoryConfigProvider>(provider);
        var root = provider.Load();
        Assert.Equal("value", root.get_field("key")?.AsString());
    }

    /// <summary>
    ///     测试 ConfigSource.FromCli 创建 CliConfigProvider 实例。
    /// </summary>
    [Fact]
    public void FromCli_CreatesCliConfigProvider()
    {
        var args = new[] { "--key=value" };

        var provider = ConfigSource.FromCli(args);

        Assert.IsType<CliConfigProvider>(provider);
        var root = provider.Load();
        Assert.Equal("value", root.get_field("key")?.AsString());
    }

    /// <summary>
    ///     测试 ConfigSource.FromEnvironment 创建 EnvironmentConfigProvider 实例。
    /// </summary>
    [Fact]
    public void FromEnvironment_CreatesEnvironmentConfigProvider()
    {
        var provider = ConfigSource.FromEnvironment("CUSTOM_");

        Assert.IsType<EnvironmentConfigProvider>(provider);
    }

    /// <summary>
    ///     测试 ConfigSource.FromEnvironment 使用默认前缀。
    /// </summary>
    [Fact]
    public void FromEnvironment_DefaultPrefix_IsAppUnderscore()
    {
        var provider = ConfigSource.FromEnvironment();

        Assert.IsType<EnvironmentConfigProvider>(provider);
    }

    /// <summary>
    ///     测试 ConfigSource.FromJson 创建 JsonConfigProvider 实例。
    /// </summary>
    [Fact]
    public void FromJson_CreatesJsonConfigProvider()
    {
        var provider = ConfigSource.FromJson("config.json");

        Assert.IsType<JsonConfigProvider>(provider);
    }

    /// <summary>
    ///     测试 ConfigSource.Select 创建 FallbackConfigProvider 实例。
    /// </summary>
    [Fact]
    public void Select_CreatesFallbackConfigProvider()
    {
        var p1 = ConfigSource.FromMemory(new Dictionary<string, string>());
        var p2 = ConfigSource.FromMemory(new Dictionary<string, string>());

        var provider = ConfigSource.Select(p1, p2);

        Assert.IsType<FallbackConfigProvider>(provider);
    }

    #endregion

    #region ConfigValidationException 测试

    /// <summary>
    ///     测试 ConfigValidationException 构造函数包含所有错误信息。
    /// </summary>
    [Fact]
    public void ConfigValidationException_ContainsAllErrors()
    {
        var errors = new List<string>
        {
            "属性 Name 是必填项",
            "属性 Port 的值不在范围内"
        };

        var exception = new ConfigValidationException(errors);

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains("属性 Name 是必填项", exception.Errors);
        Assert.Contains("属性 Port 的值不在范围内", exception.Errors);
    }

    /// <summary>
    ///     测试 ConfigValidationException 的 Message 属性包含所有错误。
    /// </summary>
    [Fact]
    public void ConfigValidationException_MessageContainsAllErrors()
    {
        var errors = new List<string>
        {
            "错误 A",
            "错误 B"
        };

        var exception = new ConfigValidationException(errors);

        Assert.Contains("错误 A", exception.Message);
        Assert.Contains("错误 B", exception.Message);
        Assert.Contains("配置验证失败", exception.Message);
    }

    /// <summary>
    ///     测试 ConfigValidationException 对空错误列表的 Message。
    /// </summary>
    [Fact]
    public void ConfigValidationException_EmptyErrors_MessageStillValid()
    {
        var exception = new ConfigValidationException([]);

        Assert.Empty(exception.Errors);
        Assert.Contains("配置验证失败", exception.Message);
    }

    #endregion
}