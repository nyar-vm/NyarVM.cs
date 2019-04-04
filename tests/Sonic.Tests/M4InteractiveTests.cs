namespace Commander.Testing;

/// <summary>
///     InteractivePrompt 交互式提示测试
/// </summary>
public sealed class InteractivePromptTests
{
    [Fact]
    public async Task AskAsync_WhenInputRedirected_ShouldReturnDefault()
    {
        var result = await InteractivePrompt.ask("姓名");
        Assert.Null(result);
    }

    [Fact]
    public async Task AskAsync_Typed_WhenInputRedirected_ShouldReturnDefault()
    {
        var result = await InteractivePrompt.ask<int>("年龄");
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task AskAsync_Typed_WithDefault_WhenInputRedirected_ShouldReturnDefault()
    {
        var result = await InteractivePrompt.ask("分数", 42);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task PasswordAsync_WhenInputRedirected_ShouldReturnNull()
    {
        var result = await InteractivePrompt.password("密码");
        Assert.Null(result);
    }

    [Fact]
    public async Task ConfirmAsync_WhenInputRedirected_ShouldReturnDefault()
    {
        var result = await InteractivePrompt.confirm("确认?");
        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmAsync_DefaultFalse_WhenInputRedirected_ShouldReturnFalse()
    {
        var result = await InteractivePrompt.confirm("确认?", false);
        Assert.False(result);
    }

    [Fact]
    public async Task SelectAsync_WhenInputRedirected_ShouldReturnNull()
    {
        var items = new List<string> { "A", "B", "C" };
        var result = await InteractivePrompt.select("选择", items);
        Assert.Null(result);
    }

    [Fact]
    public async Task SelectAsync_EmptyItems_ShouldReturnNull()
    {
        var result = await InteractivePrompt.select("选择", Array.Empty<string>());
        Assert.Null(result);
    }

    [Fact]
    public async Task MultiSelectAsync_WhenInputRedirected_ShouldReturnEmpty()
    {
        var items = new List<string> { "X", "Y", "Z" };
        var result = await InteractivePrompt.multi_select("多选", items);
        Assert.Empty(result);
    }

    [Fact]
    public async Task MultiSelectAsync_EmptyItems_ShouldReturnEmpty()
    {
        var result = await InteractivePrompt.multi_select("多选", Array.Empty<string>());
        Assert.Empty(result);
    }

    [Fact]
    public async Task FuzzyFinderAsync_WhenInputRedirected_ShouldReturnNull()
    {
        var items = new[] { "apple", "banana", "cherry" };
        var result = await InteractivePrompt.fuzzy_finder("搜索", items);
        Assert.Null(result);
    }

    [Fact]
    public async Task FuzzyFinderAsync_EmptyItems_ShouldReturnNull()
    {
        var result = await InteractivePrompt.fuzzy_finder("搜索", Array.Empty<string>());
        Assert.Null(result);
    }

    [Fact]
    public void FuzzyMatch_ExactMatch_ShouldReturnItem()
    {
        var items = new List<string> { "hello", "world", "help" };
        var result = InteractivePrompt.fuzzy_match(items, "hello", 3);
        Assert.Single(result);
        Assert.Equal("hello", result[0]);
    }

    [Fact]
    public void FuzzyMatch_PartialMatch_ShouldReturnMultiple()
    {
        var items = new List<string> { "hello", "world", "help", "held" };
        var result = InteractivePrompt.fuzzy_match(items, "hel", 3);
        Assert.Equal(3, result.Count);
        Assert.Contains("hello", result);
        Assert.Contains("help", result);
    }

    [Fact]
    public void FuzzyMatch_NoMatch_ShouldReturnEmpty()
    {
        var items = new List<string> { "abc", "def", "ghi" };
        var result = InteractivePrompt.fuzzy_match(items, "xyz", 3);
        Assert.Empty(result);
    }

    [Fact]
    public void FuzzyMatch_ScoreRanking_ExactPrefixFirst()
    {
        var items = new List<string> { "application", "apple", "apricot", "banana" };
        var result = InteractivePrompt.fuzzy_match(items, "app", 2);
        Assert.Equal(2, result.Count);
        Assert.Contains("apple", result);
    }
}

/// <summary>
///     LiveTable 动态表格测试
/// </summary>
public sealed class LiveTableTests
{
    [Fact]
    public void AddColumn_ShouldReturnSameInstance()
    {
        var table = new LiveTable();
        var result = table.add_column("名称");
        Assert.Same(table, result);
    }

    [Fact]
    public void AddRow_ShouldReturnTableRow()
    {
        var table = new LiveTable();
        var row = table.add_row(new[] { "Alice", "30" });
        Assert.NotNull(row);
        Assert.Equal("Alice", row.cells[0]);
    }

    [Fact]
    public void Render_NoColumns_ShouldReturnEmpty()
    {
        var table = new LiveTable();
        var result = table.render();
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Render_WithColumnsAndRows_ShouldReturnNonEmpty()
    {
        var table = new LiveTable();
        table.add_column("名称", 10);
        table.add_column("年龄", 8);
        table.add_row(new[] { "Alice", "30" });
        table.add_row(new[] { "Bob", "25" });

        var result = table.render();
        Assert.NotEmpty(result);
        Assert.Contains("Alice", result, StringComparison.Ordinal);
        Assert.Contains("Bob", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ShouldIncludeHeader()
    {
        var table = new LiveTable();
        table.add_column("名称", 10);
        table.add_column("年龄", 8);

        var result = table.render();
        Assert.Contains("名称", result, StringComparison.Ordinal);
        Assert.Contains("年龄", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ShouldIncludeBorders()
    {
        var table = new LiveTable();
        table.add_column("A", 6);
        table.add_row(new[] { "x" });

        var result = table.render();
        Assert.Contains("┌", result, StringComparison.Ordinal);
        Assert.Contains("┐", result, StringComparison.Ordinal);
        Assert.Contains("└", result, StringComparison.Ordinal);
        Assert.Contains("┘", result, StringComparison.Ordinal);
        Assert.Contains("│", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithAutoWidth_ShouldSizeToContent()
    {
        var table = new LiveTable();
        table.add_column("ID", 0);
        table.add_row(new[] { "12345" });

        var result = table.render();
        Assert.Contains("12345", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SortBy_ShouldAffectRenderOrder()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "Charlie" });
        table.add_row(new[] { "Alice" });
        table.add_row(new[] { "Bob" });

        table.sort_by(0, ascending: true);
        var result = table.render();

        var aliceIndex = result.IndexOf("Alice", StringComparison.Ordinal);
        var bobIndex = result.IndexOf("Bob", StringComparison.Ordinal);
        var charlieIndex = result.IndexOf("Charlie", StringComparison.Ordinal);

        Assert.True(aliceIndex < bobIndex);
        Assert.True(bobIndex < charlieIndex);
    }

    [Fact]
    public void SortBy_Descending_ShouldReverseOrder()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "Alice" });
        table.add_row(new[] { "Bob" });
        table.add_row(new[] { "Charlie" });

        table.sort_by(0, ascending: false);
        var result = table.render();

        var aliceIndex = result.IndexOf("Alice", StringComparison.Ordinal);
        var charlieIndex = result.IndexOf("Charlie", StringComparison.Ordinal);

        Assert.True(charlieIndex < aliceIndex);
    }

    [Fact]
    public void FilterBy_ShouldOnlyShowMatchingRows()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "Alice" });
        table.add_row(new[] { "Bob" });
        table.add_row(new[] { "Charlie" });

        table.filter_by(0, "bo");
        var result = table.render();

        Assert.Contains("Bob", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Alice", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Charlie", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearFilter_ShouldShowAllRows()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "Alice" });
        table.add_row(new[] { "Bob" });

        table.filter_by(0, "Alice");
        table.clear_filter();
        var result = table.render();

        Assert.Contains("Alice", result, StringComparison.Ordinal);
        Assert.Contains("Bob", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ClearSort_ShouldResetOrder()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "B" });
        table.add_row(new[] { "A" });

        table.sort_by(0, ascending: false);
        table.clear_sort();
        var result = table.render();

        var aIndex = result.IndexOf("A", StringComparison.Ordinal);
        var bIndex = result.IndexOf("B", StringComparison.Ordinal);

        Assert.True(bIndex < aIndex);
    }

    [Fact]
    public void Clear_ShouldRemoveAllRows()
    {
        var table = new LiveTable();
        table.add_column("X", 5);
        table.add_row(new[] { "a" });
        table.add_row(new[] { "b" });
        table.clear();

        Assert.Empty(table.rows);
    }

    [Fact]
    public void Columns_ShouldReturnAddedColumns()
    {
        var table = new LiveTable();
        table.add_column("A", 5);
        table.add_column("B", 5);

        Assert.Equal(2, table.columns.Count);
        Assert.Equal("A", table.columns[0].title);
        Assert.Equal("B", table.columns[1].title);
    }

    [Fact]
    public void FilterBy_CaseInsensitive_ShouldMatch()
    {
        var table = new LiveTable();
        table.add_column("Name", 10);
        table.add_row(new[] { "Alice" });

        table.filter_by(0, "ALICE");
        var result = table.render();

        Assert.Contains("Alice", result, StringComparison.Ordinal);
    }
}