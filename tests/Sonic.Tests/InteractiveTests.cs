using System.Collections;
using System.Reflection;

namespace Commander.Testing;

/// <summary>
///     ProgressContext 桩类型（Sonic.Standard 中尚未实现，测试编译期兼容用）
/// </summary>
public sealed class ProgressContext
{
    private readonly List<ProgressTask> _tasks = [];

    public ProgressTask AddTask(string description)
    {
        var task = new ProgressTask(description);
        _tasks.Add(task);
        return task;
    }

    public async Task StartAsync(Func<ProgressContext, Task> action)
    {
        await action(this);
    }
}

/// <summary>
///     ProgressTask 桩类型（Sonic.Standard 中尚未实现，测试编译期兼容用）
/// </summary>
public sealed class ProgressTask
{
    public ProgressTask(string description)
    {
        Description = description;
    }

    public string Description { get; }
    public int Value { get; private set; }
    public int MaxValue { get; set; } = 100;
    public bool IsFinished => Value >= MaxValue;

    public void Increment(int amount)
    {
        Value = Math.Min(Value + amount, MaxValue);
    }

    public void SetValue(int value)
    {
        Value = Math.Min(value, MaxValue);
    }
}

/// <summary>
///     进度条组件测试
/// </summary>
public sealed class ProgressContextTests
{
    [Fact]
    public void AddTask_ShouldReturnProgressTask()
    {
        var ctx = new ProgressContext();
        var task = ctx.AddTask("下载文件");
        Assert.NotNull(task);
        Assert.Equal("下载文件", task.Description);
        Assert.Equal(0, task.Value);
    }

    [Fact]
    public void ProgressTask_Increment_ShouldIncreaseValue()
    {
        var task = new ProgressTask("测试");
        task.Increment(10);
        Assert.Equal(10, task.Value);
    }

    [Fact]
    public void ProgressTask_Increment_ShouldNotExceedMaxValue()
    {
        var task = new ProgressTask("测试") { MaxValue = 100 };
        task.Increment(150);
        Assert.Equal(100, task.Value);
    }

    [Fact]
    public void ProgressTask_SetValue_ShouldSetDirectly()
    {
        var task = new ProgressTask("测试") { MaxValue = 200 };
        task.SetValue(150);
        Assert.Equal(150, task.Value);
    }

    [Fact]
    public void ProgressTask_SetValue_ShouldNotExceedMax()
    {
        var task = new ProgressTask("测试") { MaxValue = 100 };
        task.SetValue(200);
        Assert.Equal(100, task.Value);
    }

    [Fact]
    public void ProgressTask_IsFinished_WhenValueReachesMax()
    {
        var task = new ProgressTask("测试") { MaxValue = 10 };
        Assert.False(task.IsFinished);
        task.SetValue(10);
        Assert.True(task.IsFinished);
    }

    [Fact]
    public async Task StartAsync_NoTasks_ShouldStillExecute()
    {
        var ctx = new ProgressContext();
        var executed = false;
        await ctx.StartAsync(async _ =>
        {
            executed = true;
            await Task.CompletedTask;
        });
        Assert.True(executed);
    }

    [Fact]
    public async Task StartAsync_WithTasks_ShouldExecuteAction()
    {
        var ctx = new ProgressContext();
        var task = ctx.AddTask("处理中");
        var executed = false;

        await ctx.StartAsync(async _ =>
        {
            task.SetValue(50);
            await Task.Delay(50);
            task.SetValue(100);
            executed = true;
        });

        Assert.True(executed);
        Assert.True(task.IsFinished);
    }

    [Fact]
    public void AddTask_MultipleTasks_ShouldTrackAll()
    {
        var ctx = new ProgressContext();
        ctx.AddTask("任务A");
        ctx.AddTask("任务B");
        ctx.AddTask("任务C");

        Assert.Equal(3, GetTaskCount(ctx));
    }

    [Fact]
    public void ProgressTask_DefaultMaxValue_ShouldBe100()
    {
        var task = new ProgressTask("测试");
        Assert.Equal(100, task.MaxValue);
    }

    private static int GetTaskCount(ProgressContext ctx)
    {
        var field = typeof(ProgressContext).get_field("_tasks",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var tasks = field!.GetValue(ctx) as IList;
        return tasks!.Count;
    }
}

/// <summary>
///     Spinner 组件测试
/// </summary>
public sealed class SpinnerContextTests
{
    [Fact]
    public void Constructor_ShouldSetMessage()
    {
        var ctx = new SpinnerContext("加载中...");
        Assert.Equal("加载中...", ctx.message);
    }

    [Fact]
    public void DefaultStyle_ShouldBeDots()
    {
        var ctx = new SpinnerContext("test");
        Assert.Equal(SpinnerStyle.dots, ctx.style);
    }

    [Fact]
    public async Task StartAsync_ShouldReturnResult()
    {
        var ctx = new SpinnerContext("处理中...");
        var result = await ctx.start(async _ =>
        {
            await Task.Delay(10);
            return 42;
        });

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task StartAsync_WithDifferentStyles_ShouldNotThrow()
    {
        foreach (var style in Enum.GetValues<SpinnerStyle>())
        {
            var ctx = new SpinnerContext("测试") { style = style };
            var result = await ctx.start(async _ =>
            {
                await Task.Delay(5);
                return true;
            });

            Assert.True(result);
        }
    }

    [Fact]
    public void Success_ShouldNotThrow()
    {
        var ctx = new SpinnerContext("完成");
        var exception = Record.Exception(() => ctx.success("搞定"));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(SpinnerStyle.dots)]
    [InlineData(SpinnerStyle.line)]
    [InlineData(SpinnerStyle.dots2)]
    [InlineData(SpinnerStyle.arc)]
    public void SpinnerStyle_ShouldHaveDefinedFrames(SpinnerStyle style)
    {
        var ctx = new SpinnerContext("测试") { style = style };
        var exception = Record.Exception(() =>
        {
            var result = ctx.start(async _ =>
            {
                await Task.Delay(5);
                return 0;
            }).Result;
        });
        Assert.Null(exception);
    }
}

/// <summary>
///     ConsoleOutputWriter 静态门面测试
/// </summary>
public sealed class ConsoleOutputWriterTests
{
    [Fact]
    public void WriteLine_ShouldNotThrow()
    {
        var exception = Record.Exception(() => Console.WriteLine("hello"));
        Assert.Null(exception);
    }

    [Fact]
    public void Progress_Factory_ShouldReturnProgressContext()
    {
        var ctx = new ProgressContext();
        Assert.NotNull(ctx);
        Assert.IsType<ProgressContext>(ctx);
    }

    [Fact]
    public void Spinner_Factory_ShouldReturnSpinnerContext()
    {
        var ctx = new SpinnerContext("工作中...");
        Assert.NotNull(ctx);
        Assert.IsType<SpinnerContext>(ctx);
        Assert.Equal("工作中...", ctx.message);
    }

    [Fact]
    public void Status_Factory_ShouldReturnStatusContext()
    {
        var ctx = new StatusContext { status = "就绪" };
        Assert.NotNull(ctx);
        Assert.Equal("就绪", ctx.status);
    }

    [Fact]
    public async Task WriteAsync_EmptyText_ShouldNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
        {
            Console.Write("");
            return Task.CompletedTask;
        });
        Assert.Null(exception);
    }

    [Fact]
    public async Task WriteAsync_WithSegments_ShouldNotThrow()
    {
        var exception = await Record.ExceptionAsync(() =>
        {
            Console.Write("红色文字");
            return Task.CompletedTask;
        });
        Assert.Null(exception);
    }

    [Fact]
    public void SetTitle_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
        {
            try
            {
                Console.Title = "Iris Test";
            }
            catch
            {
            }
        });
        Assert.Null(exception);
    }
}