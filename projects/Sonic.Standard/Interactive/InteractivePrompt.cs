using System.Globalization;
using System.Text;
using Sonic.Command;

namespace Sonic.Interactive;

/// <summary>
/// 交互式提示入口，提供文本输入、确认、选择等交互控件
/// </summary>
public static class InteractivePrompt
{
    /// <summary>
    /// 基础文本输入
    /// </summary>
    /// <param name="prompt">提示文本（可本地化）</param>
    /// <param name="options">提示选项</param>
    /// <returns>用户输入文本，输入被重定向时返回 null</returns>
    public static Task<string?> ask(LocalizableString prompt, PromptOptions? options = null)
    {
        return ask_with_validation(prompt, null, options);
    }

    /// <summary>
    /// 带验证的文本输入
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="validator">验证函数，返回 null 表示通过，否则返回错误消息</param>
    /// <param name="options">提示选项</param>
    /// <returns>验证通过的用户输入</returns>
    public static async Task<string?> ask_with_validation(
        LocalizableString prompt,
        Func<string, string?>? validator = null,
        PromptOptions? options = null)
    {
        options ??= new PromptOptions();
        var symbol = options.prompt_symbol;

        if (System.Console.IsInputRedirected)
        {
            return null;
        }

        while (true)
        {
            System.Console.Write($"{symbol}{prompt}: ");
            var input = System.Console.ReadLine()?.Trim() ?? "";

            if (validator is not null)
            {
                var error = validator(input);
                if (error is not null)
                {
                    System.Console.WriteLine($"  ✗ {error}");
                    continue;
                }
            }

            return string.IsNullOrEmpty(input) ? null : input;
        }
    }

    /// <summary>
    /// 类型化输入
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="prompt">提示文本</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="options">提示选项</param>
    /// <returns>用户输入值</returns>
    public static async Task<T?> ask<T>(LocalizableString prompt, T? defaultValue = default, PromptOptions? options = null)
    {
        options ??= new PromptOptions();
        var symbol = options.prompt_symbol;

        if (System.Console.IsInputRedirected)
        {
            return defaultValue;
        }

        while (true)
        {
            System.Console.Write($"{symbol}{prompt}: ");

            if (defaultValue is not null && !EqualityComparer<T>.Default.Equals(defaultValue, default))
            {
                System.Console.Write($"[{defaultValue}] ");
            }

            var input = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) && defaultValue is not null)
            {
                return defaultValue;
            }

            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }

            try
            {
                var result = (T?)Convert.ChangeType(input, typeof(T), CultureInfo.CurrentCulture);
                return result;
            }
            catch
            {
                System.Console.WriteLine($"  ✗ 请输入有效的 {typeof(T).Name} 值");
            }
        }
    }

    /// <summary>
    /// 密码输入，输入字符回显为 *
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <returns>密码文本，输入被重定向时返回 null</returns>
    public static Task<string?> password(LocalizableString prompt)
    {
        if (System.Console.IsInputRedirected)
        {
            return Task.FromResult<string?>(null);
        }

        System.Console.Write($"? {prompt}: ");

        var sb = new StringBuilder();
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Length--;
                System.Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                System.Console.Write('*');
            }
        }

        var result = sb.ToString();
        return Task.FromResult(string.IsNullOrEmpty(result) ? null : (string?)result);
    }

    /// <summary>
    /// 确认提示
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>用户选择结果</returns>
    public static Task<bool> confirm(LocalizableString prompt, bool defaultValue = true)
    {
        var options = new ConfirmOptions { default_value = defaultValue };
        return confirm(prompt, options);
    }

    /// <summary>
    /// 确认提示（增强版，支持危险操作警告、自定义标签）
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="options">确认选项</param>
    /// <returns>用户选择结果</returns>
    public static Task<bool> confirm(LocalizableString prompt, ConfirmOptions options)
    {
        options ??= new ConfirmOptions();
        var result = InteractiveConfirmEngine.run(prompt, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 单选列表
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="items">候选项</param>
    /// <param name="options">选择选项</param>
    /// <returns>选中的项，输入被重定向时返回 null</returns>
    public static Task<string?> select(LocalizableString prompt, IReadOnlyList<string> items, SelectOptions? options = null)
    {
        options ??= new SelectOptions();

        if (items.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        if (System.Console.IsInputRedirected)
        {
            print_select_list(prompt, items, options.page_size);
            return Task.FromResult<string?>(null);
        }

        var result = InteractiveSelectEngine.run_single_select(prompt, items, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 单选列表（兼容旧 API）
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="items">候选项</param>
    /// <param name="options">提示选项</param>
    /// <returns>选中的项，输入被重定向时返回 null</returns>
    public static Task<string?> select(LocalizableString prompt, IReadOnlyList<string> items, PromptOptions? options)
    {
        options ??= new PromptOptions();
        var selectOptions = new SelectOptions
        {
            page_size = options.page_size,
            search_enabled = options.search_enabled,
            prompt_symbol = options.prompt_symbol,
        };

        return select(prompt, items, selectOptions);
    }

    /// <summary>
    /// 多选列表
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="items">候选项</param>
    /// <param name="options">多选选项</param>
    /// <returns>选中的项集合</returns>
    public static Task<IReadOnlyList<string>> multi_select(LocalizableString prompt, IReadOnlyList<string> items, MultiSelectOptions? options = null)
    {
        options ??= new MultiSelectOptions();

        if (items.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        if (System.Console.IsInputRedirected)
        {
            print_multi_select_list(prompt, items);
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var result = InteractiveSelectEngine.run_multi_select(prompt, items, options);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 多选列表（兼容旧 API）
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="items">候选项</param>
    /// <param name="minSelected">最少选择数量</param>
    /// <returns>选中的项集合</returns>
    public static Task<IReadOnlyList<string>> multi_select(LocalizableString prompt, IReadOnlyList<string> items, int minSelected)
    {
        var options = new MultiSelectOptions { min_selected = minSelected };
        return multi_select(prompt, items, options);
    }

    /// <summary>
    /// 模糊查找
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="items">候选集合</param>
    /// <returns>选中的项</returns>
    public static Task<string?> fuzzy_finder(LocalizableString prompt, IEnumerable<string> items)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        if (System.Console.IsInputRedirected)
        {
            foreach (var item in itemList)
            {
                System.Console.WriteLine($"  {item}");
            }

            return Task.FromResult<string?>(null);
        }

        System.Console.Write($"? {prompt}: ");
        var query = System.Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(query))
        {
            return Task.FromResult<string?>(null);
        }

        var matches = fuzzy_match(itemList, query, 3);
        var selectOptions = new SelectOptions { page_size = System.Math.Min(10, matches.Count), search_enabled = true };

        if (matches.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        return select(prompt, matches, selectOptions);
    }

    /// <summary>
    /// 文件浏览选择
    /// </summary>
    /// <param name="prompt">提示文本</param>
    /// <param name="startPath">起始路径，默认为当前目录</param>
    /// <param name="filter">文件过滤模式，如 "*.txt"</param>
    /// <returns>选中的文件路径，输入被重定向时返回 null</returns>
    public static Task<string?> file_browser(LocalizableString prompt, string? startPath = null, string? filter = null)
    {
        if (System.Console.IsInputRedirected)
        {
            return Task.FromResult<string?>(null);
        }

        var currentDir = string.IsNullOrEmpty(startPath) ? Directory.GetCurrentDirectory() : startPath;

        while (true)
        {
            if (!Directory.Exists(currentDir))
            {
                System.Console.WriteLine($"  ✗ 目录不存在: {currentDir}");
                return Task.FromResult<string?>(null);
            }

            var entries = new List<string>();

            try
            {
                var dirInfo = new DirectoryInfo(currentDir);
                if (dirInfo.Parent != null)
                {
                    entries.Add("..");
                }

                foreach (var dir in Directory.GetDirectories(currentDir))
                {
                    entries.Add(Path.GetFileName(dir) + Path.DirectorySeparatorChar);
                }

                var searchPattern = string.IsNullOrEmpty(filter) ? "*" : filter;
                foreach (var file in Directory.GetFiles(currentDir, searchPattern))
                {
                    entries.Add(Path.GetFileName(file));
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Console.WriteLine($"  ✗ 无权限访问: {currentDir}");
                return Task.FromResult<string?>(null);
            }

            if (entries.Count == 0)
            {
                System.Console.WriteLine($"  ✗ 目录为空: {currentDir}");
                return Task.FromResult<string?>(null);
            }

            var options = new SelectOptions { page_size = 15, search_enabled = true };
            var selected = InteractiveSelectEngine.run_single_select(prompt, entries, options);

            if (selected == null)
            {
                return Task.FromResult<string?>(null);
            }

            if (selected == "..")
            {
                var parent = Directory.GetParent(currentDir);
                if (parent != null)
                {
                    currentDir = parent.FullName;
                }
                continue;
            }

            var fullPath = Path.Combine(currentDir, selected.TrimEnd(Path.DirectorySeparatorChar));

            if (Directory.Exists(fullPath))
            {
                currentDir = fullPath;
                continue;
            }

            if (File.Exists(fullPath))
            {
                return Task.FromResult<string?>(fullPath);
            }
        }
    }

    /// <summary>
    /// 输出单选列表（非交互模式）
    /// </summary>
    private static void print_select_list(LocalizableString prompt, IReadOnlyList<string> items, int pageSize)
    {
        System.Console.WriteLine($"? {prompt.default_value}:");
        for (var i = 0; i < System.Math.Min(pageSize, items.Count); i++)
        {
            System.Console.WriteLine($"  {i + 1}. {items[i]}");
        }
    }

    /// <summary>
    /// 输出多选列表（非交互模式）
    /// </summary>
    private static void print_multi_select_list(LocalizableString prompt, IReadOnlyList<string> items)
    {
        System.Console.WriteLine($"? {prompt.default_value}:");
        for (var i = 0; i < items.Count; i++)
        {
            System.Console.WriteLine($"  [ ] {items[i]}");
        }
    }

    /// <summary>
    /// 模糊匹配算法
    /// </summary>
    /// <param name="items">候选项</param>
    /// <param name="query">查询字符串</param>
    /// <param name="topN">返回前 N 个匹配项</param>
    /// <returns>匹配项列表，按匹配度降序排列</returns>
    public static List<string> fuzzy_match(List<string> items, string query, int topN)
    {
        var queryLower = query.ToLowerInvariant();
        var scored = new List<(string item, int score)>();

        foreach (var item in items)
        {
            var score = calculate_fuzzy_score(item.ToLowerInvariant(), queryLower);
            if (score > 0)
            {
                scored.Add((item, score));
            }
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));
        return scored.Take(topN).Select(x => x.item).ToList();
    }

    /// <summary>
    /// 计算单个字符串与查询的模糊匹配分数
    /// </summary>
    private static int calculate_fuzzy_score(string source, string query)
    {
        var score = 0;
        var queryIndex = 0;
        var consecutiveBonus = 0;
        var prevMatchIndex = -1;

        for (var i = 0; i < source.Length && queryIndex < query.Length; i++)
        {
            if (source[i] == query[queryIndex])
            {
                score += 10;
                queryIndex++;

                if (prevMatchIndex >= 0 && i == prevMatchIndex + 1)
                {
                    consecutiveBonus += 5;
                    score += consecutiveBonus;
                }
                else
                {
                    consecutiveBonus = 0;
                }

                prevMatchIndex = i;
            }
        }

        if (queryIndex < query.Length)
        {
            return 0;
        }

        return score;
    }
}