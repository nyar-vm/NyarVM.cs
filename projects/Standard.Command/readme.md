# Sonic.Standard.Command attribute 驱动架构说明

## 目标

`Sonic.Standard.Command` 的正式方向为 **类似 `rust clap` 的 attribute 驱动**。

这意味着：

- 命令类型自己声明命令名、描述、参数、选项、子命令
- `Program.cs` 只负责 bootstrap，不再手写巨大的注册树
- 帮助、补全、配置回填、中间件挂载都围绕同一份命令模型展开
- 运行时 builder DSL 保留，但退为兼容层，不再是仓库 CLI 的默认接入方式

## 正式入口

### `CommandApp.run<T>()`

attribute 命令的默认入口：

```csharp
return CommandApp.run<AtlasRootCommand>(args);
```

或：

```csharp
return await CommandApp.run_async<AtlasRootCommand>(args);
```

`T` 是根命令类型，负责承载：

- 根命令元数据
- 全局选项
- 子命令树
- 实际执行入口

### `CommandHostBuilder.use_command<T>()`

需要完整宿主能力时：

```csharp
var host = new CommandHostBuilder()
    .use_configuration(config => config
        .add_json_file("atlas.json")
        .add_environment_variables("ATLAS_"))
    .use_command<AtlasRootCommand>("atlas")
    .build();
```

适合场景：DI、配置、中间件、生命周期钩子、Shell / I/O 注入。

## 命令模型

### 命名空间约定

`Core.Command` 和 `Core.Terminal` 存在同名类型（`CommandAttribute`、`OptionAttribute` 等），同时导入会导致 CS0104 歧义。正确做法：

```csharp
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;
```

**禁止** `using Core.Terminal;` 与 `using Core.Command;` 同时出现。

### 1. 命令

命令类型使用 `[Command]` 声明，实现 `Core.Command.ICommand` 接口：

```csharp
using Core.Command;
using Core.Command.Option;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

[Command("check", "执行检查")]
internal sealed class CheckCommand : ICommand
{
    [Argument(0, "目标路径", required = true)]
    public string path { get; set; } = string.Empty;

    [Option('v', "verbose", "详细输出")]
    public bool verbose { get; set; }

    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine($"check {path}, verbose={verbose}");
        return Task.FromResult(ExitCode.Success);
    }
}
```

**注意**：不要继承 `BaseCommand`，它的 `execute` 签名与 `ICommand` 不兼容。直接实现 `ICommand` 即可。

### 2. 位置参数

位置参数使用 `ArgumentAttribute`：

- `position` — 参数位置（从 0 开始）
- `description` — 参数描述
- `required` — 是否必填

```csharp
[Argument(0, "目标路径", required = true)]
public string path { get; set; } = string.Empty;
```

### 3. 命名选项

命名选项使用 `OptionAttribute`：

- `short_name` — 短选项（如 `'v'`）
- `long_name` — 长选项（如 `"verbose"`）
- `description` — 选项描述
- `alias` — 别名
- `environment_variable` — 环境变量回填
- `config_key` — 配置键回填

```csharp
[Option('o', "output", "构建输出目录")]
public string output { get; set; } = "dist";

[Option("config", "配置文件路径", alias = ["config-file"], environment_variable = "ATLAS_CONFIG")]
public string? config { get; set; }
```

### 4. 子命令

子命令通过 `[Subcommand]` 挂到根命令上：

```csharp
using Core.Command;
using ExitCode = Core.Terminal.ExitCode;
using ICommand = Core.Command.ICommand;

[Command("atlas", "管理后端基础设施 Schema 与部署")]
internal sealed class AtlasRootCommand : ICommand
{
    [Subcommand]
    public AtlasGenerateCommand? generate { get; set; }

    [Subcommand]
    public AtlasDeployCommand? deploy { get; set; }

    [Option('v', "verbose", "启用详细输出")]
    public bool verbose { get; set; }

    public Task<ExitCode> execute(ICommandContext context, CancellationToken cancel)
    {
        Console.WriteLine("使用 'atlas --help' 查看可用命令");
        return Task.FromResult(ExitCode.Success);
    }
}
```

正式结构应尽量接近 `clap`：

- 一个根命令类型
- 多个独立子命令类型
- 命令元数据与处理逻辑放在同一个类型里

## 解析与执行

### 当前基础设施

- `CliArgumentParser` — attribute 解析器（SourceGen 优先，反射回退）
- `CommandApp.run<T>()` / `run_async<T>()` — attribute 入口
- `CommandHostBuilder.use_command<T>()` — 宿主入口
- `HelpRenderer` — attribute 模式帮助生成
- `HelpGenerator` — builder 模式帮助生成
- `CompletionScriptGenerator` — 补全脚本生成
- `CommandConfiguration` — 配置系统

`CliArgumentParser` 已覆盖的能力：

- 位置参数
- 长选项和短选项
- `--key=value`
- 子命令路由
- 环境变量回填
- `CommandConfiguration` 回填
- 基本范围校验
- SourceGen `__Parse` 钩子

### 正式要求

- `CommandApp.run<T>()` 以 attribute 解析为主路径
- SourceGen 优先，反射回退
- 帮助、补全、配置都从同一份 attribute 元数据导出

## 帮助、补全与配置

### 帮助

帮助文本由标准库统一生成，不再让每个工具自己拼接 usage。

- attribute 模式：`HelpRenderer`
- builder 模式：`HelpGenerator`

### 补全

补全脚本使用 `CompletionScriptGenerator`，数据来源切到 attribute 命令树。

当前支持目标：`bash` / `zsh` / `powershell` / `fish`

### 配置

统一配置入口：`CommandConfiguration`

可复用方法：`add_json_file()` / `add_environment_variables()` / `add_command_line()` / `set()` / `get()` / `get<T>()` / `bind<T>()`

命令选项通过 attribute 上的 `environment_variable` / `config_key` 与它对接。

## 兼容层

以下 API 保留，但降级为兼容层：

- `CommandRegistryBuilder`
- `Builder.CommandConfig`
- `Builder.CommandBuilder`

定位：旧工具迁移桥接、原型验证、小型测试场景。

**不再** 是仓库 CLI 的正式默认方案。

## 推荐接入模式

### 模式 A：仓库 CLI 默认方案

所有工具使用根命令类型启动：

```csharp
using Std.Command;

internal static class Program
{
    private static int Main(string[] args)
    {
        CommandApp.with_name("atlas");
        CommandApp.with_version("0.1.0");

        return CommandApp.run<AtlasRootCommand>(args);
    }
}
```

### 模式 B：复杂工具方案

复杂工具通过宿主模式接入同一份根命令类型：

```csharp
using Std.Command.Hosting;

var host = new CommandHostBuilder()
    .use_configuration(config => config
        .add_json_file("atlas.json")
        .add_environment_variables("ATLAS_"))
    .use_command<AtlasRootCommand>("atlas")
    .build();
```

### 模式 C：builder DSL 兼容模式

builder DSL 只作为过渡期兼容层保留：

```csharp
return CommandApp.run_sync(args, registry =>
{
    registry.add("legacy", () => 0);
});
```

新代码不应再以此作为默认起点。

## 仓库工具迁移状态

以下工具已全部迁移到 attribute-first：

| 工具 | 根命令 | 子命令 | Program.cs | 编译 |
|:---|:---|:---|:---|:---|
| `vcc` | `VccRootCommand` | compile / run / test / repl / format / check / diag / disasm | bootstrap | 通过 |
| `legion` | `LegionRootCommand` | build / clean / run / check / lint / fmt / doc / test / bench / coverage | bootstrap | 通过 |
| `nargo` | `NargoRootCommand` | init / import / install / dev / build / graph / doctor / publish | bootstrap | 1 预存错误 |
| `atlas` | `AtlasRootCommand` | dev / save / load / diff / status / env / generate / publish / deploy | bootstrap | 预存错误 |
| `hermes` | `HermesRootCommand` | generate / validate / diff / init | bootstrap | 预存错误 |
| `legend` | `LegendRootCommand` | eval / run / build / list | bootstrap | 预存错误 |
| `voa` | `VoaRootCommand` | dev / start / build / clean / new / check / fmt / test / init / benchmark / coverage | bootstrap | 预存错误 |

注：预存错误均为上游缺失类型（`SchemaIR`、`LegacyVmRunner`、`Valkyrie.Asgard.DevServer` 等），与本次迁移无关。

## 禁止继续扩散的风格

- 大型 `registry.add(...)` 注册树
- 在 `Program.cs` 内堆积全部命令定义
- 把 builder DSL 写成 README 默认示例
- 手写帮助系统或补全系统
- 为单个工具维护平行配置优先级规则
- `using Core.Terminal;` 与 `using Core.Command;` 同时出现

## 总结

`Sonic.Standard.Command` 的正式方向是"命令类型 + attribute 元数据优先"。

后续所有 CLI 工具都应像 `rust clap` 一样，把命令结构写回类型本身，再由标准库统一提供解析、帮助、补全、配置与宿主能力。
