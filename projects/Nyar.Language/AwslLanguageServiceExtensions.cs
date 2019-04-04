using Nyar.Analyzer.Semantic;
using Std.Data.Text.Awsl.Semantic;
using Std.Data.Text.Awsl;

namespace Nyar.Language;

/// <summary>
///     AWSL 语言服务扩展方法，用于向 <see cref="ILanguageService" /> 注册 AWSL 语言及其语义分析提供者
/// </summary>
public static class AwslLanguageServiceExtensions
{
    /// <summary>
    ///     向语言服务注册 AWSL 语言
    /// </summary>
    /// <param name="service">语言服务实例</param>
    /// <param name="language">可选的语言定义，默认创建新实例</param>
    /// <returns>注册的 AWSL 语言定义</returns>
    public static AwslLanguage register_awsl(this ILanguageService service, AwslLanguage? language = null)
    {
        language ??= new AwslLanguage();

        var semanticBridge = new AwslSemanticBridge();
        service.register_language(language);
        service.register_provider<ISemanticAnalysisProvider>(language, semanticBridge);
        service.register_provider<IReferenceProvider>(language, semanticBridge);

        return language;
    }
}