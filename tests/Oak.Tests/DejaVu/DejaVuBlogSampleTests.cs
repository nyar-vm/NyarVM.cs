using Oak.DejaVu.Benchmark;
using Oak.DejaVu.Cli;
using Oak.DejaVu.CodeGen;
using Oak.DejaVu.Ecosystem;
using Oak.DejaVu.LanguageServer;
using Oak.DejaVu.Optimizer;
using Xunit;

namespace Oak.DejaVu.Tests;

public sealed class DejaVuBlogSampleTests
{
    #region 博客系统模板

    private const string LayoutTemplate = """
                                           <!DOCTYPE html>
                                           <html lang="zh-CN">
                                           <head>
                                               <meta charset="UTF-8">
                                               <title>{% block title %}博客{% end %} - My Blog</title>
                                               <link rel="stylesheet" href="/assets/style.css">
                                           </head>
                                           <body>
                                               <header class="site-header">
                                                   <h1><a href="/">My Blog</a></h1>
                                                   <nav>{% block nav %}{% loop item in nav_items %}<a href="{{ item.url }}">{{ item.label }}</a>{% end %}{% end %}</nav>
                                               </header>
                                               <main>{% block content %}{% end %}</main>
                                               <footer>{% block footer %}<p>&copy; 2026 My Blog</p>{% end %}</footer>
                                           </body>
                                           </html>
                                           """;

    private const string PostListTemplate = """
                                            {% extends "layouts/default.djv" %}
                                            {% block title %}文章列表{% end %}
                                            {% block content %}
                                            <div class="post-list">
                                                {% if posts %}
                                                    {% loop post in posts %}
                                                    <article class="post-card">
                                                        <h2><a href="{{ post.url }}">{{ post.title }}</a></h2>
                                                        <p>{{ post.excerpt |> truncate:200 }}</p>
                                                        <div class="meta">
                                                            <span class="date">{{ post.date |> date }}</span>
                                                            <span class="author">{{ post.author }}</span>
                                                            {% if post.tags %}
                                                            <div class="tags">{% loop tag in post.tags %}<span class="tag">{{ tag }}</span>{% end %}</div>
                                                            {% end %}
                                                        </div>
                                                    </article>
                                                    {% end %}
                                                {% else %}
                                                    <p class="empty">暂无文章</p>
                                                {% end %}
                                            </div>
                                            {% if total_pages > 1 %}
                                            <nav class="pagination">
                                                {% if current_page > 1 %}<a href="?page={{ current_page - 1 }}" class="prev">上一页</a>{% end %}
                                                {% loop p in page_range %}
                                                    {% if p == current_page %}<span class="current">{{ p }}</span>{% else %}<a href="?page={{ p }}">{{ p }}</a>{% end %}
                                                {% end %}
                                                {% if current_page < total_pages %}<a href="?page={{ current_page + 1 }}" class="next">下一页</a>{% end %}
                                            </nav>
                                            {% end %}
                                            {% end %}
                                            """;

    private const string PostDetailTemplate = """
                                              {% extends "layouts/default.djv" %}
                                              {% block title %}{{ post.title }}{% end %}
                                              {% block content %}
                                              <article class="post-detail">
                                                  <h1>{{ post.title }}</h1>
                                                  <div class="meta">
                                                      <span class="date">{{ post.date |> date }}</span>
                                                      <span class="author">{{ post.author }}</span>
                                                      {% if post.tags %}
                                                      <div class="tags">{% loop tag in post.tags %}<span class="tag">{{ tag }}</span>{% end %}</div>
                                                      {% end %}
                                                  </div>
                                                  <div class="content">{{ post.body }}</div>
                                              </article>
                                              <section class="comments">
                                                  <h2>评论 ({{ comments |> length }})</h2>
                                                  {% if comments %}
                                                      {% loop comment in comments %}
                                                      <div class="comment">
                                                          <strong>{{ comment.author }}</strong>
                                                          <span>{{ comment.date |> date }}</span>
                                                          <p>{{ comment.body }}</p>
                                                      </div>
                                                      {% end %}
                                                  {% else %}
                                                      <p class="empty">暂无评论</p>
                                                  {% end %}
                                              </section>
                                              {% end %}
                                              """;

    private const string SidebarComponent = """
                                            <aside class="sidebar">
                                                <section class="recent-posts">
                                                    <h3>最新文章</h3>
                                                    {% loop post in recent_posts %}
                                                    <a href="{{ post.url }}">{{ post.title }}</a>
                                                    {% end %}
                                                </section>
                                                <section class="categories">
                                                    <h3>分类</h3>
                                                    {% loop cat in categories %}
                                                    <a href="{{ cat.url }}">{{ cat.name }} ({{ cat.count }})</a>
                                                    {% end %}
                                                </section>
                                            </aside>
                                            """;

    #endregion

    [Fact]
    public void Blog_LayoutTemplate_ShouldCompile()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var compiled = compiler.Compile(LayoutTemplate, emitSymbolTable: true);

        Assert.NotEmpty(compiled.Nodes);
        Assert.NotNull(compiled.SymbolTable);
        Assert.True(compiled.SymbolTable.Blocks.Contains("title"));
        Assert.True(compiled.SymbolTable.Blocks.Contains("content"));
        Assert.True(compiled.SymbolTable.Blocks.Contains("nav"));
        Assert.True(compiled.SymbolTable.Blocks.Contains("footer"));
    }

    [Fact]
    public void Blog_PostListTemplate_ShouldCompileToTypeScript()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var ts = compiler.CompileToTypeScript(PostListTemplate, "renderPostList");

        Assert.Contains("export function renderPostList", ts);
        Assert.Contains("applyFilter", ts);
        Assert.Contains("toBoolean", ts);
        Assert.Contains("toIterable", ts);
    }

    [Fact]
    public void Blog_PostDetailTemplate_ShouldCompileToJava()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var java = compiler.CompileToJava(PostDetailTemplate, "PostDetailRenderer");

        Assert.Contains("public class PostDetailRenderer", java);
        Assert.Contains("applyFilter", java);
        Assert.Contains("toIterable", java);
    }

    [Fact]
    public void Blog_SidebarComponent_ShouldRegisterAsComponent()
    {
        var registry = new ComponentRegistry();
        registry.Register("sidebar", SidebarComponent,
        [
            new ComponentProp { Name = "recent_posts", Type = "any[]", Description = "最新文章列表" },
            new ComponentProp { Name = "categories", Type = "any[]", Description = "分类列表" }
        ]);

        Assert.True(registry.HasComponent("sidebar"));
        var component = registry.GetComponent("sidebar")!;
        Assert.Equal(2, component.Props.Count);
    }

    [Fact]
    public void Blog_AllTemplates_ShouldPassTypeChecking()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));

        var layoutTypes = compiler.CheckTypes(LayoutTemplate);
        var listTypes = compiler.CheckTypes(PostListTemplate);
        var detailTypes = compiler.CheckTypes(PostDetailTemplate);

        Assert.NotEmpty(layoutTypes);
        Assert.NotEmpty(listTypes);
        Assert.NotEmpty(detailTypes);
    }

    [Fact]
    public void Blog_ThemeSystem_ShouldSupportLightAndDark()
    {
        var themeRegistry = new ThemeRegistry();

        themeRegistry.Register("light", cssVariables: new Dictionary<string, string>
        {
            ["bg-color"] = "#ffffff",
            ["text-color"] = "#1e293b",
            ["primary"] = "#3b82f6",
            ["border-color"] = "#e2e8f0",
            ["card-bg"] = "#f8fafc"
        });

        themeRegistry.Register("dark", baseThemeName: "light", cssVariables: new Dictionary<string, string>
        {
            ["bg-color"] = "#0f172a",
            ["text-color"] = "#f1f5f9",
            ["primary"] = "#60a5fa",
            ["border-color"] = "#334155",
            ["card-bg"] = "#1e293b"
        });

        themeRegistry.Activate("dark");

        var css = themeRegistry.GenerateCssVariables();

        Assert.Contains("--bg-color: #0f172a", css);
        Assert.Contains("--primary: #60a5fa", css);
        Assert.Contains("--card-bg: #1e293b", css);
        Assert.DoesNotContain("--bg-color: #ffffff", css);
    }

    [Fact]
    public void Blog_LanguageService_ShouldProvideCompletions()
    {
        var service = new DejaVuLanguageService();
        service.OpenDocument("blog.djv", PostListTemplate);

        var completions = service.GetCompletions("blog.djv", 0, 0);

        Assert.NotEmpty(completions);
        Assert.Contains(completions, c => c.Label == "if");
        Assert.Contains(completions, c => c.Label == "loop");
        Assert.Contains(completions, c => c.Label == "uppercase");
        Assert.Contains(completions, c => c.Label == "trim");
    }

    [Fact]
    public void Blog_ConsistencyCheck_ShouldPass()
    {
        var compiler = new DejaVuCompiler(new DejaVuParser("doki"));
        var checker = new MultiLanguageConsistencyChecker();

        var result = checker.Check(PostListTemplate, compiler);

        Assert.NotEmpty(result.TypeScriptOutput);
        Assert.NotEmpty(result.JavaOutput);
    }

    [Fact]
    public void Blog_CliInitProject_ShouldCreateStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"dejavu-blog-test-{Guid.NewGuid():N}");
        try
        {
            var cli = new DejaVuCli();
            var files = cli.InitProject(tempDir, "my-blog");

            Assert.NotEmpty(files);
            Assert.True(Directory.Exists(Path.Combine(tempDir, "layouts")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "components")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "pages")));
            Assert.True(Directory.Exists(Path.Combine(tempDir, "themes")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Blog_BenchmarkRunner_ShouldProduceReport()
    {
        var runner = new DejaVuBenchmarkRunner();
        var report = runner.RunAll();

        Assert.NotEmpty(report.CompileBenchmarks);
        Assert.NotEmpty(report.RenderBenchmarks);
        Assert.NotEmpty(report.CodeGenBenchmarks);

        var text = DejaVuBenchmarkRunner.GenerateReport(report);
        Assert.Contains("DejaVu", text);
        Assert.Contains("编译性能", text);
        Assert.Contains("渲染性能", text);
    }
}
