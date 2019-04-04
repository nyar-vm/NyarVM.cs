using System.Text;

namespace Asgard.CLI.Commands;

public static class NewCommand
{
    private static readonly string[] _template_names = ["webapp", "api", "dashboard", "fullstack"];

    public static int execute(
        string name,
        string template = "webapp",
        string target = "wasm")
    {
        var projectDir = Path.Combine(Directory.GetCurrentDirectory(), name);

        if (Directory.Exists(projectDir) && Directory.EnumerateFileSystemEntries(projectDir).Any())
        {
            Console.Error.WriteLine($"错误：目录 '{name}' 已存在且非空");
            return 1;
        }

        Console.WriteLine($"正在创建 VOA 项目 '{name}'...");
        Console.WriteLine($"  模板：{template}");
        Console.WriteLine($"  目标：{target}");

        switch (template.ToLowerInvariant())
        {
            case "webapp":
                create_web_app_template(projectDir, name, target);
                break;
            case "api":
                create_api_template(projectDir, name, target);
                break;
            case "dashboard":
                create_dashboard_template(projectDir, name, target);
                break;
            case "fullstack":
                create_fullstack_template(projectDir, name, target);
                break;
            case "ssr":
                create_ssr_template(projectDir, name, target);
                break;
            case "mobile":
                create_mobile_template(projectDir, name, target);
                break;
            case "console":
                create_console_template(projectDir, name, target);
                break;
            default:
                Console.Error.WriteLine(
                    $"错误：不支持的模板 '{template}'，可选：webapp / api / dashboard / fullstack / ssr / mobile / console");
                return 1;
        }

        Console.WriteLine();
        Console.WriteLine("✅ 项目创建完成！");
        Console.WriteLine();
        Console.WriteLine("  接下来：");
        Console.WriteLine($"    cd {name}");
        Console.WriteLine("    voa dev");
        Console.WriteLine();

        return 0;
    }

    #region WebApp 模板

    private static void create_web_app_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "pages"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "components"));
        Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "frontend", target));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "index.awsl"), $@"<widget>
    <div class=""voa-card"">
        <h2>{name}</h2>
        <p class=""voa-subtitle"">Welcome to {name}</p>
        <div class=""voa-row"">
            <button class=""voa-btn primary"" on:click={{increment}}>Count: {{count}}</button>
            <button class=""voa-btn"" on:click={{reset}}>重置</button>
        </div>
        <a href=""/about"" class=""voa-link"">前往关于页面</a>
    </div>
</widget>

<script>
    let count: number = 0
</script>

<style>
.voa-subtitle {{
    color: var(--voa-text2);
    margin: 8px 0 16px;
    font-size: 14px;
}}
.voa-link {{
    color: #58a6ff;
    text-decoration: none;
    font-size: 14px;
}}
.voa-link:hover {{
    text-decoration: underline;
}}
</style>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "about.awsl"), $@"<widget>
    <div class=""voa-card"">
        <h2>关于 {name}</h2>
        <p class=""voa-subtitle"">这是一个使用 Asgard VOA 框架构建的全栈应用。</p>
        <a href=""/"" class=""voa-link"">返回首页</a>
    </div>
</widget>

<style>
.voa-subtitle {{
    color: var(--voa-text2);
    margin: 8px 0 16px;
    font-size: 14px;
}}
.voa-link {{
    color: #58a6ff;
    text-decoration: none;
    font-size: 14px;
}}
.voa-link:hover {{
    text-decoration: underline;
}}
</style>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "layout.awsl"), @"<widget>
    <div class=""voa-layout"">
        <header class=""voa-header"">
            <nav class=""voa-nav"">
                <a href=""/"" class=""voa-nav-link"">首页</a>
                <a href=""/about"" class=""voa-nav-link"">关于</a>
            </nav>
        </header>
        <main class=""voa-main"">
            <slot />
        </main>
    </div>
</widget>

<style>
.voa-layout {
    min-height: 100vh;
    display: flex;
    flex-direction: column;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    background: #0d1117;
    color: #e6edf3;
}
.voa-header {
    background: #161b22;
    border-bottom: 1px solid #30363d;
    padding: 0 16px;
    height: 48px;
    display: flex;
    align-items: center;
}
.voa-nav {
    display: flex;
    gap: 16px;
}
.voa-nav-link {
    color: #e6edf3;
    text-decoration: none;
    font-size: 14px;
    font-weight: 500;
}
.voa-nav-link:hover {
    color: #58a6ff;
}
.voa-main {
    flex: 1;
    padding: 24px;
}
</style>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "main.v"), $@"# {name} 应用入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region API 模板

    private static void create_api_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "services"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "models"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "backend", target, 3000));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "models.v"), $@"# {name} 数据模型

struct User {{
    id: i64
    name: string
    email: string
}}

struct ApiResponse {{
    status: i32
    data: string
    error: string
}}
");

        File.WriteAllText(Path.Combine(projectDir, "source", "services", "user-service.v"), $@"# 用户服务

import {name}.models

micro get_user(id: i64): ApiResponse {{
    let user = User {{
        id: id,
        name: ""Asgardian"",
        email: ""asgard@valhalla.net""
    }}
    return ApiResponse {{
        status: 200,
        data: user.name,
        error: """"
    }}
}}

micro health_check(): ApiResponse {{
    return ApiResponse {{
        status: 200,
        data: ""ok"",
        error: """"
    }}
}}
");

        File.WriteAllText(Path.Combine(projectDir, "source", "app.v"), $@"# {name} API 服务入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region Dashboard 模板

    private static void create_dashboard_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "pages"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "components"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "services"));
        Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "frontend", target));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "index.awsl"), $@"<widget>
    <div class=""ys-app"">
        <div class=""ys-topbar"">
            <span class=""ys-logo"">◆ {name}</span>
            <div class=""ys-topbar-right"">
                <span class=""ys-status-dot""></span>
                <span class=""ys-status-text"">在线</span>
            </div>
        </div>
        <div class=""ys-body"">
            <div class=""ys-sidebar"">
                <div class=""ys-sidebar-header""><h3>导航</h3></div>
                <div class=""ys-nav-list"">
                    <div class=""ys-nav-item active"">概览</div>
                    <div class=""ys-nav-item"">数据</div>
                    <div class=""ys-nav-item"">设置</div>
                </div>
            </div>
            <div class=""ys-main"">
                <div class=""ys-stats"">
                    <div class=""ys-stat-card"">
                        <span class=""ys-stat-value"">{{users}}</span>
                        <span class=""ys-stat-label"">用户数</span>
                    </div>
                    <div class=""ys-stat-card"">
                        <span class=""ys-stat-value"">{{requests}}</span>
                        <span class=""ys-stat-label"">请求数</span>
                    </div>
                    <div class=""ys-stat-card"">
                        <span class=""ys-stat-value"">{{uptime}}</span>
                        <span class=""ys-stat-label"">运行时间</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</widget>

<script>
    let users: string = ""1,234""
    let requests: string = ""56.7K""
    let uptime: string = ""99.9%""
</script>

<style>
:root {{
    --ys-bg: #0d1117;
    --ys-card: #161b22;
    --ys-border: #21262d;
    --ys-hover: #161b22;
    --ys-active: #1c2333;
    --ys-text: #e6edf3;
    --ys-text2: #8b949e;
    --ys-text3: #6e7681;
    --ys-accent: #58a6ff;
    --ys-success: #3fb950;
    --ys-r: 6px;
}}

.ys-app {{
    display: flex;
    flex-direction: column;
    height: 100vh;
    background: var(--ys-bg);
    color: var(--ys-text);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}}

.ys-topbar {{
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 16px;
    height: 48px;
    border-bottom: 1px solid var(--ys-border);
    background: var(--ys-card);
}}

.ys-topbar-right {{
    display: flex;
    align-items: center;
    gap: 8px;
}}

.ys-logo {{
    font-size: 15px;
    font-weight: 700;
    color: var(--ys-accent);
}}

.ys-status-dot {{
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--ys-success);
}}

.ys-status-text {{
    font-size: 12px;
    color: var(--ys-text3);
}}

.ys-body {{
    display: flex;
    flex: 1;
}}

.ys-sidebar {{
    width: 220px;
    border-right: 1px solid var(--ys-border);
    background: var(--ys-bg);
}}

.ys-sidebar-header {{
    padding: 16px;
    border-bottom: 1px solid var(--ys-border);
}}

.ys-sidebar-header h3 {{
    font-size: 12px;
    color: var(--ys-text3);
    text-transform: uppercase;
    margin: 0;
}}

.ys-nav-list {{
    padding: 8px;
}}

.ys-nav-item {{
    padding: 8px 12px;
    border-radius: var(--ys-r);
    font-size: 13px;
    color: var(--ys-text2);
    cursor: pointer;
}}

.ys-nav-item:hover {{
    background: var(--ys-hover);
}}

.ys-nav-item.active {{
    background: var(--ys-active);
    color: var(--ys-text);
}}

.ys-main {{
    flex: 1;
    padding: 24px;
}}

.ys-stats {{
    display: flex;
    gap: 16px;
}}

.ys-stat-card {{
    background: var(--ys-card);
    border: 1px solid var(--ys-border);
    border-radius: 8px;
    padding: 20px;
    min-width: 160px;
    display: flex;
    flex-direction: column;
    gap: 4px;
}}

.ys-stat-value {{
    font-size: 28px;
    font-weight: 700;
    color: var(--ys-accent);
}}

.ys-stat-label {{
    font-size: 12px;
    color: var(--ys-text3);
}}
</style>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "services", "api.v"), @"# Dashboard API 服务

micro fetch_stats(): string {
    return ""ok""
}
");

        File.WriteAllText(Path.Combine(projectDir, "source", "main.v"), $@"# {name} Dashboard 入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region Fullstack 模板

    private static void create_fullstack_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);

        File.WriteAllText(Path.Combine(projectDir, "voa.workspace.v"), @"{
    projects: [
        { name: ""frontend"", path: ""frontend"" }
        { name: ""backend"", path: ""backend"" }
        { name: ""shared"", path: ""shared"" }
    ]
}
");

        var frontendDir = Path.Combine(projectDir, "frontend");
        create_web_app_template(frontendDir, $"{name}-frontend", target);

        var backendDir = Path.Combine(projectDir, "backend");
        create_api_template(backendDir, $"{name}-backend", "clr");

        var sharedDir = Path.Combine(projectDir, "shared");
        Directory.CreateDirectory(sharedDir);
        Directory.CreateDirectory(Path.Combine(sharedDir, "source"));
        Directory.CreateDirectory(Path.Combine(sharedDir, "dist"));

        File.WriteAllText(Path.Combine(sharedDir, "voa.config.v"),
            generate_config($"{name}-shared", "library", target));
        File.WriteAllText(Path.Combine(sharedDir, "legion.von"), generate_manifest($"{name}-shared"));

        File.WriteAllText(Path.Combine(sharedDir, "source", "models.v"), $@"# {name} 共享类型定义

struct User {{
    id: i64
    name: string
    email: string
}}

struct ApiResponse {{
    status: i32
    data: string
    error: string
}}
");
    }

    #endregion

    #region SSR 模板

    private static void create_ssr_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "pages"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "frontend", target));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "index.awsl"), $@"<widget>
    <div class=""voa-card"">
        <h2>{name} (SSR)</h2>
        <p class=""voa-subtitle"">服务端渲染应用</p>
    </div>
</widget>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "main.v"), $@"# {name} SSR 应用入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region Mobile 模板

    private static void create_mobile_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "source", "pages"));
        Directory.CreateDirectory(Path.Combine(projectDir, "assets"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "frontend", target));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "pages", "index.awsl"), $@"<widget>
    <div class=""voa-card"">
        <h2>{name} (Mobile)</h2>
        <p class=""voa-subtitle"">移动端应用</p>
    </div>
</widget>
");

        File.WriteAllText(Path.Combine(projectDir, "source", "main.v"), $@"# {name} 移动端应用入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region Console 模板

    private static void create_console_template(string projectDir, string name, string target)
    {
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "source"));
        Directory.CreateDirectory(Path.Combine(projectDir, "dist"));

        File.WriteAllText(Path.Combine(projectDir, "voa.config.v"), generate_config(name, "console", target));
        File.WriteAllText(Path.Combine(projectDir, "legion.von"), generate_manifest(name));

        File.WriteAllText(Path.Combine(projectDir, "source", "main.v"), $@"# {name} 控制台应用入口

micro main(): i32 {{
    return 0
}}
");
    }

    #endregion

    #region 配置生成

    private static string generate_config(string name, string type, string target, int serverPort = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    project_type: \"{type}\",");
        sb.AppendLine($"    target: \"{target}\",");
        sb.AppendLine($"    default_render_mode: {(type == "frontend" ? "\"csr\"" : "null")},");

        if (type == "frontend")
        {
            sb.AppendLine();
            sb.AppendLine("    server: {");
            sb.AppendLine("        host: \"localhost\",");
            sb.AppendLine("        port: 3000,");
            sb.AppendLine("    },");
            sb.AppendLine();
            sb.AppendLine("    routes: [");
            sb.AppendLine("        { path: \"/\",      mode: \"csr\" },");
            sb.AppendLine("        { path: \"/about\", mode: \"csr\" },");
            sb.AppendLine("    ],");
        }

        sb.AppendLine();
        sb.AppendLine("    build: {");
        sb.AppendLine("        output: \"dist\",");
        sb.AppendLine("    },");
        sb.AppendLine();
        sb.AppendLine("    hot_reload: {");
        sb.AppendLine("        enabled: true,");
        sb.AppendLine("    },");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string generate_manifest(string name)
    {
        var sb = new StringBuilder();
        sb.AppendLine("legion {");
        sb.AppendLine($"    name: \"{name}\",");
        sb.AppendLine("    version: \"0.1.0.0\",");
        sb.AppendLine("    description: \"\",");
        sb.AppendLine("    license: \"MIT\",");
        sb.AppendLine("    dependencies: {},");
        sb.AppendLine("    devDependencies: {},");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #endregion
}