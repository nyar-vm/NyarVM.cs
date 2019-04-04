namespace Std.Data.Text.Syntax;

/// <summary>
///     语言注册表，将语言标识符映射到解析能力
/// </summary>
public static class LanguageRegistry
{
    private static readonly Dictionary<string, LanguageEntry> _entries = new();

    /// <summary>
    ///     获取所有已注册的语言标识符
    /// </summary>
    public static IReadOnlyCollection<string> registered_languages => _entries.Keys;

    /// <summary>
    ///     注册语言解析器
    /// </summary>
    /// <param name="languageId">语言标识符，如 "typescript"、"valkyrie"。</param>
    /// <param name="language">语言配置实例。</param>
    /// <param name="parser">解析器工厂，从源文本产出语法根。</param>
    public static void register(string languageId, Language language, Func<ISource, SyntaxRoot> parser)
    {
        _entries[languageId] = new LanguageEntry(language, parser);
    }

    /// <summary>
    ///     使用指定语言解析源文本
    /// </summary>
    /// <param name="languageId">语言标识符。</param>
    /// <param name="source">源文本。</param>
    /// <returns>语法根节点。</returns>
    /// <exception cref="InvalidOperationException">语言未注册</exception>
    public static SyntaxRoot parse(string languageId, ISource source)
    {
        if (!_entries.TryGetValue(languageId, out var entry))
            throw new InvalidOperationException($"语言 '{languageId}' 未注册");

        return entry.parser(source);
    }

    /// <summary>
    ///     尝试使用指定语言解析源文本
    /// </summary>
    /// <param name="languageId">语言标识符。</param>
    /// <param name="source">源文本。</param>
    /// <param name="root">解析结果。</param>
    /// <returns>是否解析成功。</returns>
    public static bool try_parse(string languageId, ISource source, out SyntaxRoot? root)
    {
        root = null;
        if (!_entries.TryGetValue(languageId, out var entry)) return false;

        root = entry.parser(source);
        return true;
    }

    /// <summary>
    ///     获取已注册的语言配置
    /// </summary>
    /// <param name="languageId">语言标识符。</param>
    /// <returns>语言配置实例，未注册时返回 null。</returns>
    public static Language? get_language(string languageId)
    {
        return _entries.TryGetValue(languageId, out var entry) ? entry.language : null;
    }

    /// <summary>
    ///     判断指定语言是否已注册
    /// </summary>
    /// <param name="languageId">语言标识符。</param>
    /// <returns>是否已注册。</returns>
    public static bool is_registered(string languageId)
    {
        return _entries.ContainsKey(languageId);
    }

    /// <summary>
    ///     移除指定语言的注册
    /// </summary>
    /// <param name="languageId">语言标识符。</param>
    /// <returns>是否成功移除。</returns>
    public static bool unregister(string languageId)
    {
        return _entries.Remove(languageId);
    }

    /// <summary>
    ///     清空所有注册
    /// </summary>
    public static void clear()
    {
        _entries.Clear();
    }

    private readonly struct LanguageEntry
    {
        public Language language { get; }
        public Func<ISource, SyntaxRoot> parser { get; }

        public LanguageEntry(Language language, Func<ISource, SyntaxRoot> parser)
        {
            this.language = language;
            this.parser = parser;
        }
    }
}