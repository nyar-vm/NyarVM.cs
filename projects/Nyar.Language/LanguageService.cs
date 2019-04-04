namespace Nyar.Language;

public class LanguageService : ILanguageService
{
    private readonly Dictionary<string, ILanguage> _languages = new();
    private readonly Dictionary<ILanguage, Dictionary<Type, object>> _providers = new();

    public ILanguage get_language(string name)
    {
        if (_languages.TryGetValue(name, out var lang)) return lang;

        throw new KeyNotFoundException($"Language '{name}' is not registered.");
    }

    public void register_language(ILanguage language)
    {
        _languages[language.name] = language;
    }

    public void register_provider<TProvider>(ILanguage language, TProvider provider) where TProvider : notnull
    {
        if (!_providers.TryGetValue(language, out var langProviders))
        {
            langProviders = new Dictionary<Type, object>();
            _providers[language] = langProviders;
        }

        langProviders[typeof(TProvider)] = provider;
    }

    public TProvider? get_provider<TProvider>(ILanguage language) where TProvider : notnull
    {
        var current = language;
        while (current != null)
        {
            if (_providers.TryGetValue(current, out var langProviders))
                if (langProviders.TryGetValue(typeof(TProvider), out var provider))
                    return (TProvider)provider;

            current = current.@base;
        }

        return default;
    }

    /// <summary>
    ///     获取所有已注册的语言
    /// </summary>
    public IReadOnlyList<ILanguage> get_all_languages()
    {
        return [.. _languages.Values];
    }
}