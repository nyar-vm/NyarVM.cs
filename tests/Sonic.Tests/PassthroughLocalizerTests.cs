namespace Commander.Testing;

/// <summary>
///     PassthroughLocalizer 本地化器测试。
/// </summary>
public class PassthroughLocalizerTests
{
    /// <summary>
    ///     测试返回回退值。
    /// </summary>
    [Fact]
    public void get_string_WithFallback_ReturnsFallback()
    {
        var localizer = new PassthroughLocalizer();

        Assert.Equal("你好", localizer.get_string("greeting", "你好"));
    }

    /// <summary>
    ///     测试无回退值时返回键。
    /// </summary>
    [Fact]
    public void get_string_NoFallback_ReturnsKey()
    {
        var localizer = new PassthroughLocalizer();

        Assert.Equal("some.key", localizer.get_string("some.key"));
    }
}