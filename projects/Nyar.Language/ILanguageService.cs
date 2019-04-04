namespace Nyar.Language;

/// <summary>
///     语言服务，充当所有语言特性 Provider 的泛型注册中心
///     绝对不耦合任何具体的 Provider 接口（如 Lexer, Parser 等）
/// </summary>
public interface ILanguageService
{
    ILanguage get_language(string name);

    void register_language(ILanguage language);

    /// <summary>
    ///     注册一个泛型 Provider
    /// </summary>
    void register_provider<TProvider>(ILanguage language, TProvider provider) where TProvider : notnull;

    /// <summary>
    ///     获取指定的泛型 Provider
    /// </summary>
    TProvider? get_provider<TProvider>(ILanguage language) where TProvider : notnull;

    /// <summary>
    ///     获取所有已注册的语言
    /// </summary>
    IReadOnlyList<ILanguage> get_all_languages();
}