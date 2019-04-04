using Nyar.Assembler;
using Nyar.Language;
using Nyar.VM.LegacyVM.Compiler;
using Nyar.VM.LegacyVM.Evaluator;

namespace Nyar.VM.LegacyVM.Runner;

/// <summary>
///     LegacyVM 统一运行入口，负责根据语言标识选择对应的求值器执行。
///     支持：bash, batch, powershell, python, javascript, typescript, c, rust, wasm, julia, lua, sql。
/// </summary>
public sealed class LegacyVmRunner
{
    private readonly LanguageService _language_service;
    private readonly Dictionary<string, Func<string, Dictionary<string, object>, object>> _evaluators;

    /// <summary>
    ///     创建运行器
    /// </summary>
    public LegacyVmRunner()
    {
        _language_service = new LanguageService();
        _language_service.register_all_builtin_languages();

        _evaluators = new Dictionary<string, Func<string, Dictionary<string, object>, object>>(StringComparer.OrdinalIgnoreCase)
        {
            ["bash"] = (source, env) => new BashEvaluator().evaluate(source, env),
            ["sh"] = (source, env) => new BashEvaluator().evaluate(source, env),
            ["batch"] = (source, env) => new BatchEvaluator().evaluate(source, env),
            ["bat"] = (source, env) => new BatchEvaluator().evaluate(source, env),
            ["cmd"] = (source, env) => new BatchEvaluator().evaluate(source, env),
            ["powershell"] = (source, env) => new PowerShellEvaluator().evaluate(source, env),
            ["ps1"] = (source, env) => new PowerShellEvaluator().evaluate(source, env),
            ["pwsh"] = (source, env) => new PowerShellEvaluator().evaluate(source, env),
            ["python"] = (source, env) => evaluate_python(source, env),
            ["py"] = (source, env) => evaluate_python(source, env),
            ["javascript"] = (source, env) => evaluate_javascript(source, env),
            ["js"] = (source, env) => evaluate_javascript(source, env),
            ["c"] = (source, env) => evaluate_c(source, env),
            ["typescript"] = (source, env) => evaluate_typescript(source, env),
            ["ts"] = (source, env) => evaluate_typescript(source, env),
            ["rust"] = (source, env) => evaluate_rust(source, env),
            ["rs"] = (source, env) => evaluate_rust(source, env),
            ["wasm"] = (source, env) => new WasmEvaluator().evaluate(source, env),
            ["julia"] = (source, env) => evaluate_julia(source, env),
            ["jl"] = (source, env) => evaluate_julia(source, env),
            ["lua"] = (source, env) => new LuaAstEvaluator(env).evaluate(source),
            ["sql"] = (source, _) => new SqlAstEvaluator().evaluate(source)
        };
    }

    /// <summary>
    ///     语言服务，提供语言注册和查找能力
    /// </summary>
    public LanguageService language_service => _language_service;

    /// <summary>
    ///     列出支持的语言
    /// </summary>
    public IReadOnlyList<string> supported_languages
    {
        get
        {
            var langs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in _evaluators.Keys)
            {
                langs.Add(key);
            }

            return [.. langs];
        }
    }

    /// <summary>
    ///     检测语言是否已注册
    /// </summary>
    /// <param name="language">语言标识</param>
    /// <returns>是否已注册</returns>
    public bool is_language_supported(string language)
    {
        return _evaluators.ContainsKey(language);
    }

    /// <summary>
    ///     根据文件路径检测语言标识
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>语言标识，无法识别时返回文件扩展名</returns>
    public static string detect_language(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        return ext switch
        {
            ".sh" or ".bash" => "bash",
            ".bat" or ".cmd" => "batch",
            ".ps1" => "powershell",
            ".py" => "python",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".c" => "c",
            ".rs" => "rust",
            ".jl" => "julia",
            ".lua" => "lua",
            ".wasm" => "wasm",
            ".sql" => "sql",
            _ => ext.TrimStart('.')
        };
    }

    /// <summary>
    ///     从文件路径和内容检测语言（shebang 优先，扩展名其次）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="source">文件源码</param>
    /// <returns>检测到的语言标识</returns>
    public static string detect_language_from_file(string filePath, string source)
    {
        var shebang = detect_language_from_shebang(source);
        if (shebang != null)
        {
            return shebang;
        }

        return detect_language(filePath);
    }

    /// <summary>
    ///     从 shebang 行检测语言
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <returns>检测到的语言标识，无法识别时返回 null</returns>
    private static string? detect_language_from_shebang(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return null;
        }

        var firstLine = source.Split('\n')[0].Trim();
        if (!firstLine.StartsWith("#!"))
        {
            return null;
        }

        var shebang = firstLine[2..].Trim();

        if (shebang.Contains("env "))
        {
            var parts = shebang.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                shebang = parts[1];
            }
        }

        if (shebang.Contains("python3") || shebang.Contains("python"))
        {
            return "python";
        }

        if (shebang.Contains("node") || shebang.Contains("deno") || shebang.Contains("bun"))
        {
            return "javascript";
        }

        if (shebang.Contains("bash") || shebang.Contains("sh"))
        {
            return "bash";
        }

        if (shebang.Contains("pwsh") || shebang.Contains("powershell"))
        {
            return "powershell";
        }

        if (shebang.Contains("lua"))
        {
            return "lua";
        }

        if (shebang.Contains("julia"))
        {
            return "julia";
        }

        if (shebang.Contains("rustc") || shebang.Contains("cargo"))
        {
            return "rust";
        }

        return null;
    }

    /// <summary>
    ///     从代码内容启发式检测语言
    /// </summary>
    /// <param name="code">代码文本</param>
    /// <returns>检测到的语言标识，无法识别时默认为 bash</returns>
    public static string detect_language_from_content(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return "bash";
        }

        var trimmed = code.TrimStart();

        if (trimmed.StartsWith("#!"))
        {
            var shebang = detect_language_from_shebang(code);
            if (shebang != null)
            {
                return shebang;
            }
        }

        if (trimmed.StartsWith("console.log") || trimmed.StartsWith("let ")
            || trimmed.StartsWith("const ") || trimmed.StartsWith("var ")
            || trimmed.StartsWith("function "))
        {
            return "javascript";
        }

        if (trimmed.StartsWith("print(") || trimmed.StartsWith("def ")
            || trimmed.StartsWith("import ") || trimmed.StartsWith("from "))
        {
            return "python";
        }

        if (trimmed.StartsWith("int ") || trimmed.StartsWith("void ")
            || trimmed.StartsWith("#include") || trimmed.StartsWith("//"))
        {
            return "c";
        }

        if (trimmed.StartsWith("echo ") || trimmed.StartsWith("#!/bin/bash")
            || trimmed.StartsWith("#!/bin/sh"))
        {
            return "bash";
        }

        if (trimmed.StartsWith("Write-Host") || trimmed.StartsWith("Get-")
            || trimmed.StartsWith("param("))
        {
            return "powershell";
        }

        if (trimmed.StartsWith("fn ") || trimmed.StartsWith("let mut") || trimmed.Contains("println!"))
        {
            return "rust";
        }

        if (trimmed.StartsWith("function ") && trimmed.Contains("end"))
        {
            return "julia";
        }

        if (trimmed.StartsWith("local ") || (trimmed.StartsWith("function ") && trimmed.Contains("end")))
        {
            return "lua";
        }

        if (trimmed.StartsWith("interface ") || trimmed.StartsWith("type ") || trimmed.StartsWith("enum "))
        {
            return "typescript";
        }

        if (trimmed.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("CREATE TABLE ", StringComparison.OrdinalIgnoreCase))
        {
            return "sql";
        }

        return "bash";
    }

    /// <summary>
    ///     运行指定语言的源码
    /// </summary>
    /// <param name="language">语言标识（bash, batch, powershell, c, python 等）</param>
    /// <param name="source">脚本源码</param>
    /// <returns>执行结果</returns>
    public object run(string language, string source)
    {
        return run(language, source, new Dictionary<string, object>());
    }

    /// <summary>
    ///     运行指定语言的源码
    /// </summary>
    /// <param name="language">语言标识</param>
    /// <param name="source">脚本源码</param>
    /// <param name="env">初始运行环境</param>
    /// <returns>执行结果</returns>
    public object run(string language, string source, Dictionary<string, object> env)
    {
        // 通过 LanguageService 验证语言是否已注册
        try
        {
            _language_service.get_language(language);
        }
        catch (KeyNotFoundException)
        {
            throw new NotSupportedException($"语言 '{language}' 不受支持。NyarVM 遵循 GraalVM 设计原则，完全自实现所有语言运行时，不依赖系统解释器。请使用 Oak 实现对应的词法分析和语法分析。");
        }

        // 通过求值器执行
        if (_evaluators.TryGetValue(language, out var evaluator))
        {
            return evaluator(source, env);
        }

        throw new NotSupportedException($"语言 '{language}' 已注册但未配置执行引擎：{language}");
    }

    /// <summary>
    ///     将源码编译为 Nyar 字节码的 GenerateModule（Futamura 编译路径）。
    /// </summary>
    /// <param name="language">语言标识</param>
    /// <param name="source">源码文本</param>
    /// <param name="moduleName">模块名称</param>
    /// <returns>编译后的 GenerateModule</returns>
    public GenerateModule compile(string language, string source, string moduleName = "main")
    {
        var compiler = get_compiler(language);
        if (compiler is null)
        {
            throw new NotSupportedException($"语言 '{language}' 尚不支持 Futamura 编译。可用语言：javascript, typescript, python");
        }

        return compiler.compile(source, moduleName);
    }

    /// <summary>
    ///     获取指定语言的编译器实例
    /// </summary>
    /// <param name="language">语言标识</param>
    /// <returns>编译器实例，不支持时返回 null</returns>
    public ILanguageCompiler? get_compiler(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "javascript" or "js" => new JavaScriptFutamuraCompiler(),
            "typescript" or "ts" => new JavaScriptFutamuraCompiler(),
            "python" or "py" => new PythonFutamuraCompiler(),
            _ => null
        };
    }

    /// <summary>
    ///     编译 JavaScript 源码为 GenerateModule
    /// </summary>
    public GenerateModule compile_javascript(string source, string moduleName = "main")
    {
        var compiler = new JavaScriptFutamuraCompiler();
        return compiler.compile(source, moduleName);
    }

    /// <summary>
    ///     编译 TypeScript 源码为 GenerateModule
    /// </summary>
    public GenerateModule compile_typescript(string source, string moduleName = "main")
    {
        var compiler = new JavaScriptFutamuraCompiler();
        return compiler.compile(source, moduleName);
    }

    /// <summary>
    ///     编译 Python 源码为 GenerateModule
    /// </summary>
    public GenerateModule compile_python(string source, string moduleName = "main")
    {
        var compiler = new PythonFutamuraCompiler();
        return compiler.compile(source, moduleName);
    }

    /// <summary>
    ///     将 GenerateModule 编译为 .nyar 字节码
    /// </summary>
    /// <param name="module">元编译模块</param>
    /// <returns>.nyar 二进制字节数组</returns>
    public byte[] compile_to_nyar(GenerateModule module)
    {
        var bytecodeCompiler = new NyarBytecodeCompiler();
        return bytecodeCompiler.compile(module);
    }

    /// <summary>
    ///     将 GenerateModule 编译为 .nyar 字节码并写入文件
    /// </summary>
    /// <param name="module">元编译模块</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <returns>.nyar 二进制字节数组</returns>
    public byte[] compile_to_nyar_file(GenerateModule module, string outputPath)
    {
        var bytes = compile_to_nyar(module);
        File.WriteAllBytes(outputPath, bytes);
        return bytes;
    }

    /// <summary>
    ///     将 x64 机器码编译为 PE 可执行文件
    /// </summary>
    /// <param name="entryFunctionName">入口函数名称</param>
    /// <param name="x64Code">x64 机器码字节数组</param>
    /// <param name="outputPath">输出 .exe 文件路径</param>
    /// <returns>编译后的 PE 字节数组</returns>
    public byte[] compile_to_pe(string entryFunctionName, byte[] x64Code, string outputPath)
    {
        var compiler = new PeCompiler();
        return compiler.compile(entryFunctionName, x64Code, outputPath);
    }

    #region 语言特定求值方法

    /// <summary>
    ///     求值 Python 源码
    /// </summary>
    private static object evaluate_python(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.Python.Lexer.PythonLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Oak.Python.Parser.PythonParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_python_config();
        var evaluator = new PythonTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    /// <summary>
    ///     求值 JavaScript 源码
    /// </summary>
    private static object evaluate_javascript(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.JavaScript.Lexer.JsLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Oak.JavaScript.Parser.JsParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_javascript_config();
        var evaluator = new TypeScriptTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    /// <summary>
    ///     求值 TypeScript 源码
    /// </summary>
    private static object evaluate_typescript(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.Typescript.Lexer.TsLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Oak.Typescript.Parsing.TsParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_typescript_config();
        var evaluator = new TypeScriptTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    /// <summary>
    ///     求值 C 源码
    /// </summary>
    private static object evaluate_c(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.C.CLexer();
        var tokens = lexer.tokenize_as_c_tokens(source);
        var parser = new Oak.C.CParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_c_config();
        var evaluator = new CTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    /// <summary>
    ///     求值 Rust 源码
    /// </summary>
    private static object evaluate_rust(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.Rust.RustLexer();
        var tokens = lexer.tokenize_as_rust_tokens(source);
        var parser = new Oak.Rust.RustParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_rust_config();
        var evaluator = new RustTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    /// <summary>
    ///     求值 Julia 源码
    /// </summary>
    private static object evaluate_julia(string source, Dictionary<string, object> env)
    {
        var lexer = new Oak.Julia.Lexer.JlLexer();
        var tokens = lexer.tokenize(source);
        var parser = new Oak.Julia.Parser.JlParser();
        var ast = parser.parse(tokens);
        var config = LanguageRuntimeConfigFactory.create_julia_config();
        var evaluator = new JuliaTreeEvaluator(config, env);
        return evaluator.evaluate(ast);
    }

    #endregion
}