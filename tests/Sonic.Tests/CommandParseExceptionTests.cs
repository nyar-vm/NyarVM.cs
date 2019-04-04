namespace Commander.Testing;

/// <summary>
///     CommandParseException 异常测试。
/// </summary>
public class CommandParseExceptionTests
{
    /// <summary>
    ///     测试单错误构造。
    /// </summary>
    [Fact]
    public void SingleError_ContainsMessage()
    {
        var ex = new CommandParseException("缺少参数");

        Assert.Single(ex.errors);
        Assert.Equal("缺少参数", ex.errors[0]);
        Assert.Contains("缺少参数", ex.Message);
    }

    /// <summary>
    ///     测试多错误构造。
    /// </summary>
    [Fact]
    public void MultipleErrors_ContainsAllMessages()
    {
        var errors = new List<string> { "错误1", "错误2" };
        var ex = new CommandParseException(errors);

        Assert.Equal(2, ex.errors.Count);
        Assert.Contains("错误1", ex.Message);
        Assert.Contains("错误2", ex.Message);
    }
}