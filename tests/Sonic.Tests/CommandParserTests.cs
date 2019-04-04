namespace Commander.Testing;

/// <summary>
///     CommandParser 类型转换测试。
/// </summary>
public class CommandParserTests
{
    /// <summary>
    ///     测试字符串类型。
    /// </summary>
    [Fact]
    public void convert_value_String()
    {
        Assert.Equal("hello", CommandParser.convert_value<string>("hello"));
    }

    /// <summary>
    ///     测试整数类型。
    /// </summary>
    [Fact]
    public void convert_value_Int()
    {
        Assert.Equal(42, CommandParser.convert_value<int>("42"));
    }

    /// <summary>
    ///     测试布尔类型。
    /// </summary>
    [Fact]
    public void convert_value_Bool()
    {
        Assert.True(CommandParser.convert_value<bool>("True"));
        Assert.False(CommandParser.convert_value<bool>("False"));
    }

    /// <summary>
    ///     测试双精度类型。
    /// </summary>
    [Fact]
    public void convert_value_Double()
    {
        Assert.Equal(3.14, CommandParser.convert_value<double>("3.14"), 0.001);
    }
}