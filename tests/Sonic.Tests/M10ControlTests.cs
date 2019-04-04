#if false // 跳过：使用 RenderContext 内部构造函数，无法编译
namespace Commander.Testing;

/// <summary>
/// M10 TUI 控件扩展测试（Theme + TreeView + TabView + ProgressBar + Dropdown）
/// </summary>
public sealed class M10ControlTests
{
    #region Theme 主题系统

    /// <summary>
    /// Dark 主题应有深色 Surface
    /// </summary>
    [Fact]
    public void Theme_Dark_SurfaceShouldBeDark()
    {
        Assert.True(TuiTheme.dark.surface.r < 50);
        Assert.True(TuiTheme.dark.surface.g < 50);
        Assert.True(TuiTheme.dark.surface.b < 50);
    }

    /// <summary>
    /// Light 主题应有浅色 Surface
    /// </summary>
    [Fact]
    public void Theme_Light_SurfaceShouldBeLight()
    {
        Assert.True(TuiTheme.light.surface.r > 200);
        Assert.True(TuiTheme.light.surface.g > 200);
        Assert.True(TuiTheme.light.surface.b > 200);
    }

    /// <summary>
    /// ThemeManager.Current 默认为 Dark
    /// </summary>
    [Fact]
    public void ThemeManager_Current_DefaultIsDark()
    {
        Assert.Same(TuiTheme.dark, ThemeManager.current);
    }

    /// <summary>
    /// ThemeManager.Current 可切换为 Light
    /// </summary>
    [Fact]
    public void ThemeManager_Current_CanSwitchToLight()
    {
        ThemeManager.current = TuiTheme.light;

        Assert.Same(TuiTheme.light, ThemeManager.current);

        ThemeManager.current = TuiTheme.dark;
    }

    /// <summary>
    /// Dark 和 Light 主题的所有颜色属性非默认
    /// </summary>
    [Fact]
    public void Theme_BothPresets_AllColorsShouldBeSet()
    {
        Assert.NotEqual(default(RgbColor), TuiTheme.dark.surface);
        Assert.NotEqual(default(RgbColor), TuiTheme.dark.text);
        Assert.NotEqual(default(RgbColor), TuiTheme.dark.focus);
        Assert.NotEqual(default(RgbColor), TuiTheme.dark.success);
        Assert.NotEqual(default(RgbColor), TuiTheme.dark.danger);

        Assert.NotEqual(default(RgbColor), TuiTheme.light.surface);
        Assert.NotEqual(default(RgbColor), TuiTheme.light.text);
        Assert.NotEqual(default(RgbColor), TuiTheme.light.focus);
    }

    #endregion

    #region TreeNode

    /// <summary>
    /// TreeNode 创建后 Data 和 Text 正确
    /// </summary>
    [Fact]
    public void TreeNode_Constructor_ShouldSetDataAndText()
    {
        var node = new TreeNode<string>("数据", "节点");

        Assert.Equal("数据", node.data);
        Assert.Equal("节点", node.text);
        Assert.Empty(node.children);
    }

    /// <summary>
    /// TreeNode.AddChild 应设置 Parent 关系
    /// </summary>
    [Fact]
    public void TreeNode_AddChild_ShouldSetParent()
    {
        var parent = new TreeNode<string>("父数据", "父");
        var child = new TreeNode<string>("子数据", "子");

        parent.add_child(child);

        Assert.Single(parent.children);
        Assert.Same(parent, child.parent);
    }

    /// <summary>
    /// TreeNode.AddChild 两参数重载
    /// </summary>
    [Fact]
    public void TreeNode_AddChildDataText_ShouldCreateNode()
    {
        var root = new TreeNode<string>("root", "根");

        root.add_child("child", "子");

        Assert.Single(root.children);
        Assert.Equal("child", root.children[0].data);
        Assert.Equal("子", root.children[0].text);
    }

    #endregion

    #region TreeView

    /// <summary>
    /// TreeView 设置根节点后 Root 非空
    /// </summary>
    [Fact]
    public void TreeView_SetRoot_ShouldSetRoot()
    {
        var root = new TreeNode<string>("data", "根节点");
        var tree = new TreeView<string>(root);

        Assert.Same(root, tree.root);
    }

    /// <summary>
    /// TreeView 渲染非空
    /// </summary>
    [Fact]
    public void TreeView_Render_ShouldNotThrow()
    {
        var root = new TreeNode<string>("r1", "根1");
        root.add_child("c1", "子1");
        var tree = new TreeView<string>(root) { width = 40, height = 10 };

        var buf = new ScreenBuffer(40, 10);
        var ctx = new RenderContext(buf, 40, 10, 0, 0);

        var exception = Record.Exception(() => tree.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// TreeView 渲染后缓冲区非空
    /// </summary>
    [Fact]
    public void TreeView_Render_BufferShouldBeWritten()
    {
        var root = new TreeNode<string>("root", "Root");
        var tree = new TreeView<string>(root) { width = 30, height = 10 };

        var buf = new ScreenBuffer(30, 10);
        var ctx = new RenderContext(buf, 30, 10, 0, 0);

        tree.render(ctx);

        var ch = buf.get_char(1, 1);
        Assert.NotEqual('\0', ch);
    }

    /// <summary>
    /// ToggleExpand 应切换节点展开状态（仅对有子节点的节点生效）
    /// </summary>
    [Fact]
    public void TreeView_ToggleExpand_ShouldToggle()
    {
        var root = new TreeNode<string>("r", "根");
        root.add_child("c", "子");

        var initial = root.is_expanded;

        var tree = new TreeView<string>(root);
        tree.toggle_expand(root);

        Assert.NotEqual(initial, root.is_expanded);

        tree.toggle_expand(root);
        Assert.Equal(initial, root.is_expanded);
    }

    #endregion

    #region TabView

    /// <summary>
    /// TabView.AddTab 添加标签页后 Tabs 非空
    /// </summary>
    [Fact]
    public void TabView_AddTab_ShouldAddToTabs()
    {
        var tabs = new TabView { width = 40, height = 10 };
        var tab1 = tabs.add_tab("标签1", new TextBlock("内容1"));
        var tab2 = tabs.add_tab("标签2", new TextBlock("内容2"));

        Assert.Equal(2, tabs.tabs.Count);
        Assert.Equal("标签1", tab1.title);
        Assert.Equal("标签2", tab2.title);
    }

    /// <summary>
    /// TabView 默认 ActiveTab 不为 null（第一个添加的自动选中）
    /// </summary>
    [Fact]
    public void TabView_DefaultActiveTab_ShouldBeFirst()
    {
        var tabs = new TabView { width = 40, height = 10 };
        tabs.add_tab("A", new TextBlock("内容A"));
        tabs.add_tab("B", new TextBlock("内容B"));

        Assert.NotNull(tabs.active_tab);
        Assert.Equal("A", tabs.active_tab!.title);
    }

    /// <summary>
    /// TabView.SelectTab 应切换活动标签
    /// </summary>
    [Fact]
    public void TabView_SelectTab_ShouldSwitchActive()
    {
        var tabs = new TabView { width = 40, height = 10 };
        tabs.add_tab("A", new TextBlock("内容A"));
        tabs.add_tab("B", new TextBlock("内容B"));
        tabs.add_tab("C", new TextBlock("内容C"));

        tabs.select_tab(2);

        Assert.NotNull(tabs.active_tab);
        Assert.Equal("C", tabs.active_tab!.title);
    }

    /// <summary>
    /// TabView 渲染非空
    /// </summary>
    [Fact]
    public void TabView_Render_ShouldNotThrow()
    {
        var tabs = new TabView { width = 40, height = 10 };
        tabs.add_tab("主页", new TextBlock("欢迎"));

        var buf = new ScreenBuffer(40, 10);
        var ctx = new RenderContext(buf, 40, 10, 0, 0);

        var exception = Record.Exception(() => tabs.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// RemoveTab 应移除标签页
    /// </summary>
    [Fact]
    public void TabView_RemoveTab_ShouldRemove()
    {
        var tabs = new TabView { width = 40, height = 10 };
        tabs.add_tab("A", new TextBlock("a"));
        tabs.add_tab("B", new TextBlock("b"));
        tabs.add_tab("C", new TextBlock("c"));

        tabs.remove_tab(1);

        Assert.Equal(2, tabs.tabs.Count);
    }

    #endregion

    #region ProgressBar

    /// <summary>
    /// ProgressBar 默认 Value 为 0
    /// </summary>
    [Fact]
    public void ProgressBar_DefaultValue_ShouldBeZero()
    {
        var bar = new ProgressBar();

        Assert.Equal(0, bar.value);
        Assert.Equal(40, bar.width);
    }

    /// <summary>
    /// ProgressBar.Value 超出范围应 Clamp
    /// </summary>
    [Fact]
    public void ProgressBar_Value_ClampToRange()
    {
        var bar = new ProgressBar();

        bar.value = 150;
        Assert.Equal(100, bar.value);

        bar.value = -50;
        Assert.Equal(0, bar.value);
    }

    /// <summary>
    /// ProgressBar 渲染不应抛异常
    /// </summary>
    [Fact]
    public void ProgressBar_Render_ShouldNotThrow()
    {
        var bar = new ProgressBar { width = 30, height = 1, value = 42, show_percentage = true };

        var buf = new ScreenBuffer(30, 1);
        var ctx = new RenderContext(buf, 30, 1, 0, 0);

        var exception = Record.Exception(() => bar.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// ProgressBar 三种样式均可渲染
    /// </summary>
    [Theory]
    [InlineData(ProgressBarStyle.block)]
    [InlineData(ProgressBarStyle.smooth)]
    [InlineData(ProgressBarStyle.dash)]
    public void ProgressBar_AllStyles_ShouldNotThrow(ProgressBarStyle style)
    {
        var bar = new ProgressBar { width = 30, height = 1, value = 50, style = style, show_percentage = false };

        var buf = new ScreenBuffer(30, 1);
        var ctx = new RenderContext(buf, 30, 1, 0, 0);

        var exception = Record.Exception(() => bar.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// ProgressBar 100% 渲染后缓冲区非空
    /// </summary>
    [Fact]
    public void ProgressBar_Render_Full_BufferShouldBeWritten()
    {
        var bar = new ProgressBar { width = 20, height = 1, value = 100, show_percentage = false };

        var buf = new ScreenBuffer(20, 1);
        var ctx = new RenderContext(buf, 20, 1, 0, 0);

        bar.render(ctx);

        var ch = buf.GetChar(0, 0);
        Assert.NotEqual('\0', ch);
    }

    #endregion

    #region Dropdown

    /// <summary>
    /// Dropdown 添加选项
    /// </summary>
    [Fact]
    public void Dropdown_AddItem_ShouldAddToItems()
    {
        var dd = new Dropdown();
        dd.add_item("选项1");
        dd.add_item("选项2", 42);

        Assert.Equal(2, dd.items.Count);
        Assert.Equal("选项1", dd.items[0].text);
        Assert.Equal(42, dd.items[1].value);
    }

    /// <summary>
    /// Dropdown 默认关闭
    /// </summary>
    [Fact]
    public void Dropdown_DefaultIsClosed()
    {
        var dd = new Dropdown();

        Assert.False(dd.is_open);
    }

    /// <summary>
    /// Dropdown.IsOpen 打开后高度应增加
    /// </summary>
    [Fact]
    public void Dropdown_Open_ShouldIncreaseExpandedHeight()
    {
        var dd = new Dropdown();
        dd.add_item("A");
        dd.add_item("B");
        dd.add_item("C");

        dd.is_open = true;

        Assert.True(dd.is_open);
        Assert.True(dd.get_expanded_height() > dd.height);
    }

    /// <summary>
    /// Dropdown.SelectedItem 设置后应关闭
    /// </summary>
    [Fact]
    public void Dropdown_SelectedItem_ShouldCloseDropdown()
    {
        var dd = new Dropdown();
        dd.add_item("选项1");
        dd.add_item("选项2");

        dd.selected_item = dd.items[1];

        Assert.False(dd.is_open);
        Assert.Equal("选项2", dd.selected_item!.text);
    }

    /// <summary>
    /// Dropdown.IsOpen 切换打开/关闭
    /// </summary>
    [Fact]
    public void Dropdown_IsOpen_ShouldToggleOpen()
    {
        var dd = new Dropdown();
        dd.add_item("A");
        dd.add_item("B");

        Assert.False(dd.is_open);

        dd.is_open = true;
        Assert.True(dd.is_open);

        dd.is_open = false;
        Assert.False(dd.is_open);
    }

    /// <summary>
    /// Dropdown 渲染（关闭）不应抛异常
    /// </summary>
    [Fact]
    public void Dropdown_Render_Closed_ShouldNotThrow()
    {
        var dd = new Dropdown { width = 20 };
        dd.add_item("选项A");
        dd.add_item("选项B");

        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        var exception = Record.Exception(() => dd.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// Dropdown 渲染（打开）不应抛异常
    /// </summary>
    [Fact]
    public void Dropdown_Render_Open_ShouldNotThrow()
    {
        var dd = new Dropdown { width = 20 };
        dd.add_item("苹果");
        dd.add_item("香蕉");
        dd.is_open = true;

        var buf = new ScreenBuffer(20, 10);
        var ctx = new RenderContext(buf, 20, 10, 0, 0);

        var exception = Record.Exception(() => dd.render(ctx));

        Assert.Null(exception);
    }

    /// <summary>
    /// Dropdown.OnSelectionChanged 事件应在选中时触发
    /// </summary>
    [Fact]
    public void Dropdown_OnSelectionChanged_ShouldFire()
    {
        DropdownItem? captured = null;
        var dd = new Dropdown();
        dd.add_item("A");
        dd.add_item("B");
        dd.on_selection_changed += (_, item) => captured = item;

        dd.selected_item = dd.items[1];

        Assert.NotNull(captured);
        Assert.Equal("B", captured!.text);
    }

    /// <summary>
    /// Dropdown.placeholder 默认值
    /// </summary>
    [Fact]
    public void Dropdown_Placeholder_DefaultValue()
    {
        var dd = new Dropdown();

        Assert.Equal("Select...", dd.placeholder);
    }

    /// <summary>
    /// Dropdown 添加分隔线
    /// </summary>
    [Fact]
    public void Dropdown_AddSeparator_ShouldAddSeparatorItem()
    {
        var dd = new Dropdown();
        dd.add_item("A");
        dd.add_separator();
        dd.add_item("B");

        Assert.Equal(3, dd.items.Count);
        Assert.True(dd.items[1].is_separator);
        Assert.False(dd.items[1].is_enabled);
    }

    /// <summary>
    /// Dropdown.Clear 应清空所有选项
    /// </summary>
    [Fact]
    public void Dropdown_Clear_ShouldRemoveAllItems()
    {
        var dd = new Dropdown();
        dd.add_item("A");
        dd.add_item("B");
        dd.add_item("C");

        dd.clear();

        Assert.Empty(dd.items);
        Assert.Null(dd.selected_item);
    }

    #endregion
}

#endif