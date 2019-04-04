using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Debug;
using Oak.DejaVu.Ecosystem;
using Oak.DejaVu.LanguageServer;
using Oak.DejaVu.Optimizer;
using Xunit;

namespace Oak.DejaVu.Tests;

public sealed class DejaVuIntegrationTests
{
    #region 编译管线端到端

    [Fact]
    public void Compiler_FullPipeline_ShouldParseOptimizeAndResolve()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% if show %}Hello {{ name |> uppercase }}{% end %}";

        var compiled = compiler.Compile(source, emitSymbolTable: true);

        Assert.NotEmpty(compiled.Nodes);
        Assert.NotNull(compiled.SymbolTable);
        Assert.True(compiled.CompiledAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Compiler_TypeScriptGeneration_ShouldProduceValidCode()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% if active %}{{ name |> uppercase }}{% end %}";

        var ts = compiler.CompileToTypeScript(source, "renderTemplate");

        Assert.Contains("export function renderTemplate", ts);
        Assert.Contains("escapeHtml", ts);
        Assert.Contains("toBoolean", ts);
        Assert.Contains("applyFilter", ts);
    }

    [Fact]
    public void Compiler_JavaGeneration_ShouldProduceValidCode()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% loop items %}{{ item }}{% end %}";

        var java = compiler.CompileToJava(source, "ItemRenderer");

        Assert.Contains("public class ItemRenderer", java);
        Assert.Contains("public String render", java);
        Assert.Contains("toIterable", java);
    }

    [Fact]
    public void Compiler_TypeScriptInterfaceInference_ShouldProduceInterface()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% if show %}{{ name }}{% end %}{% loop item in items %}{{ item }}{% end %}";

        var iface = compiler.InferTypeScriptInterface(source, "UserData");

        Assert.Contains("export interface UserData", iface);
    }

    [Fact]
    public void Compiler_TypeChecking_ShouldDetectVariables()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% let x = 1 %}{{ x }}{% end %}";

        var types = compiler.CheckTypes(source);

        Assert.NotEmpty(types);
        Assert.True(types.ContainsKey("x"));
    }

    #endregion

    #region 跨语言一致性

    [Fact]
    public void CrossLanguage_SimpleTemplate_AllLanguagesShouldGenerate()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "<h1>{{ title }}</h1><p>{{ body }}</p>";

        var ts = compiler.CompileToTypeScript(source, "render");
        var java = compiler.CompileToJava(source, "Template");

        Assert.Contains("title", ts);
        Assert.Contains("body", ts);
        Assert.Contains("title", java);
        Assert.Contains("body", java);
    }

    [Fact]
    public void CrossLanguage_IfElse_AllLanguagesShouldGenerate()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% if admin %}Admin{% else %}User{% end %}";

        var ts = compiler.CompileToTypeScript(source);
        var java = compiler.CompileToJava(source);

        Assert.Contains("if", ts);
        Assert.Contains("else", ts);
        Assert.Contains("if", java);
        Assert.Contains("else", java);
    }

    [Fact]
    public void CrossLanguage_LoopIn_AllLanguagesShouldGenerate()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% loop item in items %}{{ item }}{% end %}";

        var ts = compiler.CompileToTypeScript(source);
        var java = compiler.CompileToJava(source);

        Assert.Contains("item", ts);
        Assert.Contains("items", ts);
        Assert.Contains("item", java);
        Assert.Contains("items", java);
    }

    [Fact]
    public void CrossLanguage_PipeFilter_AllLanguagesShouldGenerate()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{{ name |> uppercase |> trim }}";

        var ts = compiler.CompileToTypeScript(source);
        var java = compiler.CompileToJava(source);

        Assert.Contains("applyFilter", ts);
        Assert.Contains("uppercase", ts);
        Assert.Contains("trim", ts);
        Assert.Contains("applyFilter", java);
        Assert.Contains("uppercase", java);
        Assert.Contains("trim", java);
    }

    [Fact]
    public void CrossLanguage_LetWith_AllLanguagesShouldGenerate()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% let x = 1 %}{{ x }}{% with alias = obj %}{{ alias.name }}{% end %}";

        var ts = compiler.CompileToTypeScript(source);
        var java = compiler.CompileToJava(source);

        Assert.NotEmpty(ts);
        Assert.NotEmpty(java);
    }

    [Fact]
    public void ConsistencyChecker_SimpleTemplate_ShouldReportCoverage()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var checker = new MultiLanguageConsistencyChecker();
        var source = "{{ name |> uppercase }}";

        var result = checker.Check(source, compiler);

        Assert.NotNull(result);
        Assert.NotEmpty(result.TypeScriptOutput);
        Assert.NotEmpty(result.JavaOutput);
    }

    #endregion

    #region 布局系统

    [Fact]
    public void LayoutResolver_CircularInheritance_ShouldDetect()
    {
        var resolver = new LayoutResolver();
        var loader = new InMemoryTemplateLoader
        {
            Templates = new Dictionary<string, string>
            {
                ["a.djv"] = "{% extends \"b.djv\" %}{% block content %}A{% end %}",
                ["b.djv"] = "{% extends \"a.djv\" %}{% block content %}B{% end %}"
            }
        };

        var chain = resolver.ResolveChain("a.djv", loader);

        Assert.Equal(LayoutResolveStatus.CircularInheritance, chain.Status);
    }

    [Fact]
    public void LayoutResolver_TemplateNotFound_ShouldReport()
    {
        var resolver = new LayoutResolver();
        var loader = new InMemoryTemplateLoader
        {
            Templates = new Dictionary<string, string>
            {
                ["child.djv"] = "{% extends \"missing.djv\" %}{% block content %}Child{% end %}"
            }
        };

        var chain = resolver.ResolveChain("child.djv", loader);

        Assert.Equal(LayoutResolveStatus.TemplateNotFound, chain.Status);
    }

    [Fact]
    public void LayoutResolver_SingleTemplate_ShouldReturnChainOfOne()
    {
        var resolver = new LayoutResolver();
        var loader = new InMemoryTemplateLoader
        {
            Templates = new Dictionary<string, string>
            {
                ["standalone.djv"] = "<h1>Hello</h1>"
            }
        };

        var chain = resolver.ResolveChain("standalone.djv", loader);

        Assert.Equal(LayoutResolveStatus.Success, chain.Status);
        Assert.Equal(1, chain.Depth);
    }

    [Fact]
    public void LayoutResolver_TwoLevelInheritance_ShouldResolve()
    {
        var resolver = new LayoutResolver();
        var loader = new InMemoryTemplateLoader
        {
            Templates = new Dictionary<string, string>
            {
                ["base.djv"] = "<html>{% block content %}Default{% end %}</html>",
                ["page.djv"] = "{% extends \"base.djv\" %}{% block content %}Page Content{% end %}"
            }
        };

        var chain = resolver.ResolveChain("page.djv", loader);

        Assert.Equal(LayoutResolveStatus.Success, chain.Status);
        Assert.Equal(2, chain.Depth);
        Assert.Equal("base.djv", chain.Nodes[0].TemplatePath);
        Assert.Equal("page.djv", chain.Nodes[1].TemplatePath);
    }

    [Fact]
    public void LayoutResolver_MergeBlocks_ChildOverridesParent()
    {
        var resolver = new LayoutResolver();
        var loader = new InMemoryTemplateLoader
        {
            Templates = new Dictionary<string, string>
            {
                ["base.djv"] = "<html>{% block title %}Base Title{% end %}{% block content %}Base Content{% end %}</html>",
                ["page.djv"] = "{% extends \"base.djv\" %}{% block title %}Page Title{% end %}"
            }
        };

        var chain = resolver.ResolveChain("page.djv", loader);
        var merged = resolver.MergeBlocks(chain);

        Assert.True(merged.ContainsKey("title"));
        Assert.True(merged.ContainsKey("content"));
        Assert.Equal("page.djv", merged["title"].OverrideFrom);
        Assert.Equal("base.djv", merged["content"].OverrideFrom);
    }

    #endregion

    #region 组件系统

    [Fact]
    public void ComponentRegistry_RegisterAndRender_ShouldWork()
    {
        var registry = new ComponentRegistry();
        registry.Register("card", "<div class=\"card\"><h2>{{ title }}</h2><div>{% block body %}Default{% end %}</div></div>",
        [
            new ComponentProp { Name = "title", Type = "string", Required = true }
        ],
        [
            new ComponentSlot { Name = "body", HasDefaultContent = true }
        ]);

        Assert.True(registry.HasComponent("card"));

        var component = registry.GetComponent("card");
        Assert.NotNull(component);
        Assert.Equal("card", component.Name);
        Assert.Single(component.Props);
        Assert.Single(component.Slots);
    }

    [Fact]
    public void ComponentRegistry_UnregisteredComponent_ShouldReturnNull()
    {
        var registry = new ComponentRegistry();

        Assert.False(registry.HasComponent("nonexistent"));
        Assert.Null(registry.GetComponent("nonexistent"));
    }

    [Fact]
    public void ComponentRegistry_Signature_ShouldIncludePropsAndSlots()
    {
        var registry = new ComponentRegistry();
        registry.Register("button", "<button>{{ label }}</button>",
        [
            new ComponentProp { Name = "label", Type = "string" },
            new ComponentProp { Name = "variant", Type = "string" }
        ]);

        var component = registry.GetComponent("button")!;
        Assert.Contains("label", component.Signature);
        Assert.Contains("variant", component.Signature);
    }

    #endregion

    #region 主题系统

    [Fact]
    public void ThemeRegistry_RegisterAndActivate_ShouldWork()
    {
        var registry = new ThemeRegistry();
        registry.Register("light", cssVariables: new Dictionary<string, string>
        {
            ["primary"] = "#3b82f6",
            ["background"] = "#ffffff"
        });

        registry.Activate("light");

        Assert.Equal("light", registry.ActiveThemeName);
        Assert.NotNull(registry.ActiveTheme);
        Assert.Equal("#3b82f6", registry.ActiveTheme.CssVariables["primary"]);
    }

    [Fact]
    public void ThemeRegistry_Inheritance_ShouldMergeVariables()
    {
        var registry = new ThemeRegistry();
        registry.Register("base", cssVariables: new Dictionary<string, string>
        {
            ["primary"] = "#3b82f6",
            ["secondary"] = "#64748b",
            ["background"] = "#ffffff"
        });

        registry.Register("dark", baseThemeName: "base", cssVariables: new Dictionary<string, string>
        {
            ["background"] = "#1e293b",
            ["text"] = "#f8fafc"
        });

        registry.Activate("dark");

        Assert.Equal("#3b82f6", registry.ActiveTheme!.CssVariables["primary"]);
        Assert.Equal("#1e293b", registry.ActiveTheme.CssVariables["background"]);
        Assert.Equal("#f8fafc", registry.ActiveTheme.CssVariables["text"]);
    }

    [Fact]
    public void ThemeRegistry_GenerateCssVariables_ShouldProduceCss()
    {
        var registry = new ThemeRegistry();
        registry.Register("test", cssVariables: new Dictionary<string, string>
        {
            ["primary"] = "#3b82f6",
            ["radius"] = "0.5rem"
        });

        registry.Activate("test");

        var css = registry.GenerateCssVariables();

        Assert.Contains(":root {", css);
        Assert.Contains("--primary: #3b82f6;", css);
        Assert.Contains("--radius: 0.5rem;", css);
    }

    [Fact]
    public void ThemeRegistry_ResolveCssVariables_ShouldReplace()
    {
        var registry = new ThemeRegistry();
        registry.Register("test", cssVariables: new Dictionary<string, string>
        {
            ["primary"] = "#3b82f6"
        });

        registry.Activate("test");

        var result = registry.ResolveCssVariables("color: var(--primary);");

        Assert.Equal("color: #3b82f6;", result);
    }

    [Fact]
    public void ThemeRegistry_ActivateUnregistered_ShouldThrow()
    {
        var registry = new ThemeRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.Activate("nonexistent"));
    }

    #endregion

    #region 语言服务

    [Fact]
    public void LanguageService_OpenDocument_ShouldReturnDiagnostics()
    {
        var service = new DejaVuLanguageService();
        var source = "<h1>{{ title }}</h1>";

        var diagnostics = service.OpenDocument("test.djv", source);

        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void LanguageService_GetCompletions_ShouldReturnItems()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("test.djv", "{{ name }}");

        var completions = service.GetCompletions("test.djv", 0, 3);

        Assert.NotEmpty(completions);
        Assert.Contains(completions, c => c.Label == "if");
        Assert.Contains(completions, c => c.Label == "loop");
        Assert.Contains(completions, c => c.Label == "uppercase");
    }

    [Fact]
    public void LanguageService_GetHover_ForFilter_ShouldReturnInfo()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("test.djv", "{{ name |> uppercase }}");

        var hover = service.GetHover("test.djv", 0, 15);

        Assert.NotNull(hover);
        Assert.Contains("uppercase", hover.Contents);
    }

    [Fact]
    public void LanguageService_GetDocumentSymbols_ShouldReturnBlocks()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("test.djv", "{% block content %}Hello{% end %}");

        var symbols = service.GetDocumentSymbols("test.djv");

        Assert.NotEmpty(symbols);
        Assert.Contains(symbols, s => s.Name == "content");
    }

    [Fact]
    public void LanguageService_UpdateDocument_ShouldRefreshDiagnostics()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("test.djv", "{{ x }}");

        var diagnostics = service.UpdateDocument("test.djv", "{{ y }}");

        Assert.NotNull(diagnostics);
    }

    [Fact]
    public void LanguageService_CloseDocument_ShouldRemoveCache()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("test.djv", "hello");
        service.CloseDocument("test.djv");

        var completions = service.GetCompletions("test.djv", 0, 0);

        Assert.Empty(completions);
    }

    #endregion

    #region 类型检查

    [Fact]
    public void TypeChecker_LiteralTypes_ShouldInferCorrectly()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% let x = 42 %}{{ x }}{% end %}";

        var types = compiler.CheckTypes(source);

        Assert.NotEmpty(types);
        Assert.True(types.ContainsKey("x"));
    }

    [Fact]
    public void TypeChecker_KnownTypes_ShouldResolve()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{{ name }} {{ age }}";

        var knownTypes = new Dictionary<string, TemplateType>
        {
            ["name"] = TemplateType.String,
            ["age"] = TemplateType.Number
        };

        var types = compiler.CheckTypes(source, knownTypes);

        Assert.Equal(TemplateType.String, types["name"]);
        Assert.Equal(TemplateType.Number, types["age"]);
    }

    [Fact]
    public void TypeChecker_LoopVariable_ShouldBeInScope()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% loop item in items %}{{ item }}{% end %}";

        var types = compiler.CheckTypes(source);

        Assert.True(types.ContainsKey("item"));
    }

    [Fact]
    public void TypeChecker_LetVariable_ShouldBeInScope()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{% let x = 42 %}{{ x }}{% end %}";

        var types = compiler.CheckTypes(source);

        Assert.True(types.ContainsKey("x"));
    }

    #endregion

    #region 语法高亮元数据

    [Fact]
    public void SyntaxScopes_ShouldDefineAllScopes()
    {
        var scopes = DejaVuSyntaxScopes.GetAllScopes();

        Assert.NotEmpty(scopes);
        Assert.True(scopes.Count >= 20);
        Assert.Contains(scopes, s => s.Scope == "keyword.control.dejavu");
        Assert.Contains(scopes, s => s.Scope == "variable.other.dejavu");
        Assert.Contains(scopes, s => s.Scope == "entity.name.function.filter.dejavu");
    }

    [Fact]
    public void SyntaxScopes_LanguageConfiguration_ShouldHaveComments()
    {
        var config = DejaVuSyntaxScopes.GetLanguageConfiguration();

        Assert.NotNull(config.Comments.BlockComment);
        Assert.Equal("{%--", config.Comments.BlockComment.Open);
        Assert.Equal("--%}", config.Comments.BlockComment.Close);
    }

    [Fact]
    public void SyntaxScopes_LanguageConfiguration_ShouldHaveFolding()
    {
        var config = DejaVuSyntaxScopes.GetLanguageConfiguration();

        Assert.NotNull(config.Folding.Markers);
        Assert.Contains("if", config.Folding.Markers.Start);
        Assert.Contains("end", config.Folding.Markers.End);
    }

    #endregion

    #region 标准库

    [Fact]
    public void StandardLibrary_ShouldHaveDefaultHelpers()
    {
        var lib = new TemplateStandardLibrary();

        Assert.True(lib.HasHelper("formatDate"));
        Assert.True(lib.HasHelper("formatNumber"));
        Assert.True(lib.HasHelper("pluralize"));
        Assert.True(lib.HasHelper("i18n"));
        Assert.True(lib.HasHelper("currency"));
        Assert.True(lib.HasHelper("percentage"));
        Assert.True(lib.HasHelper("urlEncode"));
        Assert.True(lib.HasHelper("jsonEncode"));
    }

    [Fact]
    public void StandardLibrary_HelperShouldHaveDetails()
    {
        var lib = new TemplateStandardLibrary();
        var helper = lib.GetHelper("currency");

        Assert.NotNull(helper);
        Assert.Equal("currency", helper.Name);
        Assert.NotEmpty(helper.Description);
        Assert.NotEmpty(helper.Parameters);
        Assert.Equal(TemplateType.String, helper.OutputType);
        Assert.NotEmpty(helper.TsImplementation);
        Assert.NotEmpty(helper.JavaImplementation);
    }

    #endregion

    #region 调试工具

    [Fact]
    public void SourceMap_BuildFromNodes_ShouldCreateMappings()
    {
        var parser = new DejaVuParser("doki");
        var result = parser.Parse("{% if x %}Hello{% end %}");

        var sourceMap = SourceMap.BuildFromNodes(result.Nodes, "test.djv");

        Assert.NotEmpty(sourceMap.Mappings);
    }

    [Fact]
    public void TemplateDebugger_TraceReport_ShouldGenerate()
    {
        var debugger = new TemplateDebugger { EnableTracing = true, EnableProfiling = true };
        debugger.StartTrace();
        debugger.Trace("Code", 1, 0, "name", 0.5);
        debugger.Trace("If", 2, 0, "condition", 1.2);
        debugger.StopTrace();

        var report = debugger.GenerateTraceReport();

        Assert.Contains("DejaVu", report);
        Assert.Contains("Code", report);
        Assert.Contains("If", report);
    }

    [Fact]
    public void TemplateDebugger_ContextSnapshot_ShouldGenerate()
    {
        var context = new Dictionary<string, object>
        {
            ["name"] = "World",
            ["count"] = 42,
            ["items"] = new List<object> { 1, 2, 3 }
        };

        var snapshot = TemplateDebugger.GenerateContextSnapshot(context);

        Assert.Contains("name", snapshot);
        Assert.Contains("World", snapshot);
        Assert.Contains("count", snapshot);
    }

    #endregion

    #region HTML 转义

    [Fact]
    public void HtmlEscaper_Content_ShouldEscapeSpecialChars()
    {
        var result = Security.HtmlEscaper.EscapeHtmlContent("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
        Assert.Contains("&#x27;", result);
    }

    [Fact]
    public void HtmlEscaper_Attribute_ShouldEscapeQuotes()
    {
        var result = Security.HtmlEscaper.EscapeHtmlAttribute("value\"onclick='bad'");

        Assert.Contains("&quot;", result);
        Assert.Contains("&apos;", result);
    }

    [Fact]
    public void HtmlEscaper_JavaScript_ShouldEscapeForStringContext()
    {
        var result = Security.HtmlEscaper.EscapeJavaScript("alert(\"xss\")");

        Assert.Contains("\\\"", result);
    }

    [Fact]
    public void HtmlEscaper_Url_ShouldEncodeSpecialChars()
    {
        var result = Security.HtmlEscaper.EscapeUrl("hello world&foo=bar");

        Assert.DoesNotContain(" ", result);
        Assert.DoesNotContain("&", result);
    }

    [Fact]
    public void HtmlEscaper_Css_ShouldEscapeNonAlphanumeric()
    {
        var result = Security.HtmlEscaper.EscapeCss("color: red;");

        Assert.Contains("\\:", result);
        Assert.Contains("\\;", result);
        Assert.Contains("\\ ", result);
    }

    [Fact]
    public void HtmlEscaper_NullInput_ShouldReturnEmpty()
    {
        Assert.Equal(string.Empty, Security.HtmlEscaper.EscapeHtmlContent(null));
        Assert.Equal(string.Empty, Security.HtmlEscaper.EscapeJavaScript(null));
        Assert.Equal(string.Empty, Security.HtmlEscaper.EscapeUrl(null));
    }

    #endregion

    #region 编译缓存

    [Fact]
    public void DejaVuCompiler_CompileTwice_ShouldProduceSameResult()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "{{ name }}";

        var compiled1 = compiler.Compile(source);
        var compiled2 = compiler.Compile(source);

        Assert.Equal(compiled1.Nodes.Count, compiled2.Nodes.Count);
    }

    [Fact]
    public void CompiledTemplate_ShouldHaveMetadata()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var source = "<h1>{{ title }}</h1>";

        var compiled = compiler.Compile(source, "test.djv");

        Assert.Equal("test.djv", compiled.TemplatePath);
        Assert.NotEmpty(compiled.Nodes);
        Assert.True(compiled.CompiledAt <= DateTimeOffset.UtcNow);
    }

    #endregion

    private sealed class InMemoryTemplateLoader : ITemplateLoader
    {
        public Dictionary<string, string> Templates { get; init; } = new();

        public string? Load(string templatePath)
        {
            return Templates.GetValueOrDefault(templatePath);
        }

        public string ResolvePath(string templateName)
        {
            return Templates.Keys.FirstOrDefault(k => k.Contains(templateName)) ?? templateName;
        }
    }
}
