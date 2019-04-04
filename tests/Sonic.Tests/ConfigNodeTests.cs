namespace Sonic.Testing.Config;

/// <summary>
///     ConfigNode 类型体系的单元测试。
/// </summary>
public class ConfigNodeTests
{
    #region ObjectConfigNode 测试

    /// <summary>
    ///     测试 ObjectConfigNode 使用字段字典创建后 GetField 返回正确的节点。
    /// </summary>
    [Fact]
    public void ObjectConfigNode_GetField_ReturnsCorrectNode()
    {
        var fields = new Dictionary<string, ConfigNode>
        {
            ["host"] = new ScalarConfigNode("localhost"),
            ["port"] = new ScalarConfigNode("8080")
        };
        var node = new ObjectConfigNode(fields);

        var hostNode = node.get_field("host");

        Assert.NotNull(hostNode);
        Assert.Equal(ConfigNode.NodeType.Scalar, hostNode.type);
        Assert.Equal("localhost", hostNode.AsString());
    }

    /// <summary>
    ///     测试 ObjectConfigNode 对不存在的字段返回 null。
    /// </summary>
    [Fact]
    public void ObjectConfigNode_GetField_NotFound_ReturnsNull()
    {
        var fields = new Dictionary<string, ConfigNode>
        {
            ["host"] = new ScalarConfigNode("localhost")
        };
        var node = new ObjectConfigNode(fields);

        var result = node.get_field("missing");

        Assert.Null(result);
    }

    /// <summary>
    ///     测试 ObjectConfigNode 的 EnumerateFields 返回所有字段。
    /// </summary>
    [Fact]
    public void ObjectConfigNode_EnumerateFields_ReturnsAllFields()
    {
        var fields = new Dictionary<string, ConfigNode>
        {
            ["host"] = new ScalarConfigNode("localhost"),
            ["port"] = new ScalarConfigNode("8080"),
            ["debug"] = new ScalarConfigNode("true")
        };
        var node = new ObjectConfigNode(fields);

        var enumerated = node.EnumerateFields().ToList();

        Assert.Equal(3, enumerated.Count);
        Assert.Contains(enumerated, kvp => kvp.Key == "host");
        Assert.Contains(enumerated, kvp => kvp.Key == "port");
        Assert.Contains(enumerated, kvp => kvp.Key == "debug");
    }

    /// <summary>
    ///     测试 ObjectConfigNode 的 Type 属性始终为 Object。
    /// </summary>
    [Fact]
    public void ObjectConfigNode_Type_IsObject()
    {
        var node = new ObjectConfigNode(new Dictionary<string, ConfigNode>());

        Assert.Equal(ConfigNode.NodeType.Object, node.type);
    }

    /// <summary>
    ///     测试 ObjectConfigNode 使用 null 字典创建时不会抛出异常。
    /// </summary>
    [Fact]
    public void ObjectConfigNode_NullFields_CreatesEmptyNode()
    {
        var node = new ObjectConfigNode(null!);

        Assert.Equal(ConfigNode.NodeType.Object, node.type);
        Assert.Empty(node.EnumerateFields());
    }

    #endregion

    #region ArrayConfigNode 测试

    /// <summary>
    ///     测试 ArrayConfigNode 使用元素列表创建后 EnumerateArray 返回所有元素。
    /// </summary>
    [Fact]
    public void ArrayConfigNode_EnumerateArray_ReturnsAllItems()
    {
        var items = new List<ConfigNode>
        {
            new ScalarConfigNode("alpha"),
            new ScalarConfigNode("beta"),
            new ScalarConfigNode("gamma")
        };
        var node = new ArrayConfigNode(items);

        var enumerated = node.EnumerateArray().ToList();

        Assert.Equal(3, enumerated.Count);
        Assert.Equal("alpha", enumerated[0].AsString());
        Assert.Equal("beta", enumerated[1].AsString());
        Assert.Equal("gamma", enumerated[2].AsString());
    }

    /// <summary>
    ///     测试 ArrayConfigNode 的 Type 属性始终为 Array。
    /// </summary>
    [Fact]
    public void ArrayConfigNode_Type_IsArray()
    {
        var node = new ArrayConfigNode(new List<ConfigNode>());

        Assert.Equal(ConfigNode.NodeType.Array, node.type);
    }

    /// <summary>
    ///     测试 ArrayConfigNode 使用 null 列表创建时不会抛出异常。
    /// </summary>
    [Fact]
    public void ArrayConfigNode_NullItems_CreatesEmptyNode()
    {
        var node = new ArrayConfigNode(null!);

        Assert.Equal(ConfigNode.NodeType.Array, node.type);
        Assert.Empty(node.EnumerateArray());
    }

    #endregion

    #region ScalarConfigNode 测试

    /// <summary>
    ///     测试 ScalarConfigNode 的 Type 属性始终为 Scalar。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_Type_IsScalar()
    {
        var node = new ScalarConfigNode("hello");

        Assert.Equal(ConfigNode.NodeType.Scalar, node.type);
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsString 返回原始字符串值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsString_ReturnsValue()
    {
        var node = new ScalarConfigNode("hello world");

        Assert.Equal("hello world", node.AsString());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsInt32 对有效整数返回正确值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsInt32_ValidInteger_ReturnsValue()
    {
        var node = new ScalarConfigNode("42");

        Assert.Equal(42, node.AsInt32());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsInt32 对无效值返回 0。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsInt32_InvalidValue_ReturnsZero()
    {
        var node = new ScalarConfigNode("not_a_number");

        Assert.Equal(0, node.AsInt32());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsBoolean 对 "true" 返回 true。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsBoolean_TrueValue_ReturnsTrue()
    {
        var node = new ScalarConfigNode("true");

        Assert.True(node.AsBoolean());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsBoolean 对 "false" 返回 false。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsBoolean_FalseValue_ReturnsFalse()
    {
        var node = new ScalarConfigNode("false");

        Assert.False(node.AsBoolean());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsBoolean 对无效值返回 false。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsBoolean_InvalidValue_ReturnsFalse()
    {
        var node = new ScalarConfigNode("maybe");

        Assert.False(node.AsBoolean());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsDouble 对有效浮点数返回正确值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsDouble_ValidDouble_ReturnsValue()
    {
        var node = new ScalarConfigNode("3.14");

        Assert.Equal(3.14, node.AsDouble(), 0.001);
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsDouble 对无效值返回 0。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsDouble_InvalidValue_ReturnsZero()
    {
        var node = new ScalarConfigNode("abc");

        Assert.Equal(0.0, node.AsDouble());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 使用 null 值创建时 AsString 返回空字符串。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_NullValue_AsStringReturnsEmpty()
    {
        var node = new ScalarConfigNode(null!);

        Assert.Equal(string.Empty, node.AsString());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsInt64 对有效长整数返回正确值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsInt64_ValidLong_ReturnsValue()
    {
        var node = new ScalarConfigNode("9999999999");

        Assert.Equal(9999999999L, node.AsInt64());
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsFloat 对有效浮点数返回正确值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsFloat_ValidFloat_ReturnsValue()
    {
        var node = new ScalarConfigNode("2.5");

        Assert.Equal(2.5f, node.AsFloat(), 0.001);
    }

    /// <summary>
    ///     测试 ScalarConfigNode 的 AsDecimal 对有效十进制数返回正确值。
    /// </summary>
    [Fact]
    public void ScalarConfigNode_AsDecimal_ValidDecimal_ReturnsValue()
    {
        var node = new ScalarConfigNode("99.99");

        Assert.Equal(99.99m, node.AsDecimal());
    }

    #endregion

    #region NullConfigNode 测试

    /// <summary>
    ///     测试 NullConfigNode 的 Instance 是单例。
    /// </summary>
    [Fact]
    public void NullConfigNode_Instance_IsSingleton()
    {
        var a = NullConfigNode.Instance;
        var b = NullConfigNode.Instance;

        Assert.Same(a, b);
    }

    /// <summary>
    ///     测试 NullConfigNode 的 Type 属性始终为 Null。
    /// </summary>
    [Fact]
    public void NullConfigNode_Type_IsNull()
    {
        Assert.Equal(ConfigNode.NodeType.Null, NullConfigNode.Instance.type);
    }

    #endregion

    #region ConfigNode 虚方法默认值测试

    /// <summary>
    ///     测试非标量节点的 AsString 返回 null。
    /// </summary>
    [Fact]
    public void NonScalarNode_AsString_ReturnsNull()
    {
        ConfigNode node = new ArrayConfigNode(new List<ConfigNode>());

        Assert.Null(node.AsString());
    }

    /// <summary>
    ///     测试非标量节点的 AsInt32 返回 0。
    /// </summary>
    [Fact]
    public void NonScalarNode_AsInt32_ReturnsZero()
    {
        ConfigNode node = NullConfigNode.Instance;

        Assert.Equal(0, node.AsInt32());
    }

    /// <summary>
    ///     测试非标量节点的 AsBoolean 返回 false。
    /// </summary>
    [Fact]
    public void NonScalarNode_AsBoolean_ReturnsFalse()
    {
        ConfigNode node = new ObjectConfigNode(new Dictionary<string, ConfigNode>());

        Assert.False(node.AsBoolean());
    }

    /// <summary>
    ///     测试非标量节点的 AsDouble 返回 0。
    /// </summary>
    [Fact]
    public void NonScalarNode_AsDouble_ReturnsZero()
    {
        ConfigNode node = new ArrayConfigNode(new List<ConfigNode>());

        Assert.Equal(0.0, node.AsDouble());
    }

    /// <summary>
    ///     测试非对象节点的 GetField 返回 null。
    /// </summary>
    [Fact]
    public void NonObjectNode_GetField_ReturnsNull()
    {
        ConfigNode node = new ScalarConfigNode("value");

        Assert.Null(node.get_field("any"));
    }

    /// <summary>
    ///     测试非对象节点的 EnumerateFields 返回空序列。
    /// </summary>
    [Fact]
    public void NonObjectNode_EnumerateFields_ReturnsEmpty()
    {
        ConfigNode node = new ScalarConfigNode("value");

        Assert.Empty(node.EnumerateFields());
    }

    /// <summary>
    ///     测试非数组节点的 EnumerateArray 返回空序列。
    /// </summary>
    [Fact]
    public void NonArrayNode_EnumerateArray_ReturnsEmpty()
    {
        ConfigNode node = new ObjectConfigNode(new Dictionary<string, ConfigNode>());

        Assert.Empty(node.EnumerateArray());
    }

    #endregion
}