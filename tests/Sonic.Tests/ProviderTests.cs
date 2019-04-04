using Core.Config;

namespace Sonic.Testing.Config;

/// <summary>
///     配置提供程序的单元测试。
/// </summary>
public class ProviderTests
{
    #region MemoryConfigProvider 测试

    /// <summary>
    ///     测试 MemoryConfigProvider 从字典创建后 Load 返回正确的配置树。
    /// </summary>
    [Fact]
    public void MemoryConfigProvider_Load_ReturnsCorrectTree()
    {
        var data = new Dictionary<string, string>
        {
            ["host"] = "localhost",
            ["port"] = "8080"
        };
        var provider = new MemoryConfigProvider(data);

        var root = provider.Load();

        Assert.Equal(ConfigNode.NodeType.Object, root.type);
        var hostNode = root.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("localhost", hostNode.AsString());
        var portNode = root.get_field("port");
        Assert.NotNull(portNode);
        Assert.Equal(8080, portNode.AsInt32());
    }

    /// <summary>
    ///     测试 MemoryConfigProvider 支持冒号分隔的嵌套路径。
    /// </summary>
    [Fact]
    public void MemoryConfigProvider_NestedPath_CreatesHierarchy()
    {
        var data = new Dictionary<string, string>
        {
            ["database:host"] = "localhost",
            ["database:port"] = "5432"
        };
        var provider = new MemoryConfigProvider(data);

        var root = provider.Load();

        var dbNode = root.get_field("database");
        Assert.NotNull(dbNode);
        Assert.Equal(ConfigNode.NodeType.Object, dbNode.type);
        var hostNode = dbNode.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("localhost", hostNode.AsString());
    }

    /// <summary>
    ///     测试 MemoryConfigProvider 对空字典返回空对象节点。
    /// </summary>
    [Fact]
    public void MemoryConfigProvider_EmptyDictionary_ReturnsEmptyObject()
    {
        var provider = new MemoryConfigProvider(new Dictionary<string, string>());

        var root = provider.Load();

        Assert.Equal(ConfigNode.NodeType.Object, root.type);
        Assert.Empty(root.EnumerateFields());
    }

    #endregion

    #region CliConfigProvider 测试

    /// <summary>
    ///     测试 CliConfigProvider 解析 --key=value 格式。
    /// </summary>
    [Fact]
    public void CliConfigProvider_KeyEqualsValue_ParsesCorrectly()
    {
        var args = new[] { "--host=localhost", "--port=8080" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        var hostNode = root.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("localhost", hostNode.AsString());
        var portNode = root.get_field("port");
        Assert.NotNull(portNode);
        Assert.Equal(8080, portNode.AsInt32());
    }

    /// <summary>
    ///     测试 CliConfigProvider 解析 --key value 格式。
    /// </summary>
    [Fact]
    public void CliConfigProvider_KeySpaceValue_ParsesCorrectly()
    {
        var args = new[] { "--host", "localhost", "--port", "8080" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        var hostNode = root.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("localhost", hostNode.AsString());
    }

    /// <summary>
    ///     测试 CliConfigProvider 解析 --no-flag 格式为布尔 false。
    /// </summary>
    [Fact]
    public void CliConfigProvider_NoFlag_ParsesAsFalse()
    {
        var args = new[] { "--no-verbose" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        var verboseNode = root.get_field("verbose");
        Assert.NotNull(verboseNode);
        Assert.False(verboseNode.AsBoolean());
    }

    /// <summary>
    ///     测试 CliConfigProvider 解析 --flag 格式为布尔 true。
    /// </summary>
    [Fact]
    public void CliConfigProvider_Flag_ParsesAsTrue()
    {
        var args = new[] { "--verbose" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        var verboseNode = root.get_field("verbose");
        Assert.NotNull(verboseNode);
        Assert.True(verboseNode.AsBoolean());
    }

    /// <summary>
    ///     测试 CliConfigProvider 支持冒号分隔的嵌套路径。
    /// </summary>
    [Fact]
    public void CliConfigProvider_NestedPath_CreatesHierarchy()
    {
        var args = new[] { "--database:host=localhost" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        var dbNode = root.get_field("database");
        Assert.NotNull(dbNode);
        var hostNode = dbNode.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("localhost", hostNode.AsString());
    }

    /// <summary>
    ///     测试 CliConfigProvider 忽略非 -- 开头的参数。
    /// </summary>
    [Fact]
    public void CliConfigProvider_NonDashArgs_IgnoresThem()
    {
        var args = new[] { "positional", "--host=localhost" };
        var provider = new CliConfigProvider(args);

        var root = provider.Load();

        Assert.NotNull(root.get_field("host"));
        Assert.Equal(1, root.EnumerateFields().Count());
    }

    /// <summary>
    ///     测试 CliConfigProvider 对空参数数组返回空对象节点。
    /// </summary>
    [Fact]
    public void CliConfigProvider_EmptyArgs_ReturnsEmptyObject()
    {
        var provider = new CliConfigProvider(Array.Empty<string>());

        var root = provider.Load();

        Assert.Equal(ConfigNode.NodeType.Object, root.type);
        Assert.Empty(root.EnumerateFields());
    }

    #endregion

    #region EnvironmentConfigProvider 测试

    /// <summary>
    ///     测试 EnvironmentConfigProvider 使用前缀过滤环境变量。
    /// </summary>
    [Fact]
    public void EnvironmentConfigProvider_WithPrefix_FiltersCorrectly()
    {
        var prefix = $"TEST_SONIC_{Guid.NewGuid():N}_";
        try
        {
            Environment.SetEnvironmentVariable($"{prefix}HOST", "localhost");
            Environment.SetEnvironmentVariable($"{prefix}PORT", "9090");
            Environment.SetEnvironmentVariable("UNRELATED_VAR", "ignored");

            var provider = new EnvironmentConfigProvider(prefix);

            var root = provider.Load();

            var hostNode = root.get_field("host");
            Assert.NotNull(hostNode);
            Assert.Equal("localhost", hostNode.AsString());
            Assert.Null(root.get_field("unrelated_var"));
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}HOST", null);
            Environment.SetEnvironmentVariable($"{prefix}PORT", null);
        }
    }

    /// <summary>
    ///     测试 EnvironmentConfigProvider 使用双下划线作为嵌套分隔符。
    /// </summary>
    [Fact]
    public void EnvironmentConfigProvider_DoubleUnderscore_CreatesHierarchy()
    {
        var prefix = $"TEST_SONIC_{Guid.NewGuid():N}_";
        try
        {
            Environment.SetEnvironmentVariable($"{prefix}DATABASE__HOST", "db.example.com");

            var provider = new EnvironmentConfigProvider(prefix);

            var root = provider.Load();

            var dbNode = root.get_field("database");
            Assert.NotNull(dbNode);
            var hostNode = dbNode.get_field("host");
            Assert.NotNull(hostNode);
            Assert.Equal("db.example.com", hostNode.AsString());
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{prefix}DATABASE__HOST", null);
        }
    }

    #endregion

    #region FallbackConfigProvider 测试

    /// <summary>
    ///     测试 FallbackConfigProvider 的 Select 组合子：前面的源优先，缺失时回退。
    /// </summary>
    [Fact]
    public void FallbackConfigProvider_Select_PrioritizesEarlierProvider()
    {
        var primary = new MemoryConfigProvider(new Dictionary<string, string>
        {
            ["host"] = "primary-host",
            ["port"] = "3000"
        });
        var fallback = new MemoryConfigProvider(new Dictionary<string, string>
        {
            ["host"] = "fallback-host",
            ["timeout"] = "30"
        });
        var provider = ConfigSource.Select(primary, fallback);

        var root = provider.Load();

        var hostNode = root.get_field("host");
        Assert.NotNull(hostNode);
        Assert.Equal("primary-host", hostNode.AsString());
        var timeoutNode = root.get_field("timeout");
        Assert.NotNull(timeoutNode);
        Assert.Equal(30, timeoutNode.AsInt32());
    }

    /// <summary>
    ///     测试 FallbackConfigProvider 对嵌套对象进行深度合并。
    /// </summary>
    [Fact]
    public void FallbackConfigProvider_DeepMerges_NestedObjects()
    {
        var primary = new MemoryConfigProvider(new Dictionary<string, string>
        {
            ["database:host"] = "primary-db"
        });
        var fallback = new MemoryConfigProvider(new Dictionary<string, string>
        {
            ["database:port"] = "5432"
        });
        var provider = ConfigSource.Select(primary, fallback);

        var root = provider.Load();

        var dbNode = root.get_field("database");
        Assert.NotNull(dbNode);
        Assert.Equal("primary-db", dbNode.get_field("host")?.AsString());
        Assert.Equal(5432, dbNode.get_field("port")?.AsInt32());
    }

    #endregion

    #region OptionalConfigProvider 测试

    /// <summary>
    ///     测试 OptionalConfigProvider 对缺失文件返回 NullConfigNode。
    /// </summary>
    [Fact]
    public void OptionalConfigProvider_MissingFile_ReturnsNullNode()
    {
        var inner = new JsonConfigProvider("nonexistent_file_12345.json");
        var provider = new OptionalConfigProvider(inner);

        var root = provider.Load();

        Assert.Equal(ConfigNode.NodeType.Null, root.type);
    }

    /// <summary>
    ///     测试 OptionalConfigProvider 扩展方法 Optional 包装正常。
    /// </summary>
    [Fact]
    public void OptionalConfigProvider_ExtensionMethod_WrapsCorrectly()
    {
        var inner = new JsonConfigProvider("nonexistent_file_12345.json");
        var provider = inner.Optional();

        var root = provider.Load();

        Assert.Equal(ConfigNode.NodeType.Null, root.type);
    }

    #endregion
}