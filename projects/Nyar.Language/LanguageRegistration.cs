using Nyar.Language.Bash;
using Nyar.Language.Batch;
using Nyar.Language.C;
using Nyar.Language.JavaScript;
using Nyar.Language.Julia;
using Nyar.Language.Lua;
using Nyar.Language.PowerShell;
using Nyar.Language.Python;
using Nyar.Language.Rust;
using Nyar.Language.Sql;
using Nyar.Language.TypeScript;
using Nyar.Language.Wasm;

namespace Nyar.Language;

/// <summary>
///     语言注册扩展方法，集中注册所有内置语言
/// </summary>
public static class LanguageRegistration
{
    /// <summary>
    ///     向 LanguageService 注册所有内置语言及其方言目标
    /// </summary>
    public static void register_all_builtin_languages(this LanguageService service)
    {
        // Standard 方言语言（GC 高级语言及脚本语言）
        register(service, new PythonLanguage(), DialectTarget.Standard);
        register(service, new JavaScriptLanguage(), DialectTarget.Standard);
        register(service, new TypeScriptLanguage(), DialectTarget.Standard);
        register(service, new LuaLanguage(), DialectTarget.Standard);
        register(service, new JuliaLanguage(), DialectTarget.Standard);
        register(service, new SqlLanguage(), DialectTarget.Standard);
        register(service, new BashLanguage(), DialectTarget.Standard);
        register(service, new BatchLanguage(), DialectTarget.Standard);
        register(service, new PowerShellLanguage(), DialectTarget.Standard);

        // Core 方言语言（非 GC 系统级语言）
        register(service, new CLanguage(), DialectTarget.Core);
        register(service, new RustLanguage(), DialectTarget.Core);
        register(service, new WasmLanguage(), DialectTarget.Core);
    }

    private static void register(LanguageService service, Language language, DialectTarget target)
    {
        service.register_language(language);
        service.register_provider(language, target);
    }
}