using Commander.Testing.Commands;

namespace Commander.Testing;

/// <summary>
///     GetJsonSchema 生成测试。
/// </summary>
public class JsonSchemaTests
{
    /// <summary>
    ///     测试 Schema 包含 JSON Schema 基础结构。
    /// </summary>
    [Fact]
    public void GetJsonSchema_ContainsSchemaHeader()
    {
        var schema = GreetCommand.GetJsonSchema();

        Assert.Contains("json-schema.org", schema);
        Assert.Contains("\"type\":\"object\"", schema);
    }

    /// <summary>
    ///     测试 Schema 包含属性定义。
    /// </summary>
    [Fact]
    public void GetJsonSchema_ContainsProperties()
    {
        var schema = GreetCommand.GetJsonSchema();

        Assert.Contains("\"Name\"", schema);
        Assert.Contains("\"greeting\"", schema);
        Assert.Contains("\"loud\"", schema);
    }

    /// <summary>
    ///     测试 Schema 包含必需字段。
    /// </summary>
    [Fact]
    public void GetJsonSchema_ContainsRequired()
    {
        var schema = GreetCommand.GetJsonSchema();

        Assert.Contains("\"required\"", schema);
        Assert.Contains("\"Name\"", schema);
    }

    /// <summary>
    ///     测试布尔类型映射为 "boolean"。
    /// </summary>
    [Fact]
    public void GetJsonSchema_BoolType_MapsToBoolean()
    {
        var schema = GreetCommand.GetJsonSchema();

        Assert.Contains("\"loud\"", schema);
        Assert.Contains("boolean", schema);
    }

    /// <summary>
    ///     测试整数类型映射为 "integer"。
    /// </summary>
    [Fact]
    public void GetJsonSchema_IntType_MapsToInteger()
    {
        var schema = BuildCommand.GetJsonSchema();

        Assert.Contains("\"jobs\"", schema);
        Assert.Contains("integer", schema);
    }
}