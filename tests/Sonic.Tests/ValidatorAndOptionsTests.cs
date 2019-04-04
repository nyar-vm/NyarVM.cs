namespace Commander.Testing;

/// <summary>
///     PromptValidator 和 ConfirmOptions 测试
/// </summary>
public sealed class ValidatorAndOptionsTests
{
    #region PromptOptions 测试

    [Fact]
    public void PromptOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new PromptOptions();
        Assert.Equal("? ", options.prompt_symbol);
        Assert.False(options.search_enabled);
        Assert.Equal(10, options.page_size);
        Assert.False(options.history_enabled);
    }

    #endregion

    #region PromptValidator 测试

    [Fact]
    public void Required_NullInput_ShouldReturnError()
    {
        var validator = PromptValidator.required();
        var result = validator(null!);
        Assert.NotNull(result);
        Assert.Contains("不能为空", result);
    }

    [Fact]
    public void Required_EmptyInput_ShouldReturnError()
    {
        var validator = PromptValidator.required();
        var result = validator("");
        Assert.NotNull(result);
        Assert.Contains("不能为空", result);
    }

    [Fact]
    public void Required_WhitespaceInput_ShouldReturnError()
    {
        var validator = PromptValidator.required();
        var result = validator("");
        Assert.NotNull(result);
        Assert.Contains("不能为空", result);
    }

    [Fact]
    public void Required_ValidInput_ShouldReturnNull()
    {
        var validator = PromptValidator.required();
        var result = validator("hello");
        Assert.Null(result);
    }

    [Fact]
    public void MinLength_TooShort_ShouldReturnError()
    {
        var validator = PromptValidator.min_length(3);
        var result = validator("ab");
        Assert.NotNull(result);
        Assert.Contains("3", result);
    }

    [Fact]
    public void MinLength_ExactLength_ShouldReturnNull()
    {
        var validator = PromptValidator.min_length(3);
        var result = validator("abc");
        Assert.Null(result);
    }

    [Fact]
    public void MinLength_Longer_ShouldReturnNull()
    {
        var validator = PromptValidator.min_length(3);
        var result = validator("abcdef");
        Assert.Null(result);
    }

    [Fact]
    public void MaxLength_TooLong_ShouldReturnError()
    {
        var validator = PromptValidator.max_length(5);
        var result = validator("abcdef");
        Assert.NotNull(result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void MaxLength_ExactLength_ShouldReturnNull()
    {
        var validator = PromptValidator.max_length(5);
        var result = validator("abcde");
        Assert.Null(result);
    }

    [Fact]
    public void Regex_Match_ShouldReturnNull()
    {
        var validator = PromptValidator.regex(@"^\d+$", "必须为数字");
        var result = validator("12345");
        Assert.Null(result);
    }

    [Fact]
    public void Regex_NoMatch_ShouldReturnError()
    {
        var validator = PromptValidator.regex(@"^\d+$", "必须为数字");
        var result = validator("abc");
        Assert.NotNull(result);
        Assert.Contains("必须为数字", result);
    }

    [Fact]
    public void Email_ValidEmail_ShouldReturnNull()
    {
        var validator = PromptValidator.email();
        var result = validator("user@example.com");
        Assert.Null(result);
    }

    [Fact]
    public void Email_InvalidEmail_ShouldReturnError()
    {
        var validator = PromptValidator.email();
        var result = validator("not-an-email");
        Assert.NotNull(result);
    }

    [Fact]
    public void Combine_AllPass_ShouldReturnNull()
    {
        var validator = PromptValidator.combine(
            PromptValidator.required(),
            PromptValidator.min_length(3));

        var result = validator("hello");
        Assert.Null(result);
    }

    [Fact]
    public void Combine_FirstFails_ShouldReturnFirstError()
    {
        var validator = PromptValidator.combine(
            PromptValidator.required(),
            PromptValidator.min_length(3));

        var result = validator("");
        Assert.NotNull(result);
        Assert.Contains("不能为空", result);
    }

    [Fact]
    public void Combine_SecondFails_ShouldReturnSecondError()
    {
        var validator = PromptValidator.combine(
            PromptValidator.required(),
            PromptValidator.min_length(5));

        var result = validator("abc");
        Assert.NotNull(result);
        Assert.Contains("5", result);
    }

    #endregion

    #region ConfirmOptions 测试

    [Fact]
    public void ConfirmOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new ConfirmOptions();
        Assert.True(options.default_value);
        Assert.False(options.dangerous);
        Assert.Equal("Y", options.yes_label);
        Assert.Equal("N", options.no_label);
        Assert.Equal("? ", options.prompt_symbol);
    }

    [Fact]
    public void ConfirmOptions_Dangerous_ShouldBeSettable()
    {
        var options = new ConfirmOptions { dangerous = true };
        Assert.True(options.dangerous);
    }

    [Fact]
    public void ConfirmOptions_CustomLabels_ShouldBeSettable()
    {
        var options = new ConfirmOptions { yes_label = "是", no_label = "否" };
        Assert.Equal("是", options.yes_label);
        Assert.Equal("否", options.no_label);
    }

    #endregion

    #region SelectOptions 测试

    [Fact]
    public void SelectOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new SelectOptions();
        Assert.Equal("? ", options.prompt_symbol);
        Assert.Equal(10, options.page_size);
        Assert.False(options.search_enabled);
        Assert.True(options.show_instructions);
        Assert.Equal(0, options.default_index);
    }

    [Fact]
    public void MultiSelectOptions_DefaultValues_ShouldBeCorrect()
    {
        var options = new MultiSelectOptions();
        Assert.Equal("? ", options.prompt_symbol);
        Assert.Equal(10, options.page_size);
        Assert.Equal(1, options.min_selected);
        Assert.Equal(0, options.max_selected);
        Assert.False(options.search_enabled);
        Assert.True(options.show_instructions);
        Assert.Empty(options.default_selected);
        Assert.True(options.required);
    }

    [Fact]
    public void MultiSelectOptions_WithDefaults_ShouldBeSettable()
    {
        var options = new MultiSelectOptions
        {
            min_selected = 2,
            max_selected = 5,
            search_enabled = true,
            default_selected = [0, 2]
        };

        Assert.Equal(2, options.min_selected);
        Assert.Equal(5, options.max_selected);
        Assert.True(options.search_enabled);
        Assert.Equal([0, 2], options.default_selected);
    }

    #endregion

    #region FuzzyMatch 测试

    [Fact]
    public void FuzzyMatch_ExactMatch_ShouldScoreHighest()
    {
        var items = new List<string> { "apple", "application", "banana" };
        var results = InteractivePrompt.fuzzy_match(items, "apple", 3);

        Assert.Single(results);
        Assert.Equal("apple", results[0]);
    }

    [Fact]
    public void FuzzyMatch_PrefixMatch_ShouldReturnResults()
    {
        var items = new List<string> { "apple", "application", "apricot", "banana" };
        var results = InteractivePrompt.fuzzy_match(items, "app", 3);

        Assert.True(results.Count <= 3);
        Assert.Contains("apple", results);
        Assert.Contains("application", results);
    }

    [Fact]
    public void FuzzyMatch_NoMatch_ShouldReturnEmpty()
    {
        var items = new List<string> { "apple", "banana" };
        var results = InteractivePrompt.fuzzy_match(items, "xyz", 3);

        Assert.Empty(results);
    }

    [Fact]
    public void FuzzyMatch_CaseInsensitive_ShouldMatch()
    {
        var items = new List<string> { "Apple", "BANANA" };
        var results = InteractivePrompt.fuzzy_match(items, "apple", 3);

        Assert.Contains("Apple", results);
    }

    #endregion
}