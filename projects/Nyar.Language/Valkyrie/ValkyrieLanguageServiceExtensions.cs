using Nyar.Semantic;
using Oak.Valkyrie;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie;

public static class ValkyrieLanguageServiceExtensions
{
    public static ValkyrieLanguage register_valkyrie(this ILanguageService service, ValkyrieLanguage? language = null)
    {
        language ??= ValkyrieLanguage.Standard;

        var semanticBridge = new ValkyrieSemanticBridge();
        service.register_language(language);
        service.register_provider<ISemanticAnalysisProvider>(language, semanticBridge);
        service.register_provider<IReferenceProvider>(language, semanticBridge);

        return language;
    }
}
