using Core.Config;

namespace Sonic.Testing.Config;

/// <summary>
///     使用 [Config] 标记类测试源代码生成器的集成测试。
/// </summary>
public class IntegrationTests
{
    #region IConfigurable.ResetToDefaults 测试

    /// <summary>
    ///     测试 IConfigurable.ResetToDefaults 将所有字段重置为默认值。
    /// </summary>
    [Fact]
    public void ResetToDefaults_RestoresDefaultValues()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "custom.com",
            ["port"] = "9999",
            ["debug"] = "true"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = BasicConfig.From(provider).Build();
        Assert.Equal("custom.com", config.Host);

        config.ResetToDefaults();

        Assert.Equal("localhost", config.Host);
        Assert.Equal(8080, config.Port);
        Assert.False(config.Debug);
    }

    #endregion

    #region [Append] 合并策略测试

    /// <summary>
    ///     测试 [Append] 合并策略将新元素追加到列表末尾。
    /// </summary>
    [Fact]
    public void Append_MergeStrategy_AppendsToList()
    {
        var primary = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["servers:0"] = "server-a",
            ["servers:1"] = "server-b"
        });
        var secondary = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["servers:0"] = "server-c"
        });

        var config = AppendConfig.From(primary).Merge(secondary).Build();

        Assert.Contains("server-a", config.Servers);
        Assert.Contains("server-c", config.Servers);
    }

    #endregion

    #region 多源合并优先级测试

    /// <summary>
    ///     测试多源合并时后面的源覆盖前面的源的同名字段。
    /// </summary>
    [Fact]
    public void MultiSourceMerge_LaterSourceOverrides_EarlierField()
    {
        var first = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["host"] = "first-host",
            ["port"] = "1111"
        });
        var second = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["host"] = "second-host"
        });

        var config = BasicConfig.From(first).Merge(second).Build();

        Assert.Equal("second-host", config.Host);
        Assert.Equal(1111, config.Port);
    }

    #endregion

    #region Builder 工厂方法测试

    /// <summary>
    ///     测试 From(memoryProvider).Build() 生成的 Builder 可以正确构建配置实例。
    /// </summary>
    [Fact]
    public void Builder_FromMemory_BuildsCorrectly()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "example.com",
            ["port"] = "9090",
            ["debug"] = "true"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = BasicConfig.From(provider).Build();

        Assert.Equal("example.com", config.Host);
        Assert.Equal(9090, config.Port);
        Assert.True(config.Debug);
    }

    /// <summary>
    ///     测试 Builder 使用默认值构建配置实例。
    /// </summary>
    [Fact]
    public void Builder_DefaultValues_AreApplied()
    {
        var provider = ConfigSource.FromMemory(new Dictionary<string, string>());

        var config = BasicConfig.From(provider).Build();

        Assert.Equal("localhost", config.Host);
        Assert.Equal(8080, config.Port);
        Assert.False(config.Debug);
    }

    /// <summary>
    ///     测试 Builder.Merge 合并多个配置源。
    /// </summary>
    [Fact]
    public void Builder_Merge_CombinesMultipleSources()
    {
        var primary = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["host"] = "primary-host"
        });
        var secondary = ConfigSource.FromMemory(new Dictionary<string, string>
        {
            ["port"] = "3000",
            ["debug"] = "true"
        });

        var config = BasicConfig.From(primary).Merge(secondary).Build();

        Assert.Equal("primary-host", config.Host);
        Assert.Equal(3000, config.Port);
        Assert.True(config.Debug);
    }

    /// <summary>
    ///     测试 Builder.WithoutValidation 跳过验证。
    /// </summary>
    [Fact]
    public void Builder_WithoutValidation_SkipsValidation()
    {
        var provider = ConfigSource.FromMemory(new Dictionary<string, string>());

        var config = ValidatedConfig.From(provider).WithoutValidation().Build();

        Assert.Equal("", config.AppName);
    }

    #endregion

    #region IConfigurable.Validate 测试

    /// <summary>
    ///     测试 IConfigurable.Validate 对 [Required] 属性返回验证错误。
    /// </summary>
    [Fact]
    public void Validate_RequiredFieldEmpty_ReturnsError()
    {
        var provider = ConfigSource.FromMemory(new Dictionary<string, string>());

        var config = ValidatedConfig.From(provider).WithoutValidation().Build();

        var errors = config.Validate();
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("AppName"));
    }

    /// <summary>
    ///     测试 IConfigurable.Validate 对满足条件的配置返回空错误列表。
    /// </summary>
    [Fact]
    public void Validate_AllFieldsValid_ReturnsNoErrors()
    {
        var data = new Dictionary<string, string>
        {
            ["appName"] = "MyApp",
            ["apiKey"] = "secret-key"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = ValidatedConfig.From(provider).Build();

        var errors = config.Validate();
        Assert.Empty(errors);
    }

    #endregion

    #region IConfigurable.Clone 测试

    /// <summary>
    ///     测试 IConfigurable.Clone 创建深克隆副本。
    /// </summary>
    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "original.com",
            ["port"] = "5000"
        };
        var provider = ConfigSource.FromMemory(data);

        var original = BasicConfig.From(provider).Build();
        var cloned = (BasicConfig)original.Clone();

        Assert.NotSame(original, cloned);
        Assert.Equal(original.Host, cloned.Host);
        Assert.Equal(original.Port, cloned.Port);
    }

    /// <summary>
    ///     测试 Clone 的副本修改不影响原始实例。
    /// </summary>
    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "original.com"
        };
        var provider = ConfigSource.FromMemory(data);

        var original = BasicConfig.From(provider).Build();
        var cloned = (BasicConfig)original.Clone();
        cloned.Host = "modified.com";

        Assert.Equal("original.com", original.Host);
        Assert.Equal("modified.com", cloned.Host);
    }

    #endregion

    #region IConfigurable.GetSchema 测试

    /// <summary>
    ///     测试 IConfigurable.GetSchema 返回有效的 JSON Schema 字符串。
    /// </summary>
    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "localhost"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = BasicConfig.From(provider).Build();
        var schema = config.GetSchema();

        Assert.Contains("json-schema.org", schema);
        Assert.Contains("BasicConfig", schema);
        Assert.Contains("host", schema);
        Assert.Contains("port", schema);
    }

    /// <summary>
    ///     测试 GetSchema 对 [Sensitive] 属性标记 writeOnly。
    /// </summary>
    [Fact]
    public void GetSchema_SensitiveProperty_MarkedWriteOnly()
    {
        var data = new Dictionary<string, string>
        {
            ["appName"] = "TestApp",
            ["apiKey"] = "secret"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = ValidatedConfig.From(provider).Build();
        var schema = config.GetSchema();

        Assert.Contains("writeOnly", schema);
    }

    /// <summary>
    ///     测试 GetSchema 对 [Required] 属性标记 required 数组。
    /// </summary>
    [Fact]
    public void GetSchema_RequiredProperty_InRequiredArray()
    {
        var data = new Dictionary<string, string>
        {
            ["appName"] = "TestApp",
            ["apiKey"] = "secret"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = ValidatedConfig.From(provider).Build();
        var schema = config.GetSchema();

        Assert.Contains("required", schema);
        Assert.Contains("appName", schema);
    }

    #endregion

    #region ToSafeString 测试

    /// <summary>
    ///     测试 ToSafeString 遮蔽 [Sensitive] 属性为 ***。
    /// </summary>
    [Fact]
    public void ToSafeString_SensitiveProperty_Masked()
    {
        var data = new Dictionary<string, string>
        {
            ["appName"] = "MyApp",
            ["apiKey"] = "super-secret-key"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = ValidatedConfig.From(provider).Build();
        var safeString = config.ToSafeString();

        Assert.DoesNotContain("super-secret-key", safeString);
        Assert.Contains("***", safeString);
        Assert.Contains("MyApp", safeString);
    }

    /// <summary>
    ///     测试 ToSafeString 对非敏感属性正常输出。
    /// </summary>
    [Fact]
    public void ToSafeString_NonSensitiveProperty_Visible()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "example.com",
            ["port"] = "8080"
        };
        var provider = ConfigSource.FromMemory(data);

        var config = BasicConfig.From(provider).Build();
        var safeString = config.ToSafeString();

        Assert.Contains("example.com", safeString);
        Assert.Contains("8080", safeString);
    }

    #endregion
}