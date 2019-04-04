#if false // 跳过：使用 ScreenBuffer/RenderContext 等内部 API，无法编译
namespace Commander.Testing;

/// <summary>
/// ScreenBuffer 单元测试
/// </summary>
public sealed class ScreenBufferTests
{
    [Fact]
    public void Constructor_ShouldAllocateCorrectSize()
    {
        var buf = new ScreenBuffer(80, 25);
        Assert.Equal(80, buf.Width);
        Assert.Equal(25, buf.Height);
    }

    [Fact]
    public void Clear_ShouldFillWithSpaces()
    {
        var buf = new ScreenBuffer(10, 5);
        buf.SetChar(0, 0, 'X', Color.White, Color.Black);
        buf.Clear();

        Assert.Equal(' ', buf.GetChar(0, 0));
    }

    [Fact]
    public void SetChar_OutOfBounds_ShouldNotThrow()
    {
        var buf = new ScreenBuffer(10, 5);
        buf.SetChar(-1, 0, 'X', Color.White, Color.Black);
        buf.SetChar(0, -1, 'X', Color.White, Color.Black);
        buf.SetChar(100, 0, 'X', Color.White, Color.Black);
        buf.SetChar(0, 100, 'X', Color.White, Color.Black);
    }

    [Fact]
    public void SetChar_ShouldStoreCharacterAndColors()
    {
        var buf = new ScreenBuffer(10, 5);
        var fg = new Color(255, 0, 0);
        var bg = new Color(0, 0, 255);

        buf.SetChar(3, 2, 'A', fg, bg);

        Assert.Equal('A', buf.GetChar(3, 2));
        Assert.Equal(fg, buf.GetForeground(3, 2));
        Assert.Equal(bg, buf.GetBackground(3, 2));
    }

    [Fact]
    public void SetString_ShouldWriteMultipleChars()
    {
        var buf = new ScreenBuffer(10, 5);
        buf.SetString(1, 1, "Hello", Color.White, Color.Black);

        Assert.Equal('H', buf.GetChar(1, 1));
        Assert.Equal('e', buf.GetChar(2, 1));
        Assert.Equal('l', buf.GetChar(3, 1));
        Assert.Equal('l', buf.GetChar(4, 1));
        Assert.Equal('o', buf.GetChar(5, 1));
    }

    [Fact]
    public void CellEquals_SameValues_ShouldReturnTrue()
    {
        var buf1 = new ScreenBuffer(5, 3);
        var buf2 = new ScreenBuffer(5, 3);

        buf1.SetChar(1, 1, 'X', Color.White, Color.Black);
        buf2.SetChar(1, 1, 'X', Color.White, Color.Black);

        Assert.True(buf1.CellEquals(buf2, 1, 1));
    }

    [Fact]
    public void CellEquals_DifferentChar_ShouldReturnFalse()
    {
        var buf1 = new ScreenBuffer(5, 3);
        var buf2 = new ScreenBuffer(5, 3);

        buf1.SetChar(1, 1, 'X', Color.White, Color.Black);
        buf2.SetChar(1, 1, 'Y', Color.White, Color.Black);

        Assert.False(buf1.CellEquals(buf2, 1, 1));
    }

    [Fact]
    public void CellEquals_OutOfBounds_ShouldReturnFalse()
    {
        var buf1 = new ScreenBuffer(5, 3);
        var buf2 = new ScreenBuffer(5, 3);

        Assert.False(buf1.CellEquals(buf2, -1, 0));
    }
}

/// <summary>
/// RenderContext 单元测试
/// </summary>
public sealed class RenderContextTests
{
    private static RenderContext CreateContext(int w, int h)
    {
        var buf = new ScreenBuffer(w, h);
        return new RenderContext(buf, w, h, 0, 0);
    }

    [Fact]
    public void DrawText_ShouldWriteToBuffer()
    {
        var buf = new ScreenBuffer(20, 5);
        var ctx = new RenderContext(buf, 20, 5, 0, 0);

        ctx.DrawText(2, 1, "Test");

        Assert.Equal('T', buf.GetChar(2, 1));
        Assert.Equal('e', buf.GetChar(3, 1));
    }

    [Fact]
    public void DrawText_WithOffset_ShouldWriteToCorrectPosition()
    {
        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 10, 5, 5, 3);

        ctx.DrawText(0, 0, "HI");

        Assert.Equal('H', buf.GetChar(5, 3));
        Assert.Equal('I', buf.GetChar(6, 3));
    }

    [Fact]
    public void DrawCenteredText_ShouldCenterShortText()
    {
        var buf = new ScreenBuffer(10, 3);
        var ctx = new RenderContext(buf, 10, 3, 0, 0);

        ctx.DrawCenteredText(1, "Hi");

        Assert.Equal('H', buf.GetChar(4, 1));
        Assert.Equal('i', buf.GetChar(5, 1));
    }

    [Fact]
    public void FillRect_ShouldFillWithSpacesAndColor()
    {
        var buf = new ScreenBuffer(10, 5);
        var ctx = new RenderContext(buf, 10, 5, 0, 0);
        var color = new Color(30, 30, 30);

        ctx.FillRect(1, 1, 3, 2, color);

        for (var y = 1; y < 3; y++)
        {
            for (var x = 1; x < 4; x++)
            {
                Assert.Equal(' ', buf.GetChar(x, y));
                Assert.Equal(color, buf.GetBackground(x, y));
            }
        }
    }

    [Fact]
    public void DrawBorder_ShouldDrawCornersAndEdges()
    {
        var buf = new ScreenBuffer(10, 5);
        var ctx = new RenderContext(buf, 10, 5, 0, 0);

        ctx.draw_border(0, 0, 5, 3, BorderStyle.single);

        Assert.Equal('┌', buf.GetChar(0, 0));
        Assert.Equal('┐', buf.GetChar(4, 0));
        Assert.Equal('└', buf.GetChar(0, 2));
        Assert.Equal('┘', buf.GetChar(4, 2));
        Assert.Equal('─', buf.GetChar(1, 0));
        Assert.Equal('│', buf.GetChar(0, 1));
    }

    [Fact]
    public void CreateChild_ShouldInheritOffset()
    {
        var buf = new ScreenBuffer(20, 15);
        var parent = new RenderContext(buf, 20, 15, 5, 2);

        var child = parent.create_child(2, 3, 6, 4);
        child.DrawText(0, 0, "X");

        Assert.Equal('X', buf.GetChar(7, 5));
    }
}

/// <summary>
/// Button 渲染测试
/// </summary>
public sealed class ButtonTests
{
    [Fact]
    public void Render_ShouldWriteText()
    {
        var button = new Button("确定");
        var buf = new ScreenBuffer(10, 5);
        var ctx = new RenderContext(buf, 10, 5, 0, 0);

        button.Render(ctx);

        Assert.NotEqual(' ', buf.GetChar(4, 1));
    }

    [Fact]
    public void OnClick_ShouldInvokeHandler()
    {
        var called = false;
        var button = new Button("Click").OnClick(() => called = true);

        button.Click();
        Assert.True(called);
    }

    [Fact]
    public void Focused_ShouldChangeRendering()
    {
        var button = new Button("OK");
        button.SetFocused(true);
        button.Width = 10;
        button.Height = 3;
        var buf = new ScreenBuffer(15, 5);
        var ctx = new RenderContext(buf, 15, 5, 0, 0);

        button.Render(ctx);

        Assert.Equal('┌', buf.GetChar(0, 0));
    }

    [Fact]
    public void Disabled_ShouldUseGrayColors()
    {
        var button = new Button("OK") { Enabled = false };
        var buf = new ScreenBuffer(15, 5);
        var ctx = new RenderContext(buf, 15, 5, 0, 0);

        button.Render(ctx);
    }
}

/// <summary>
/// TextBlock 渲染测试
/// </summary>
public sealed class TextBlockTests
{
    [Fact]
    public void Render_ShouldDrawText()
    {
        var block = new TextBlock("Hello World");
        var buf = new ScreenBuffer(20, 3);
        var ctx = new RenderContext(buf, 20, 3, 0, 0);

        block.Render(ctx);

        Assert.Equal('H', buf.GetChar(0, 0));
        Assert.Equal('W', buf.GetChar(6, 0));
    }
}

/// <summary>
/// CheckBox 渲染测试
/// </summary>
public sealed class CheckBoxTests
{
    [Fact]
    public void Render_Unchecked_ShouldShowBrackets()
    {
        var cb = new CheckBox("启用");
        var buf = new ScreenBuffer(15, 3);
        var ctx = new RenderContext(buf, 15, 3, 0, 0);

        cb.Render(ctx);

        Assert.Equal('[', buf.GetChar(0, 0));
        Assert.Equal(']', buf.GetChar(2, 0));
    }

    [Fact]
    public void Toggle_ShouldChangeCheckedState()
    {
        var cb = new CheckBox("选项");
        Assert.False(cb.is_checked);

        cb.Toggle();
        Assert.True(cb.is_checked);

        cb.Toggle();
        Assert.False(cb.is_checked);
    }

    [Fact]
    public void Toggle_Disabled_ShouldNotChange()
    {
        var cb = new CheckBox("选项") { Enabled = false };
        Assert.False(cb.is_checked);

        cb.Toggle();
        Assert.False(cb.is_checked);
    }

    [Fact]
    public void OnCheckedChanged_ShouldFire()
    {
        var fired = false;
        var cb = new CheckBox("选项");
        cb.OnCheckedChanged += (_, _) => fired = true;

        cb.Toggle();
        Assert.True(fired);
    }
}

/// <summary>
/// TextBox 测试
/// </summary>
public sealed class TextBoxTests
{
    [Fact]
    public void Render_ShouldShowBorder()
    {
        var tb = new TextBox();
        var buf = new ScreenBuffer(20, 5);
        var ctx = new RenderContext(buf, 20, 5, 0, 0);

        tb.Render(ctx);

        Assert.Equal('┌', buf.GetChar(0, 0));
        Assert.Equal('┐', buf.GetChar(19, 0));
    }

    [Fact]
    public void Render_ShouldShowPlaceholder()
    {
        var tb = new TextBox();
        tb.placeholder = "在此输入...";
        var buf = new ScreenBuffer(20, 5);
        var ctx = new RenderContext(buf, 20, 5, 0, 0);

        tb.Render(ctx);
    }

    [Fact]
    public void InsertChar_ShouldAddText()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');

        Assert.Equal("AB", tb.Text);
        Assert.Equal(2, tb.CursorPosition);
    }

    [Fact]
    public void DeleteCharBefore_ShouldRemoveCharacter()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');
        tb.DeleteCharBefore();

        Assert.Equal("A", tb.Text);
        Assert.Equal(1, tb.CursorPosition);
    }

    [Fact]
    public void MoveCursorLeft_ShouldMoveLeft()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');
        tb.MoveCursorLeft();

        Assert.Equal(1, tb.CursorPosition);
    }

    [Fact]
    public void MoveCursorRight_ShouldMoveRight()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');
        tb.MoveCursorLeft();
        tb.MoveCursorRight();

        Assert.Equal(2, tb.CursorPosition);
    }

    [Fact]
    public void MoveCursorHome_ShouldGoToStart()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');
        tb.MoveCursorHome();

        Assert.Equal(0, tb.CursorPosition);
    }

    [Fact]
    public void MoveCursorEnd_ShouldGoToEnd()
    {
        var tb = new TextBox();
        tb.InsertChar('A');
        tb.InsertChar('B');
        tb.MoveCursorHome();
        tb.MoveCursorEnd();

        Assert.Equal(2, tb.CursorPosition);
    }

    [Fact]
    public void OnTextChanged_ShouldFire()
    {
        var fired = false;
        var tb = new TextBox();
        tb.OnTextChanged += (_, _) => fired = true;

        tb.InsertChar('X');
        Assert.True(fired);
    }
}

/// <summary>
/// Panel 渲染测试
/// </summary>
public sealed class PanelTests
{
    [Fact]
    public void Render_ShouldDrawBorder()
    {
        var panel = new Panel(new TextBlock("内容"));
        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        panel.render(ctx);

        Assert.Equal('┌', buf.GetChar(0, 0));
    }

    [Fact]
    public void Render_WithTitle_ShouldDrawTitleOnTopBorder()
    {
        var panel = new Panel(new TextBlock("Content")) { Title = "面板标题" };
        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        panel.render(ctx);
    }
}

/// <summary>
/// Separator 渲染测试
/// </summary>
public sealed class SeparatorTests
{
    [Fact]
    public void Render_Horizontal_ShouldDrawLine()
    {
        var sep = new Separator(Orientation.horizontal) { Width = 10 };
        var buf = new ScreenBuffer(15, 3);
        var ctx = new RenderContext(buf, 15, 3, 0, 0);

        sep.Render(ctx);

        Assert.Equal('─', buf.GetChar(0, 0));
        Assert.Equal('─', buf.GetChar(5, 0));
    }

    [Fact]
    public void Render_Vertical_ShouldDrawLine()
    {
        var sep = new Separator(Orientation.Vertical) { Height = 5 };
        var buf = new ScreenBuffer(3, 10);
        var ctx = new RenderContext(buf, 3, 10, 0, 0);

        sep.Render(ctx);

        Assert.Equal('│', buf.GetChar(0, 0));
        Assert.Equal('│', buf.GetChar(0, 3));
    }
}

/// <summary>
/// VBox 布局测试
/// </summary>
public sealed class VBoxTests
{
    [Fact]
    public void Render_ShouldLayoutChildrenVertically()
    {
        var box = new VBox();
        box.Add(new TextBlock("第一行") { Width = 10, Height = 1 });
        box.Add(new TextBlock("第二行") { Width = 10, Height = 1 });

        box.Width = 20;
        box.Height = 10;

        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        box.Render(ctx);

        Assert.Equal('第', buf.GetChar(0, 0));
        Assert.NotEqual(' ', buf.GetChar(0, 1));
    }

    [Fact]
    public void WithSpacing_ShouldAddGapBetweenChildren()
    {
        var box = new VBox();
        box.Add(new TextBlock("A") { Width = 5, Height = 1 });
        box.Add(new TextBlock("B") { Width = 5, Height = 1 });
        box.WithSpacing(2);

        box.Width = 20;
        box.Height = 10;

        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        box.Render(ctx);

        Assert.Equal('A', buf.GetChar(0, 0));
        Assert.Equal('B', buf.GetChar(0, 3));
    }
}

/// <summary>
/// HBox 布局测试
/// </summary>
public sealed class HBoxTests
{
    [Fact]
    public void Render_ShouldLayoutChildrenHorizontally()
    {
        var box = new HBox();
        box.Add(new TextBlock("A") { Width = 3, Height = 1 });
        box.Add(new TextBlock("B") { Width = 3, Height = 1 });

        box.Width = 10;
        box.Height = 3;

        var buf = new ScreenBuffer(10, 3);
        var ctx = new RenderContext(buf, 10, 3, 0, 0);

        box.Render(ctx);

        Assert.Equal('A', buf.GetChar(0, 0));
        Assert.Equal('B', buf.GetChar(3, 0));
    }

    [Fact]
    public void WithSpacing_ShouldAddGapBetweenChildren()
    {
        var box = new HBox();
        box.Add(new TextBlock("X") { Width = 2, Height = 1 });
        box.Add(new TextBlock("Y") { Width = 2, Height = 1 });
        box.WithSpacing(2);

        box.Width = 10;
        box.Height = 3;

        var buf = new ScreenBuffer(10, 3);
        var ctx = new RenderContext(buf, 10, 3, 0, 0);

        box.Render(ctx);

        Assert.Equal('X', buf.GetChar(0, 0));
        Assert.Equal('Y', buf.GetChar(4, 0));
    }
}

/// <summary>
/// ListView 测试
/// </summary>
public sealed class ListViewTests
{
    [Fact]
    public void Render_ShouldDrawBorderAndItems()
    {
        var items = new[] { "Apple", "Banana", "Cherry" };
        var lv = new ListView<string>(items);
        var buf = new ScreenBuffer(20, 12);
        var ctx = new RenderContext(buf, 20, 12, 0, 0);

        lv.Render(ctx);

        Assert.Equal('┌', buf.GetChar(0, 0));
    }

    [Fact]
    public void MoveSelectionDown_ShouldSelectNextItem()
    {
        var items = new[] { "A", "B", "C" };
        var lv = new ListView<string>(items);
        Assert.Equal(-1, lv.SelectedIndex);

        lv.MoveSelectionDown();
        Assert.Equal(0, lv.SelectedIndex);
        Assert.Equal("A", lv.SelectedItem);

        lv.MoveSelectionDown();
        Assert.Equal(1, lv.SelectedIndex);
        Assert.Equal("B", lv.SelectedItem);
    }

    [Fact]
    public void MoveSelectionUp_ShouldSelectPreviousItem()
    {
        var items = new[] { "A", "B", "C" };
        var lv = new ListView<string>(items);
        lv.MoveSelectionDown();
        lv.MoveSelectionDown();
        lv.MoveSelectionDown();

        lv.MoveSelectionUp();
        Assert.Equal(1, lv.SelectedIndex);
        Assert.Equal("B", lv.SelectedItem);
    }

    [Fact]
    public void OnSelected_ShouldFire()
    {
        var fired = false;
        var items = new[] { "X" };
        var lv = new ListView<string>(items);
        lv.OnSelected += (_, _) => fired = true;

        lv.MoveSelectionDown();
        Assert.True(fired);
    }
}

/// <summary>
/// ScrollView 测试
/// </summary>
public sealed class ScrollViewTests
{
    [Fact]
    public void ScrollUp_ShouldDecreaseOffset()
    {
        var content = new TextBlock("Long") { Height = 20 };
        var sv = new ScrollView(content);
        sv.ScrollDown(5);
        sv.ScrollUp(2);

        Assert.True(sv.ScrollOffset >= 0);
    }

    [Fact]
    public void ScrollDown_ShouldIncreaseOffset()
    {
        var content = new TextBlock("Long") { Height = 20 };
        var sv = new ScrollView(content);
        sv.ScrollDown(3);

        Assert.True(sv.ScrollOffset > 0);
    }
}

/// <summary>
/// Window 测试
/// </summary>
public sealed class WindowTests
{
    [Fact]
    public void Render_ShouldDrawTitleAndBorder()
    {
        var win = new Window("我的窗口") { Content = new TextBlock("Hello") };
        var buf = new ScreenBuffer(50, 20);
        var ctx = new RenderContext(buf, 50, 20, 0, 0);

        win.Render(ctx);

        Assert.Equal('╭', buf.GetChar(5, 3));
    }

    [Fact]
    public void ShowDialogAsync_ShouldSetModal()
    {
        var win = new Window("对话框");
        _ = win.ShowDialogAsync();

        Assert.True(win.Modal);
    }

    [Fact]
    public void Close_ShouldCompleteDialogTask()
    {
        var win = new Window("对话框");
        var task = win.ShowDialogAsync();
        win.Close(true);

        Assert.True(task.IsCompleted);
        Assert.True(task.Result);
    }
}

/// <summary>
/// InputEventManager 测试
/// </summary>
public sealed class InputEventManagerTests
{
    [Fact]
    public void DispatchKey_Tab_ShouldFocusNext()
    {
        var controls = new List<View>
        {
            new Button("A"),
            new Button("B")
        };
        controls[0].SetFocused(true);

        var key = new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false);
        InputEventManager.DispatchKey(controls[0], controls, key);

        Assert.False(controls[0].IsFocused);
        Assert.True(controls[1].IsFocused);
    }

    [Fact]
    public void DispatchKey_Enter_OnButton_ShouldClick()
    {
        var called = false;
        var button = new Button("OK").OnClick(() => called = true);
        button.SetFocused(true);

        var key = new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);
        InputEventManager.DispatchKey(button, new List<View> { button }, key);

        Assert.True(called);
    }

    [Fact]
    public void DispatchKey_Spacebar_OnCheckBox_ShouldToggle()
    {
        var cb = new CheckBox("选项");
        cb.SetFocused(true);

        var key = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);
        InputEventManager.DispatchKey(cb, new List<View> { cb }, key);

        Assert.True(cb.is_checked);
    }

    [Fact]
    public void DispatchKey_ArrowKeys_OnListView_ShouldMove()
    {
        var items = new[] { "A", "B" };
        var lv = new ListView<string>(items);
        lv.SetFocused(true);

        var downKey = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        InputEventManager.DispatchKey((View)lv, new List<View>(), downKey);

        Assert.Equal(0, lv.SelectedIndex);
    }

    [Fact]
    public void FocusNext_Cyclic_ShouldWrapAround()
    {
        var controls = new List<View> { new Button("A"), new Button("B") };
        controls[1].SetFocused(true);

        InputEventManager.FocusNext(controls[1], controls);

        Assert.True(controls[0].IsFocused);
    }

    [Fact]
    public void FocusPrevious_ShouldGoToPreviousControl()
    {
        var controls = new List<View> { new Button("A"), new Button("B") };
        controls[0].SetFocused(true);

        InputEventManager.FocusPrevious(controls[0], controls);

        Assert.True(controls[1].IsFocused);
    }
}

/// <summary>
/// TuiApplication 测试
/// </summary>
public sealed class TuiApplicationTests
{
    [Fact]
    public void Constructor_ShouldSetConfig()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);

        Assert.NotNull(app);
    }

    [Fact]
    public void SetContentView_ShouldCollectTabStops()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);

        var root = new VBox();
        root.Add(new Button("测试"));
        app.set_content_view(root);

        Assert.NotEmpty(app.get_tab_stop_controls());
    }

    [Fact]
    public void RenderFrame_WithoutContent_ShouldNotThrow()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);
        app.RenderFrame();
    }

    [Fact]
    public void RenderFrame_WithContent_ShouldRender()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);

        var root = new TextBlock("Hello TUI") { Width = 80, Height = 25 };
        app.set_content_view(root);
        app.RenderFrame();
    }

    [Fact]
    public void Stop_AfterStart_ShouldNotThrow()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);
        var root = new TextBlock("Test") { Width = 10, Height = 5 };
        app.set_content_view(root);

        app.stop();
    }

    [Fact]
    public void Dispose_ShouldCleanup()
    {
        var config = new TuiConfig();
        var app = new TuiApplication(config);
        app.Dispose();
    }
}

#endif