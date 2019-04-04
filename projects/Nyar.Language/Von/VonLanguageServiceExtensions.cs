using Nyar.Analyzer.Semantic;
using Nyar.Language.Von.Semantic;

namespace Nyar.Language.Von;

public static class VonLanguageServiceExtensions
{
    public static VonLanguage register_von(this ILanguageService service, VonLanguage? language = null)
    {
        language ??= new VonLanguage();

        var semanticBridge = new VonSemanticBridge();
        service.register_language(language);
        service.register_provider<ISemanticAnalysisProvider>(language, semanticBridge);
        service.register_provider<IReferenceProvider>(language, semanticBridge);

        return language;
    }
}