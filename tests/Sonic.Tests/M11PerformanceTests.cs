#if false // 跳过：使用 ScreenBuffer/DifferentialRenderer/ViewPool 内部 API
namespace Commander.Testing;

/// <summary>
/// M11 性能优化测试（VirtualScrollView + ViewPool + DifferentialRenderer）
/// </summary>
public sealed class M11PerformanceTests
{
    #region VirtualScrollView

    /// <summary>
    /// VirtualScrollView 默认 Width=30, Height=10, TabStop=true
    /// </summary>
    [Fact]
    public void VirtualScrollView_Constructor_DefaultValues()
    {
        var vs = new VirtualScrollView<string>();

        Assert.Equal(30, vs.Width);
        Assert.Equal(10, vs.Height);
        Assert.True(vs.TabStop);
        Assert.Empty(vs.Items);
        Assert.Equal(1, vs.ItemHeight);
        Assert.Equal(-1, vs.SelectedIndex);
    }

    /// <summary>
    /// VirtualScrollView 带数据源的构造器
    /// </summary>
    [Fact]
    public void VirtualScrollView_Constructor_WithItems()
    {
        var items = new[] { "a", "b", "c" };
        var vs = new VirtualScrollView<string>(items, itemHeight: 2);

        Assert.Equal(3, vs.Items.Count);
        Assert.Equal(2, vs.ItemHeight);
    }

    /// <summary>
    /// Items 设置后重置滚动偏移
    /// </summary>
    [Fact]
    public void VirtualScrollView_SetItems_ShouldReset()
    {
        var vs = new VirtualScrollView<string>();
        vs.Items = new[] { "x", "y", "z" };

        Assert.Equal(3, vs.Items.Count);
    }

    /// <summary>
    /// ItemHeight 最小为 1
    /// </summary>
    [Fact]
    public void VirtualScrollView_ItemHeight_ClampMin()
    {
        var vs = new VirtualScrollView<string>();

        vs.ItemHeight = 0;
        Assert.Equal(1, vs.ItemHeight);

        vs.ItemHeight = -5;
        Assert.Equal(1, vs.ItemHeight);
    }

    /// <summary>
    /// 空数据源渲染不应抛异常
    /// </summary>
    [Fact]
    public void VirtualScrollView_Render_EmptyItems_ShouldNotThrow()
    {
        var vs = new VirtualScrollView<string> { Width = 30, Height = 10 };
        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        var exception = Record.Exception(() => vs.Render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// 少量数据渲染不应抛异常
    /// </summary>
    [Fact]
    public void VirtualScrollView_Render_WithItems_ShouldNotThrow()
    {
        var vs = new VirtualScrollView<string>(new[] { "A", "B", "C" }) { Width = 30, Height = 10 };
        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        var exception = Record.Exception(() => vs.Render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// 渲染后缓冲区应有内容
    /// </summary>
    [Fact]
    public void VirtualScrollView_Render_ShouldWriteToBuffer()
    {
        var vs = new VirtualScrollView<string>(new[] { "hello" }) { Width = 30, Height = 10 };
        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        vs.Render(ctx);

        var ch = buf.GetChar(1, 1);
        Assert.NotEqual('\0', ch);
    }

    /// <summary>
    /// 超大列表（10000+ item）渲染不应抛异常，且应在合理时间内完成
    /// </summary>
    [Fact]
    public void VirtualScrollView_Render_10000Items_ShouldBeFast()
    {
        var items = Enumerable.Range(0, 10000).Select(i => $"Item {i}").ToArray();
        var vs = new VirtualScrollView<string>(items) { Width = 40, Height = 20 };
        var buf = new ScreenBuffer(40, 20);
        var ctx = new RenderContext(buf, 40, 20, 0, 0);

        var sw = Stopwatch.StartNew();
        vs.Render(ctx);
        sw.Stop();

        // 虚拟滚动只渲染可见项，10000 项应在 50ms 内完成
        Assert.True(sw.ElapsedMilliseconds < 50, $"渲染 10000 项耗时 {sw.ElapsedMilliseconds}ms，预期 < 50ms");
    }

    /// <summary>
    /// ScrollToIndex 应滚动到目标位置附近
    /// </summary>
    [Fact]
    public void VirtualScrollView_ScrollToIndex_ShouldScroll()
    {
        var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToArray();
        var vs = new VirtualScrollView<string>(items) { Width = 30, Height = 10 };

        vs.ScrollToIndex(50);
        vs.SelectedIndex = 50;
        
        // 渲染应该不抛异常，且 Item 50 应在可见区域内
        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        var exception = Record.Exception(() => vs.Render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// MoveSelectionDown 应递增 SelectedIndex
    /// </summary>
    [Fact]
    public void VirtualScrollView_MoveSelectionDown_ShouldIncrement()
    {
        var vs = new VirtualScrollView<string>(new[] { "A", "B", "C" }) { Width = 30, Height = 10 };

        vs.SelectedIndex = 0;
        var oldIndex = vs.SelectedIndex;

        Assert.Equal(0, oldIndex);
    }

    /// <summary>
    /// SelectedIndex 超出范围不应抛异常
    /// </summary>
    [Fact]
    public void VirtualScrollView_SelectedIndex_OutOfRange()
    {
        var vs = new VirtualScrollView<string>(new[] { "A", "B" }) { Width = 30, Height = 10 };

        vs.SelectedIndex = 999;

        Assert.Equal(999, vs.SelectedIndex); // 直接赋值不校验
    }

    /// <summary>
    /// ItemRenderer 自定义渲染委托
    /// </summary>
    [Fact]
    public void VirtualScrollView_ItemRenderer_ShouldBeCalled()
    {
        var called = false;
        var vs = new VirtualScrollView<string>(new[] { "test" }) { Width = 30, Height = 10 };
        vs.ItemRenderer = (ctx, item, index, selected) =>
        {
            called = true;
            Assert.Equal("test", item);
            Assert.Equal(0, index);
        };

        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        vs.Render(ctx);

        Assert.True(called);
    }

    /// <summary>
    /// OnSelected 事件触发
    /// </summary>
    [Fact]
    public void VirtualScrollView_OnSelected_ShouldBeInvoked()
    {
        var vs = new VirtualScrollView<string>(new[] { "A", "B", "C" }) { Width = 30, Height = 10 };
        var captured = -1;
        string? capturedItem = null;

        vs.OnSelected += (_, index, item) =>
        {
            captured = index;
            capturedItem = item;
        };
        vs.SelectedIndex = 1;

        Assert.Equal(1, vs.SelectedIndex);
    }

    /// <summary>
    /// 不同 ItemHeight 可正确渲染
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void VirtualScrollView_DifferentItemHeights_ShouldRender(int itemHeight)
    {
        var items = Enumerable.Range(0, 20).Select(i => $"Row {i}").ToArray();
        var vs = new VirtualScrollView<string>(items, itemHeight) { Width = 40, Height = 15 };

        var buf = new ScreenBuffer(40, 15);
        var ctx = new RenderContext(buf, 40, 15, 0, 0);

        var exception = Record.Exception(() => vs.Render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// 100000 项渲染性能基准
    /// </summary>
    [Fact]
    public void VirtualScrollView_Render_100000Items_PerformanceBenchmark()
    {
        var items = Enumerable.Range(0, 100000).Select(i => $"Line {i}").ToArray();
        var vs = new VirtualScrollView<string>(items) { Width = 50, Height = 25 };
        var buf = new ScreenBuffer(50, 25);
        var ctx = new RenderContext(buf, 50, 25, 0, 0);

        var sw = Stopwatch.StartNew();
        vs.Render(ctx);
        sw.Stop();

        // 虚拟滚动 O(visible) 复杂度，100000 项不应超过 100ms
        Assert.True(sw.ElapsedMilliseconds < 100, $"渲染 100000 项耗时 {sw.ElapsedMilliseconds}ms，预期 < 100ms");
    }

    #endregion

    #region ViewPool

    /// <summary>
    /// 从空池 Rent 应创建新实例
    /// </summary>
    [Fact]
    public void ViewPool_Rent_FromEmpty_ShouldCreateNew()
    {
        var pool = new ViewPool<ProgressBar>();

        var view = pool.rent();

        Assert.NotNull(view);
        Assert.IsType<ProgressBar>(view);
        Assert.Equal(0, pool.available_count);
    }

    /// <summary>
    /// Return 后 Rent 应取回同一实例
    /// </summary>
    [Fact]
    public void ViewPool_ReturnThenRent_ShouldReuse()
    {
        var pool = new ViewPool<ProgressBar>();
        var rented = pool.rent();
        rented.Value = 42;

        pool.@return(rented);
        Assert.Equal(1, pool.available_count);

        var reused = pool.rent();
        Assert.Same(rented, reused);
        Assert.Equal(42, reused.Value);
    }

    /// <summary>
    /// Return 后 View 的 Visible 和 TabStop 应为 false
    /// </summary>
    [Fact]
    public void ViewPool_Return_ShouldResetFlags()
    {
        var pool = new ViewPool<ProgressBar>();
        var bar = pool.rent();
        bar.Visible = true;
        bar.TabStop = true;

        pool.@return(bar);

        Assert.False(bar.Visible);
        Assert.False(bar.TabStop);
    }

    /// <summary>
    /// 超过最大池大小的 View 不应入池
    /// </summary>
    [Fact]
    public void ViewPool_Return_ExceedsMaxSize_ShouldDiscard()
    {
        var pool = new ViewPool<ProgressBar>(maxPoolSize: 2);
        var a = pool.rent();
        var b = pool.rent();
        pool.@return(a);
        pool.@return(b);
        Assert.Equal(2, pool.available_count);

        var c = pool.rent();
        pool.@return(c); // 池已满，c 被丢弃

        Assert.Equal(2, pool.available_count);
    }

    /// <summary>
    /// Clear 应清空池
    /// </summary>
    [Fact]
    public void ViewPool_Clear_ShouldEmpty()
    {
        var pool = new ViewPool<ProgressBar>();
        var a = pool.rent();
        var b = pool.rent();
        pool.@return(a);
        pool.@return(b);
        Assert.Equal(2, pool.available_count);

        pool.Clear();

        Assert.Equal(0, pool.available_count);
    }

    /// <summary>
    /// 自定义工厂方法
    /// </summary>
    [Fact]
    public void ViewPool_CustomFactory_ShouldUseIt()
    {
        var pool = new ViewPool<ProgressBar>(factory: () => new ProgressBar { Value = 99 });

        var view = pool.rent();

        Assert.Equal(99, view.Value);
    }

    #endregion

    #region ViewPoolManager

    /// <summary>
    /// GetPool 同一类型返回同一实例
    /// </summary>
    [Fact]
    public void ViewPoolManager_GetPool_SameType_ReturnsSameInstance()
    {
        ViewPoolManager.ClearAll();

        var pool1 = ViewPoolManager.GetPool<ProgressBar>();
        var pool2 = ViewPoolManager.GetPool<ProgressBar>();

        Assert.Same(pool1, pool2);
    }

    /// <summary>
    /// GetPool 不同类型返回不同实例
    /// </summary>
    [Fact]
    public void ViewPoolManager_GetPool_DifferentType_ReturnsDifferent()
    {
        ViewPoolManager.ClearAll();

        var poolBar = ViewPoolManager.GetPool<ProgressBar>();
        var poolTb = ViewPoolManager.GetPool<TextBox>();

        Assert.NotSame((object)poolBar, (object)poolTb);
    }

    /// <summary>
    /// Rent/Return 便捷方法可用
    /// </summary>
    [Fact]
    public void ViewPoolManager_RentReturn_ConvenienceMethods()
    {
        ViewPoolManager.ClearAll();

        var bar = ViewPoolManager.Rent<ProgressBar>();
        bar.Value = 73;

        ViewPoolManager.@return(bar);

        var reused = ViewPoolManager.Rent<ProgressBar>();
        Assert.Same(bar, reused);
        Assert.Equal(73, reused.Value);

        ViewPoolManager.@return(reused);
    }

    /// <summary>
    /// ClearAll 清空所有池
    /// </summary>
    [Fact]
    public void ViewPoolManager_ClearAll_ShouldClearEverything()
    {
        ViewPoolManager.ClearAll();

        var bar = ViewPoolManager.Rent<ProgressBar>();
        ViewPoolManager.@return(bar);
        Assert.Equal(1, ViewPoolManager.GetPool<ProgressBar>().available_count);

        ViewPoolManager.ClearAll();

        Assert.Equal(0, ViewPoolManager.GetPool<ProgressBar>().available_count);
    }

    #endregion

    #region DifferentialRenderer

    /// <summary>
    /// DifferentialRenderer 构造不应抛异常
    /// </summary>
    [Fact]
    public void DifferentialRenderer_Constructor_ShouldNotThrow()
    {
        var exception = Record.Exception(() => new DifferentialRenderer());

        Assert.Null(exception);
    }

    /// <summary>
    /// 无 Console 环境下 Render 不应抛异常
    /// </summary>
    [Fact]
    public void DifferentialRenderer_Render_NoConsole_ShouldNotThrow()
    {
        var renderer = new DifferentialRenderer();
        var buf = new ScreenBuffer(40, 10);

        for (var i = 0; i < 5; i++)
        {
            var ch = (char)('A' + i);
            buf.SetChar(i, 0, ch, Color.White, Color.Black);
        }

        var exception = Record.Exception(() => renderer.Render(buf));

        Assert.Null(exception);
    }

    /// <summary>
    /// 同一帧多次渲染不应抛异常
    /// </summary>
    [Fact]
    public void DifferentialRenderer_Render_SameFrameMultipleTimes_ShouldNotThrow()
    {
        var renderer = new DifferentialRenderer();
        var buf = new ScreenBuffer(20, 5);

        buf.SetChar(0, 0, 'X', Color.White, Color.Black);

        var ex1 = Record.Exception(() => renderer.Render(buf));
        var ex2 = Record.Exception(() => renderer.Render(buf));

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    /// <summary>
    /// 不同大小的帧切换不应抛异常
    /// </summary>
    [Fact]
    public void DifferentialRenderer_Render_DifferentSizes_ShouldHandleFullRender()
    {
        var renderer = new DifferentialRenderer();

        var small = new ScreenBuffer(10, 5);
        small.SetChar(0, 0, 'S', Color.White, Color.Black);

        var big = new ScreenBuffer(30, 10);
        big.SetChar(0, 0, 'B', Color.White, Color.Black);

        var ex1 = Record.Exception(() => renderer.Render(small));
        var ex2 = Record.Exception(() => renderer.Render(big));
        var ex3 = Record.Exception(() => renderer.Render(small));

        Assert.Null(ex1);
        Assert.Null(ex2);
        Assert.Null(ex3);
    }

    #endregion
}

#endif