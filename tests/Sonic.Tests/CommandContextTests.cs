namespace Commander.Testing;

/// <summary>
///     CommandContext 上下文测试。
/// </summary>
public class CommandContextTests
{
    /// <summary>
    ///     测试默认值。
    /// </summary>
    [Fact]
    public void DefaultValues_AllNull()
    {
        var ctx = new CommandContext();

        Assert.Null(ctx.console);
        Assert.Null(ctx.renderer);
        Assert.Null(ctx.localizer);
        Assert.Equal(CancellationToken.None, ctx.cancellation_token);
    }

    /// <summary>
    ///     测试属性赋值。
    /// </summary>
    [Fact]
    public void SetProperties_StoresValues()
    {
        var cts = new CancellationTokenSource();
        var ctx = new CommandContext
        {
            localizer = new PassthroughLocalizer(),
            cancellation_token = cts.Token
        };

        Assert.NotNull(ctx.localizer);
        Assert.Equal(cts.Token, ctx.cancellation_token);
    }
}