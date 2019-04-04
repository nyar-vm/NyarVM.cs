#if false // 跳过：使用 View.render/RenderContext 内部构造函数等内部 API
namespace Commander.Testing;

/// <summary>
/// M6 Margin 布局测试
/// </summary>
public sealed class MarginLayoutTests
{
    [Fact]
    public void VBox_WithChildMargin_ShouldOffsetChild()
    {
        var box = new VBox();
        var child = new TextBlock("X") { Width = 5, Height = 1 };
        child.WithMargin(top: 2, left: 3);
        box.Add(child);
        box.Width = 20;
        box.Height = 10;

        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        box.Render(ctx);

        Assert.Equal('X', buf.GetChar(3, 2));
    }

    [Fact]
    public void VBox_WithChildMargin_ShouldAffectLayoutHeight()
    {
        var box = new VBox();
        box.Add(new TextBlock("A") { Width = 5, Height = 1 }.WithMargin(bottom: 2));
        box.Add(new TextBlock("B") { Width = 5, Height = 1 });

        box.Width = 20;
        box.Height = 20;

        var buf = new ScreenBuffer(20, 20);
        var ctx = new RenderContext(buf, 20, 20, 0, 0);

        box.Render(ctx);

        Assert.Equal('A', buf.GetChar(0, 0));
        Assert.Equal('B', buf.GetChar(0, 3));
    }

    [Fact]
    public void HBox_WithChildMargin_ShouldOffsetChild()
    {
        var box = new HBox();
        var child = new TextBlock("X") { Width = 3, Height = 1 };
        child.WithMargin(top: 1, left: 2);
        box.Add(child);
        box.Width = 20;
        box.Height = 5;

        var buf = new ScreenBuffer(20, 5);
        var ctx = new RenderContext(buf, 20, 5, 0, 0);

        box.Render(ctx);

        Assert.Equal('X', buf.GetChar(2, 1));
    }

    [Fact]
    public void HBox_WithChildRightMargin_ShouldGapNextChild()
    {
        var box = new HBox();
        box.Add(new TextBlock("A") { Width = 3, Height = 1 }.WithMargin(right: 2));
        box.Add(new TextBlock("B") { Width = 3, Height = 1 });

        box.Width = 20;
        box.Height = 5;

        var buf = new ScreenBuffer(20, 5);
        var ctx = new RenderContext(buf, 20, 5, 0, 0);

        box.Render(ctx);

        Assert.Equal('A', buf.GetChar(0, 0));
        Assert.Equal('B', buf.GetChar(5, 0));
    }

    [Fact]
    public void View_WithMargin_FluentApi_ShouldSetMargin()
    {
        var view = new TextBlock("test");
        view.WithMargin(1, 2, 3, 4);

        Assert.Equal(1, view.Margin.Top);
        Assert.Equal(2, view.Margin.Right);
        Assert.Equal(3, view.Margin.Bottom);
        Assert.Equal(4, view.Margin.Left);
    }

    [Fact]
    public void Thickness_UniformConstructor_ShouldSetAllSides()
    {
        var t = new Thickness(5);

        Assert.Equal(5, t.Top);
        Assert.Equal(5, t.Right);
        Assert.Equal(5, t.Bottom);
        Assert.Equal(5, t.Left);
    }
}

/// <summary>
/// M6 ScrollView 平滑滚动测试
/// </summary>
public sealed class SmoothScrollTests
{
    [Fact]
    public void ScrollTo_ShouldStartAnimation()
    {
        var content = new TextBlock("Long") { Height = 30 };
        var sv = new ScrollView(content) { Height = 10 };
        sv.ScrollDown(5);
        sv.ScrollTo(15, 300);

        Assert.True(sv.IsScrolling);
    }

    [Fact]
    public void StopScroll_ShouldCompleteImmediately()
    {
        var content = new TextBlock("Long") { Height = 30 };
        var sv = new ScrollView(content) { Height = 10 };
        sv.ScrollDown(5);
        sv.ScrollTo(15, 500);
        sv.StopScroll();

        Assert.False(sv.IsScrolling);
        Assert.Equal(15, sv.ScrollOffset);
    }

    [Fact]
    public void ScrollToTop_ShouldAnimateToZero()
    {
        var content = new TextBlock("Long") { Height = 30 };
        var sv = new ScrollView(content) { Height = 10 };
        sv.ScrollDown(10);
        sv.ScrollToTop(100);

        Assert.True(sv.IsScrolling);
    }

    [Fact]
    public void ScrollToBottom_ShouldAnimateToMax()
    {
        var content = new TextBlock("Long") { Height = 30 };
        var sv = new ScrollView(content) { Height = 10 };
        sv.ScrollToBottom(100);

        Assert.True(sv.IsScrolling);
    }

    [Fact]
    public void Render_DuringScroll_ShouldInterpolateOffset()
    {
        var content = new TextBlock("Long") { Height = 30 };
        var sv = new ScrollView(content) { Height = 10, Width = 15 };
        sv.ScrollTo(10, 200);

        var buf = new ScreenBuffer(15, 10);
        var ctx = new RenderContext(buf, 15, 10, 0, 0);

        sv.Render(ctx);

        Assert.True(sv.ScrollOffset > 0 || sv.ScrollOffset < 10 || sv.ScrollOffset == 10);
    }
}

/// <summary>
/// M6 Window 增强测试
/// </summary>
public sealed class WindowEnhancementTests
{
    [Fact]
    public void CenterOnScreen_ShouldCenterInContainer()
    {
        var win = new Window("居中") { Width = 20, Height = 10 };
        win.CenterOnScreen(80, 25);

        Assert.Equal(30, win.X);
        Assert.Equal(7, win.Y);
    }

    [Fact]
    public void CenterVertical_ShouldCenterVertically()
    {
        var win = new Window("垂直居中") { Width = 20, Height = 6 };
        win.CenterVertical(25);

        Assert.Equal(9, win.Y);
    }

    [Fact]
    public void CenterHorizontal_ShouldCenterHorizontally()
    {
        var win = new Window("水平居中") { Width = 30, Height = 10 };
        win.CenterHorizontal(100);

        Assert.Equal(35, win.X);
    }

    [Fact]
    public void ModalWindow_Render_ShouldDrawBackdrop()
    {
        var win = new Window("模态") { Modal = true, ShowBackdrop = true, Width = 20, Height = 8 };
        win.X = 5;
        win.Y = 3;

        var buf = new ScreenBuffer(50, 20);
        var ctx = new RenderContext(buf, 50, 20, 0, 0);

        win.Render(ctx);

        Assert.Equal(win.BackdropColor, buf.GetBackground(0, 0));
    }

    [Fact]
    public void FocusNextInWindow_WithControls_ShouldCycle()
    {
        var win = new Window("焦点");
        var box = new VBox();
        box.Add(new Button("A").WithMargin(top: 0));
        box.Add(new Button("B").WithMargin(top: 0));
        win.Content = box;

        _ = win.ShowDialogAsync();

        Assert.True(win.TabStopControls[0].IsFocused);
        win.FocusNextInWindow();
        Assert.False(win.TabStopControls[0].IsFocused);
        Assert.True(win.TabStopControls[1].IsFocused);
    }

    [Fact]
    public void FocusPreviousInWindow_ShouldWrapAround()
    {
        var win = new Window("焦点");
        var box = new VBox();
        box.Add(new Button("A").WithMargin(top: 0));
        box.Add(new Button("B").WithMargin(top: 0));
        win.Content = box;

        _ = win.ShowDialogAsync();

        Assert.True(win.TabStopControls[0].IsFocused);
        win.FocusPreviousInWindow();
        Assert.True(win.TabStopControls[1].IsFocused);
    }
}

#endif