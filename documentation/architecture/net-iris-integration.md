# Sonic + Iris 整合方案

> **文档状态**：M0 产出，对应路线图 [14-sonic-team.md](file:///e:/RiderProjects/.trae/roadmaps/14-sonic-team.md) 子领域 D-15

## 一、整合目标

使 Sonic 应用能够：
1. **自动注册 Iris CLI 命令**——Sonic 应用启动后，Iris CLI 框架的命令自动可用
2. **集成 Iris.Interactive 终端输出**——Sonic 日志和状态信息通过 Iris 终端美化输出
3. **`sonic new` 项目脚手架**——通过 DejaVu 模板引擎一键生成新项目

## 二、整合架构

```
┌──────────────────────────────────────────────────────┐
│              Sonic CLI (sonic 命令)                    │
│  ┌────────────────────────────────────────────────┐  │
│  │           Iris CLI Framework                   │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────┐ │  │
│  │  │ 命令解析  │  │ 参数绑定  │  │ 帮助生成     │ │  │
│  │  └──────────┘  └──────────┘  └──────────────┘ │  │
│  └────────────────────────────────────────────────┘  │
│                         │                             │
│  ┌────────────────────────────────────────────────┐  │
│  │          Sonic CLI Commands (注册层)           │  │
│  │  ┌────────┐ ┌────────┐ ┌──────┐ ┌──────────┐ │  │
│  │  │  new   │ │  dev   │ │deploy│ │ generate │ │  │
│  │  └────────┘ └────────┘ └──────┘ └──────────┘ │  │
│  └────────────────────────────────────────────────┘  │
│                         │                             │
│  ┌────────────────────────────────────────────────┐  │
│  │       Iris.Interactive (终端输出)               │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────┐ │  │
│  │  │ 进度条    │  │ 表格渲染  │  │ 颜色主题     │ │  │
│  │  └──────────┘  └──────────┘  └──────────────┘ │  │
│  └────────────────────────────────────────────────┘  │
│                         │                             │
│  ┌────────────────────────────────────────────────┐  │
│  │      DejaVu Template Engine (模板引擎)          │  │
│  │  ┌──────────────────────────────────────────┐  │  │
│  │  │ 项目模板 (.dejavu) → 生成项目文件          │  │  │
│  │  └──────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

## 三、Iris CLI 命令自动注册

### 3.1 命令体系

| 命令 | 说明 | 委托团队 |
|:---|:---|:---|
| `sonic new` | 创建新项目 | DejaVu（模板） |
| `sonic dev` | 启动开发服务器 | Iris（终端） |
| `sonic generate` | 代码生成 | Hermes + Sonic.Generator |
| `sonic deploy` | 部署到目标环境 | DejaVu.Deploy |
| `sonic diff` | 数据库 Schema 差异 | Hermes.Migrator |
| `sonic publish` | 发布到生产环境 | DejaVu.Deploy |
| `sonic load` | 数据加载 | Hermes.ORM |
| `sonic save` | 数据备份 | Hermes.ORM |
| `sonic status` | 项目状态 | — |
| `sonic env` | 环境管理 | DejaVu.Config |

### 3.2 命令注册机制

```csharp
// Sonic.CLI/Program.cs 中的自动注册
public static async Task Main(string[] args)
{
    var app = new CommandApp();

    // Iris CLI 框架自动扫描 ISonicCommand 实现
    app.Configure(config =>
    {
        config.ScanCommands(typeof(Program).Assembly);
        config.SetApplicationName("sonic");
    });

    await app.RunAsync(args);
}
```

### 3.3 命令定义示例

```csharp
[Command("dev", Description = "启动 Sonic 开发服务器")]
public class DevCommand : ISonicCommand
{
    public async Task ExecuteAsync(CommandContext context)
    {
        // 使用 Iris.Interactive 美化输出
        using var spinner = context.CreateSpinner("正在启动 Sonic 服务器...");
        var host = NetHost.Create();
        await host.StartAsync();
        spinner.Success("Sonic 服务器已启动");
        await host.WaitForShutdownAsync();
    }
}
```

## 四、Iris.Interactive 终端输出集成

### 4.1 集成点

| Sonic 功能 | Iris.Interactive 集成 | 说明 |
|:---|:---|:---|
| 服务器启动 | `Spinner` / `ProgressBar` | 显示启动进度 |
| 请求日志 | 彩色表格渲染 | 格式化 HTTP 请求日志 |
| 健康检查 | 状态图标（✅/❌） | Liveness/Readiness 状态 |
| 定时任务 | 执行状态表格 | 任务名称 + 上次运行时间 + 下次运行时间 |
| 错误输出 | 红色错误面板 | 异常详情格式化 |
| Schema 同步 | 差异对比表格 | `sonic diff` 结果 |

### 4.2 日志输出适配

```csharp
// Sonic 日志适配器 → Iris.Interactive
public class IrisLogAdapter : ISonicLogger
{
    private readonly IIrisTerminal _terminal;

    public IrisLogAdapter(IIrisTerminal terminal)
    {
        _terminal = terminal;
    }

    public void Log(LogLevel level, string message)
    {
        switch (level)
        {
            case LogLevel.Information:
                _terminal.WriteInfo(message);
                break;
            case LogLevel.Warning:
                _terminal.WriteWarning($"⚠ {message}");
                break;
            case LogLevel.Error:
                _terminal.WriteError($"✖ {message}");
                break;
        }
    }
}
```

### 4.3 终端主题

| 元素 | 颜色 | 说明 |
|:---|:---|:---|
| 成功信息 | 绿色 | ✅ 操作成功 |
| 警告信息 | 黄色 | ⚠ 需要注意 |
| 错误信息 | 红色 | ✖ 操作失败 |
| 进度指示 | 青色旋转 | 正在进行中 |
| 标题 | 白色加粗 | 分区标题 |

## 五、`sonic new` 项目脚手架

### 5.1 脚手架流程

```
$ sonic new my-api --template webapi

1. Iris CLI 解析命令和参数
2. Sonic.CLI 调用 DejaVu 模板引擎
3. DejaVu 选择模板 → webapi.dejavu
4. 模板引擎渲染项目文件：
   ├── my-api/
   │   ├── schema.he                  # Hermes Schema 模板
   │   ├── sonic.von                  # Sonic 配置模板
   │   ├── src/
   │   │   └── Controllers/
   │   │       └── HomeController.cs  # 控制器模板
   │   ├── my-api.csproj              # 项目文件模板
   │   └── .gitignore                 # Git 忽略模板
5. Iris.Interactive 显示创建进度
6. 提示后续步骤（cd my-api && sonic dev）
```

### 5.2 内置模板

| 模板 | 说明 | 适用场景 |
|:---|:---|:---|
| `webapi` | RESTful API 项目 | 标准后端服务 |
| `websocket` | WebSocket 项目 | 实时通信服务 |
| `minimal` | 最小化项目 | 简单脚本/微服务 |
| `grpc` | gRPC 服务项目 | RPC 服务 |
| `fullstack` | 全栈项目（含 VOA 前端） | BFF 应用 |

### 5.3 技术方案

- **模板引擎**：DejaVu (`Hermes.Dejavu`)，已存在于 `Olymp.Hermes/projects/Hermes.Dejavu/`
- **模板格式**：`.dejavu` 模板文件，支持变量替换和条件渲染
- **项目结构**：遵循 Sonic 目录结构约定（见架构文档）

## 六、与 Iris 团队的接口契约

| 接口 | 提供方 | 消费方 | 说明 |
|:---|:---|:---|:---|
| `Iris.CLI.ICommandApp` | 13-Iris | Sonic.CLI | CLI 应用框架 |
| `Iris.CLI.CommandAttribute` | 13-Iris | Sonic.CLI | 命令属性标记 |
| `Iris.Interactive.IIrisTerminal` | 13-Iris | Sonic | 终端交互接口 |
| `Iris.Interactive.Spinner` | 13-Iris | Sonic | 进度指示器 |

## 七、与 DejaVu 团队的接口契约

| 接口 | 提供方 | 消费方 | 说明 |
|:---|:---|:---|:---|
| `Dejavu.IDejaVuTemplateEngine` | 12-Hermes & DejaVu | Sonic.CLI | 模板引擎 |
| `Dejavu.Deploy.IDeployer` | 12-Hermes & DejaVu | Sonic.CLI | 部署接口 |

## 八、红线约束

| 红线 | 遵守方式 |
|:---|:---|
| Sonic 不做 CLI 框架 | Sonic.CLI 仅定义命令实现，底层 CLI 框架由 Iris 提供 |
| Sonic 不做 TUI 框架 | 终端交互和渲染全部通过 `IIrisTerminal` 接口委托 Iris.Interactive |
| Sonic 不做模板引擎 | 项目脚手架通过 DejaVu 模板引擎生成，Sonic 仅提供模板内容和参数 |
| Sonic 不绕过 Iris | 所有 CLI 和终端输出功能必须通过 Iris 提供的接口 |
