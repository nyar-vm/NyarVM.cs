# Sonic.Configuration 完整设计

## 摘要

Sonic 是一个面向 .NET 的现代化标准库，旨在为配置管理提供极致简洁、编译时安全、零样板代码的解决方案。Sonic.Configuration
是整个标准库的配置子系统，其核心抽象为 `IConfigurable` 接口，设计范式围绕单一标记 `[Config]` 展开。开发者只需在 partial
类上标注 `[Config]`，所有配置加载、多源优先级合并、集合策略、验证、JSON Schema 生成、热加载、依赖注入集成、CLI 绑定等复杂逻辑均由
Sonic 的 Source Generator 在编译时自动生成。本文档详细阐述 Sonic.Configuration 的设计哲学、架构体系、API
设计、生成代码结构、配置源模型、合并语义、验证框架、Schema 生成机制、环境切换、CLI 集成、性能考量及完整使用示例，总篇幅约 20000
字，为 Sonic 配置库的实现提供权威蓝图。

---

## 第一章：引言

### 1.1 配置管理的困境

在当代应用程序开发中，配置管理始终是一个看似简单却极易演变为“代码泥潭”的领域。开发者面对的现实是：

- 配置数据散落在 JSON、YAML、TOML、环境变量、命令行参数、远程配置中心等多种来源。
- 每个配置项都需要在代码中定义键名、类型、默认值，并手写读取、转换、合并、验证的样板代码。
- 环境切换（开发、测试、生产）往往需要冗长的条件逻辑和文件路径拼接。
- 配置结构的文档化、编辑器智能提示、敏感数据脱敏等需求常常被忽视或临时修补。

这些问题的根源在于： **配置不是类型**。在传统 .NET 配置模型中，`IConfiguration` 是一个扁平的字符串字典，开发者需要手动将它与强类型对象映射，于是样板代码不断滋生。

其他语言生态提供了诸多启发：

- **Rust** 的 `serde` 与 `config` crate：通过 `#[derive(Deserialize)]` 让结构体自动获得反序列化能力，配合 `builder`
  模式组合多种源。
- **Python** 的 `pydantic`：依靠类型标注和装饰器在运行时提供解析、验证和 Schema 导出。
- **TypeScript** 的 `zod`：运行时类型定义与验证一体化，彻底消灭类型转换的样板。
- **Kotlin** 的 `Hocon`：支持层次化配置的深度合并。

这些项目的共同点在于： **声明式定义数据结构，工具自动处理细节**。Sonic.Configuration 正是要将这一理念引入 .NET，并利用 C# 的
Source Generator 技术将其推向极致。

### 1.2 Sonic 的使命

Sonic 是一个追求“极简表面、极强内力”的标准库。它的设计原则是：

- **极简 API**：开发者只需了解一个标记 `[Config]` 和一个接口 `IConfigurable`。
- **编译时生成**：所有繁琐的代码都由 Source Generator 在编译时产生，无运行时反射，性能与手写代码无异。
- **类型安全**：配置键名、类型、默认值、验证规则全部是编译时确定的，拼写错误会立即触发编译错误。
- **可组合性**：配置源的组合采用函数式的 `Merge` 和 `Select` 原语，优先级一目了然。
- **全生命周期**：不仅关注启动时的加载，还覆盖热加载、环境切换、脱敏、文档生成等完整生命周期。

Sonic.Configuration 是 Sonic 库中负责配置管理的子系统。它的核心抽象 `IConfigurable` 将配置对象变为一个具备自我验证、自我描述、自我复制能力的“智能体”。而
`[Config]` 标记则是一切的入口，驱动 Source Generator 完成所有魔法。

---

## 第二章：核心设计哲学

### 2.1 配置的本质

配置管理的本质可以提炼为两个基本问题：

1. **缺省处理**：当某个配置源没有提供某字段时，应该使用什么值？
2. **冲突解决**：当多个源提供了同一字段时，按照怎样的优先级决定有效值？

然而，当字段类型从简单标量扩展到列表、字典、嵌套对象时，“冲突”的含义变得丰富：

- 列表是全部替换，还是追加新元素，还是按唯一键去重？
- 字典是整体替换，还是浅层覆盖键值，还是递归深度合并？
- 嵌套对象是完全替换，还是仅仅覆盖其内部部分字段？

Sonic.Configuration 将 **合并策略** 提升为配置元数据的一等公民，并为其提供合理的默认行为，同时允许按需定制。

### 2.2 声明式与推断式

传统的显式注解模式（如 `[ConfigProperty("timeout")]`）提供了完全的控制力，但也带来了书写成本。Sonic 选择了一条更极简的道路：
**推断式声明**。在 `[Config]` 标记下，属性名本身就是配置键名（可自动转换为 camelCase、snake_case
等约定），属性的初始化器就是默认值，属性的类型决定了基本验证和类型转换逻辑。

这种设计背后的理念是： **约定优于配置**。大多数情况下，开发者的命名已经足够清晰，无需重复。仅当特殊需求出现时（如配置键名与属性名不同，或需要非默认合并策略），才需要附加少量
Attribute。这使得零样本代码成为可能，且不会牺牲灵活性。

### 2.3 可组合的配置源

配置源被抽象为 `IConfigProvider` 接口，它只暴露一个方法：加载并返回一个统一的配置树结构 `ConfigNode`
。不同的源（JSON、TOML、环境变量、CLI 等）都实现该接口。

优先级组合通过两个高阶组合子实现：

- **`Merge(A, B)`**：B 的优先级高于 A，B 中的值会按照各字段的合并策略覆盖 A。
- **`Select(A, B)`**：字段级回退，如果 A 中不存在该字段，则从 B 获取；否则使用 A。

这两个组合子让配置源的编排像搭积木一样直观，且可以无限嵌套。

### 2.4 智能配置对象：IConfigurable

`IConfigurable` 接口是 Sonic.Configuration 的另一块基石。它定义了四个基础能力：

- **Validate ()**：返回所有验证错误，空集合表示合法。
- **GetSchema ()**：返回 JSON Schema 字符串，用于编辑器支持和文档生成。
- **Clone ()**：深克隆当前配置，用于快照和变更追踪。
- **ResetToDefaults ()**：将配置重置为编译时默认值。

这让配置对象从一个被动的数据容器，变成了一个可自我诊断、可安全复制、可重置的智能体，贯穿应用的整个生命周期。

---

## 第三章：整体架构

Sonic.Configuration 的架构分为三个层次：核心抽象层、编译时生成层、运行时提供与集成层。

### 3.1 核心抽象层

- **`[Config]` Attribute**：标记在 partial 类上，声明该类为配置节。
- **`IConfigurable` 接口**：配置对象的标准契约。
- **`IConfigProvider` 接口**：配置源抽象。
- **`ConfigNode` 类型**：轻量级配置树节点，屏蔽不同数据格式的差异。
- **`ConfigBuilder<T>` 类**：通用构建器，负责按顺序应用源并产出配置。
- **`CollectionMergeStrategy` 枚举**：定义集合合并策略。

### 3.2 编译时生成层

- **SonicConfigGenerator**：一个增量 Source Generator，它扫描所有带 `[Config]` 的类，为每个类生成：
  - 一个嵌套的 `Builder` 类（包含 `ApplySource` 方法，实现字段赋值与策略处理）
  - `IConfigurable` 的完整实现
  - 静态工厂方法（`FromJson`、`FromEnvironment` 等）
  - JSON Schema 作为嵌入资源或常量字符串
  - 可选的 Markdown 文档和示例配置文件

### 3.3 运行时提供与集成层

- **内建提供者**：`JsonConfigProvider`、`TomlConfigProvider`、`YamlConfigProvider`、`EnvironmentConfigProvider`、
  `CliConfigProvider`、`MemoryConfigProvider`。
- **环境与 CLI 绑定**：通过简单的 API 接入环境变量和命令行参数。
- **依赖注入集成**：`AddSonicConfig<T>` 扩展方法，将配置注册到 `IServiceCollection`，支持 `IOptions<T>`、
  `IOptionsSnapshot<T>`、`IOptionsMonitor<T>` 模式。
- **热加载**：基于 `FileSystemWatcher` 的 `BuildWithReload` 方法，返回 `IConfigWatcher<T>`。
- **错误处理**：聚合验证异常、配置加载异常的统一处理。

---

## 第四章：配置标记 [Config]

### 4.1 标记定义

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConfigAttribute : Attribute
{
    /// <summary>
    /// 指定配置源中的节名。如果未设置，将使用类名转换为 camelCase。
    /// </summary>
    public string? SectionName { get; init; }

    /// <summary>
    /// 指定属性到配置键名的命名约定，默认为 CamelCase。
    /// </summary>
    public NamingConvention Naming { get; init; } = NamingConvention.CamelCase;
}

public enum NamingConvention
{
    CamelCase,      // Registry -> registry
    PascalCase,     // Registry -> Registry
    SnakeCase,      // RegistryEndpoints -> registry_endpoints
    KebabCase,      // RegistryEndpoints -> registry-endpoints
    LowerCase,      // Registry -> registry (与 CamelCase 可能相同)
    UpperCase       // REGISTRY
}
```

### 4.2 使用方式

```csharp
[Config(SectionName = "app", Naming = NamingConvention.CamelCase)]
public partial class AppConfig
{
    public string Registry { get; set; } = "https://default.registry";
    public int TimeoutSeconds { get; set; } = 30;
    public List<string> Mirrors { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
}

[Config]
public partial class DatabaseConfig
{
    public string ConnectionString { get; set; } = "";
    public int MaxPoolSize { get; set; } = 100;
}
```

就这么简单。没有 `[ConfigProperty]`，没有 `[Required]`（除非需要显式标记），一切由生成器推断。

### 4.3 推断规则

Source Generator 会为每个公开可写属性生成如下元数据：

- **配置键名**：根据 `Naming` 约定转换属性名。
- **默认值**：若属性有初始化值，则作为默认值；否则按类型默认值（`null`、`0`、`false`）。
- **类型转换**：根据属性类型生成相应的 `ConfigNode` 取值和转换代码。
- **合并策略**：
  - 简单类型（`string`、`int`、`bool` 等）：直接覆盖（`Overwrite`）。
  - `List<T>`、`IList<T>`、`ICollection<T>`：默认 `Overwrite`。
  - `Dictionary<string, T>`：默认 `ShallowMerge`（键级合并，新键添加，已有键覆盖）。
  - 标记 `[Config]` 的嵌套类型：默认 `DeepMerge`（递归合并该嵌套对象的字段）。
- **验证**：属性类型本身提供基本类型检查；可使用标准 `System.ComponentModel.DataAnnotations` 的验证特性或 Sonic
  提供的验证特性进一步约束。

### 4.4 例外覆盖

如果某个字段需要非默认行为，只需添加轻量特性：

```csharp
[Config]
public partial class AppConfig
{
    // 覆盖键名
    [Property("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    // 列表改为追加
    [Append]
    public List<string> Mirrors { get; set; } = new();

    // 显式必填（尽管有默认值，但要求源提供）
    [Required]
    public string ApiKey { get; set; }

    // 嵌套对象改为整体替换而非深度合并
    [Overwrite]
    public DatabaseConfig Database { get; set; } = new();
}
```

这些覆盖特性是可选的，绝大多数字段无需任何额外标注。

---

## 第五章：IConfigurable 接口

### 5.1 接口定义

```csharp
public interface IConfigurable
{
    /// <summary>
    /// 验证当前配置，返回所有错误消息。空集合表示验证通过。
    /// </summary>
    IReadOnlyList<string> Validate();

    /// <summary>
    /// 返回该配置对应的 JSON Schema 字符串。
    /// </summary>
    string GetSchema();

    /// <summary>
    /// 创建当前配置的深克隆副本。
    /// </summary>
    object Clone();

    /// <summary>
    /// 将所有字段重置为编译时确定的默认值。
    /// </summary>
    void ResetToDefaults();
}
```

### 5.2 生成实现

`[Config]` 类会由 Source Generator 自动实现该接口，生成类似如下的 partial 类代码：

```csharp
public partial class AppConfig : IConfigurable
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        // 自动生成的验证逻辑
        if (TimeoutSeconds < 0 || TimeoutSeconds > 300)
            errors.Add("'timeoutSeconds' 必须在 0 到 300 之间。");
        // 嵌套验证
        if (Database is IConfigurable db)
        {
            foreach (var e in db.Validate())
                errors.Add("database." + e);
        }
        return errors;
    }

    public string GetSchema() => AppConfigSchema.Json; // 内嵌资源

    public object Clone()
    {
        return new AppConfig
        {
            Registry = this.Registry,
            TimeoutSeconds = this.TimeoutSeconds,
            Mirrors = new List<string>(this.Mirrors),
            Database = (DatabaseConfig)this.Database.Clone()
        };
    }

    public void ResetToDefaults()
    {
        Registry = "https://default.registry";
        TimeoutSeconds = 30;
        Mirrors = new List<string>();
        Database = new DatabaseConfig();
    }
}
```

这些方法的内容完全由 Source Generator 根据属性元数据生成，性能等同于手写。

---

## 第六章：配置源与 ConfigNode

### 6.1 ConfigNode 设计

`ConfigNode` 是一个轻量级的抽象语法树节点，它抽象了不同数据格式的结构。定义如下：

```csharp
public abstract class ConfigNode
{
    public enum NodeType { Object, Array, Scalar, Null }

    public abstract NodeType Type { get; }

    // 标量访问
    public virtual string? AsString() => null;
    public virtual bool AsBoolean() => false;
    public virtual int AsInt32() => 0;
    // ... 其他数值类型

    // 对象访问
    public virtual ConfigNode? GetField(string name) => null;
    public virtual IEnumerable<KeyValuePair<string, ConfigNode>> EnumerateFields() => 
        Enumerable.Empty<KeyValuePair<string, ConfigNode>>();

    // 数组访问
    public virtual IEnumerable<ConfigNode> EnumerateArray() => Enumerable.Empty<ConfigNode>();
}
```

不同的提供者负责解析原始数据并构建 `ConfigNode` 树。

### 6.2 IConfigProvider 接口

```csharp
public interface IConfigProvider
{
    ConfigNode Load();
}
```

每个配置源实现该接口，返回一个 `ConfigNode` 根节点。如果加载失败（如文件不存在），可以选择抛出异常或返回一个 `Null` 节点。

### 6.3 内建配置源

Sonic 提供静态工厂类 `ConfigSource`，方便创建各种源：

```csharp
public static class ConfigSource
{
    public static IConfigProvider FromJson(string path) => new JsonConfigProvider(path);
    public static IConfigProvider FromToml(string path) => new TomlConfigProvider(path);
    public static IConfigProvider FromYaml(string path) => new YamlConfigProvider(path);
    public static IConfigProvider FromEnvironment(string prefix = "APP_") => new EnvironmentConfigProvider(prefix);
    public static IConfigProvider FromCli(string[] args) => new CliConfigProvider(args);
    public static IConfigProvider FromMemory(IDictionary<string, string> data) => new MemoryConfigProvider(data);

    // 组合子：字段级回退
    public static IConfigProvider Select(params IConfigProvider[] providers) => new FallbackConfigProvider(providers);
}
```

每个提供者的实现细节被隐藏，仅通过 `ConfigNode` 暴露数据。

---

## 第七章：配置构建器与优先级合并

### 7.1 生成的 Builder

每个 `[Config]` 类都会生成一个嵌套的 `Builder` 类，并提供静态入口方法：

```csharp
public partial class AppConfig
{
    public static Builder From(IConfigProvider provider) => new Builder().Merge(provider);
    public static Builder FromEnvironment() => From(ConfigSource.FromEnvironment());
    // ... 其他便捷方法

    public class Builder
    {
        private readonly List<IConfigProvider> _sources = new();
        private bool _validate = true;

        public Builder Merge(params IConfigProvider[] providers)
        {
            _sources.AddRange(providers);
            return this;
        }

        public Builder WithoutValidation() { _validate = false; return this; }

        public AppConfig Build()
        {
            var config = new AppConfig();
            config.ResetToDefaults();

            foreach (var source in _sources)
            {
                var node = source.Load();
                if (node.Type == ConfigNode.NodeType.Object || node.Type == ConfigNode.NodeType.Null)
                    ApplySource(config, node);
            }

            if (_validate)
            {
                var errors = config.Validate();
                if (errors.Any())
                    throw new ConfigValidationException(errors);
            }
            return config;
        }

        // 由 Source Generator 生成的核心方法
        private static void ApplySource(AppConfig target, ConfigNode source) { ... }
    }
}
```

### 7.2 Merge 与 Select 的语义

- **`Merge(A, B)`**：将 B 源的数据应用到当前配置，覆盖 A 已经设置的值。具体如何覆盖由每个字段的合并策略决定。这是层级覆盖的机制。
- **`Select(A, B)`**：在 `FallbackConfigProvider` 内部，对于每个字段的查询，优先从 A 获取，如果 A 返回 null，则从 B
  获取。这是字段级回退机制，适合“本地文件优先，全局文件兜底”的场景。

实际使用中，两者可以组合：

```csharp
var config = AppConfig.From(ConfigSource.Select(
        ConfigSource.FromJson("local.json"),
        ConfigSource.FromToml("local.toml")))
    .Merge(ConfigSource.FromJson("global.json"))
    .Build();
```

这段代码表示：先尝试从 `local.json` 读取字段，若不存在则读 `local.toml`；然后再用 `global.json` 覆盖所有结果（优先级更高）。

### 7.3 ApplySource 生成逻辑

`ApplySource` 是 Builder 的核心，由 Source Generator 为每个配置类生成。以下为生成逻辑的伪代码示例：

```csharp
private static void ApplySource(AppConfig target, ConfigNode source)
{
    if (source.Type != ConfigNode.NodeType.Object) return;

    // Registry (string)
    var node0 = source.GetField("registry");
    if (node0 != null)
        target.Registry = node0.AsString() ?? target.Registry;

    // TimeoutSeconds (int, default=30)
    var node1 = source.GetField("timeoutSeconds");
    if (node1 != null)
    {
        if (node1.Type == ConfigNode.NodeType.Scalar && int.TryParse(node1.AsString(), out var val))
            target.TimeoutSeconds = val;
        else
            target.TimeoutSeconds = 30; // 使用默认值
    }

    // Mirrors (List<string>, append)
    var node2 = source.GetField("mirrors");
    if (node2 != null && node2.Type == ConfigNode.NodeType.Array)
    {
        foreach (var item in node2.EnumerateArray())
            target.Mirrors.Add(item.AsString() ?? "");
    }

    // Database (DatabaseConfig, deep merge)
    var node3 = source.GetField("database");
    if (node3 != null)
        DatabaseConfig.Builder.ApplySource(target.Database, node3); // 递归
}
```

生成器根据属性类型、默认值、合并策略动态生成这些分支，完全类型安全且无运行时开销。

---

## 第八章：验证框架

### 8.1 内建验证特性

Sonic.Configuration 自带了一套轻量验证特性，同时也兼容 `System.ComponentModel.DataAnnotations` 中的常用特性。内建特性包括：

- **`[Required]`**：字段必须存在且值不为默认值/空。
- **`[Range(min, max)]`**：数值范围检查。
- **`[Regex(pattern)]`**：字符串正则匹配。
- **`[CollectionNotEmpty]`**：集合不能为空。
- **`[Uri]`**：字符串必须是有效的绝对 URI。
- **`[Enum]`**：枚举字段的值必须在定义范围内。

### 8.2 生成验证代码

在生成的 `Validate()` 方法中，Source Generator 会检查属性上的验证特性，并生成相应的条件语句。如果使用
`System.ComponentModel.DataAnnotations` 的标准特性，也可以利用反射，但 Sonic
推荐使用自己的内建特性来避免反射。为了保持极简，用户甚至可以不添加验证特性，此时只进行类型安全保证（转换失败会记录错误）。

### 8.3 聚合异常

`ConfigValidationException` 收集所有验证错误，提供格式化的错误消息，指出配置文件的路径、字段名和错误原因。这允许开发者一次性修正所有配置问题，而不是反复启动应用来逐个排查。

---

## 第九章：JSON Schema 生成

### 9.1 Schema 生成机制

Sonic Source Generator 在编译时会根据 `[Config]` 类的元数据生成对应的 JSON Schema 字符串，并将其作为嵌入式资源或常量包含在程序集中。

Schema 生成规则：

- 类 → `type: object`
- 属性 → 根据类型映射：
  - `string` → `{"type": "string"}`
  - `int` / `long` 等 → `{"type": "integer"}`
  - `float` / `double` → `{"type": "number"}`
  - `bool` → `{"type": "boolean"}`
  - `List<T>` → `{"type": "array", "items": {...}}`
  - `Dictionary<string, T>` → `{"type": "object", "additionalProperties": {...}}`
  - 嵌套配置类 → `{"$ref": "#/definitions/..."}`
- 属性默认值 → `"default": ...`
- `[Required]` 或不可空且无默认值的属性 → `"required": [...]`
- `[Range]` → `"minimum"`, `"maximum"`
- `[Regex]` → `"pattern"`
- `[Uri]` → `"format": "uri"`
- 敏感字段（`[Sensitive]`） → `"writeOnly": true`

### 9.2 编辑器集成

生成的 Schema 文件可以输出到编译目录或项目中的 `Schemas/` 文件夹。开发者在实际的 JSON 配置文件中添加 `$schema` 引用：

```json
{
  "$schema": "./Schemas/AppConfig.json",
  "registry": "...",
  "timeoutSeconds": 30
}
```

在 VS Code 或 Visual Studio 中，编辑器将自动提供字段提示、类型校验和错误标记，极大提升编辑体验。

### 9.3 文档与示例

此外，生成器还可以产出 Markdown 格式的配置参考文档，以及包含所有默认值的示例 JSON 文件（如 `appsettings.example.json`
），便于新人快速上手。

---

## 第十章：环境切换

Sonic.Configuration 将环境切换视为配置源组合顺序的问题，无需特殊的框架支持。

### 10.1 基于环境变量的手动切换

```csharp
var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var config = AppConfig
    .FromJson($"appsettings.{env}.json")
    .Merge(ConfigSource.FromJson("appsettings.json"))
    .Merge(ConfigSource.FromEnvironment())
    .Build();
```

### 10.2 Select 实现本地覆盖

开发环境中，经常需要一个不被 git 跟踪的 `local.json` 来覆盖任何设置：

```csharp
var config = AppConfig.FromEnvironment()
    .Merge(ConfigSource.Select(
        ConfigSource.FromJson("local.json"),
        ConfigSource.FromJson($"appsettings.{env}.json")))
    .Merge(ConfigSource.FromJson("appsettings.json"))
    .Build();
```

### 10.3 配置文件继承（通过 profile）

Sonic 支持类似 ASP.NET Core 的文件命名约定，可以直接在 `FromJson` 等方法中使用。因此不需要内置 profile 系统，交由用户自己编排。

---

## 第十一章：CLI 参数绑定

### 11.1 CliConfigProvider

CLI 源将命令行参数映射为配置键值对。默认映射规则：

- `--registry=value` 或 `--registry value` → `registry`
- `--no-offline` → `offline = false`
- `--offline` → `offline = true`
- 支持嵌套：`--database:maxPoolSize=200` 映射到 `database.maxPoolSize`

### 11.2 使用方式

```csharp
var config = AppConfig.FromCli(args)
    .Merge(ConfigSource.FromJson("appsettings.json"))
    .Build();
```

因为 CLI 通常最后 Merge，所以具有最高优先级，符合命令行覆盖文件的直觉。

### 11.3 帮助生成

Source Generator 可以根据配置类和属性上的注释或描述特性，自动生成帮助文本。用户可调用 `AppConfig.Builder.GenerateHelp()`
输出到控制台。

---

## 第十二章：依赖注入集成

### 12.1 注册配置

Sonic 提供了与 `Microsoft.Extensions.DependencyInjection` 的无缝集成：

```csharp
services.AddSonicConfig<AppConfig>(AppConfig.FromEnvironment()
    .Merge(ConfigSource.FromJson("appsettings.json"))
    .BuildWithReload());
```

该方法会注册：

- `AppConfig` 自身为 Singleton
- `IOptions<AppConfig>`、`IOptionsSnapshot<AppConfig>`、`IOptionsMonitor<AppConfig>` 的标准实现，内部代理到热加载的配置实例。

### 12.2 热加载与 IOptionsMonitor

`BuildWithReload` 返回 `IConfigWatcher<T>`，它会监控所有参与的文件源，当文件变化时自动重建配置并原子替换。因此注入
`IOptionsMonitor<T>` 的服务可以实时获取最新配置。

---

## 第十三章：热加载实现

### 13.1 IConfigWatcher 接口

```csharp
public interface IConfigWatcher<out T> : IDisposable where T : IConfigurable
{
    T Current { get; }
    event Action<T>? OnReloaded;
}
```

### 13.2 内部机制

Builder 在 `BuildWithReload` 时，会收集所有 `FileBasedConfigProvider` 的文件路径，启动 `FileSystemWatcher`
监听变化。当文件发生变更，经过防抖延迟后重新执行 `Build()`；如果新配置验证通过，则更新 `Current` 并触发 `OnReloaded`
事件；若验证失败，则保留旧配置并记录错误。

### 13.3 使用示例

```csharp
var watcher = AppConfig.FromJson("config.json").BuildWithReload();
Console.WriteLine(watcher.Current.Registry);
watcher.OnReloaded += cfg => Console.WriteLine("配置已更新！");
```

在 ASP.NET Core 中，直接将 `watcher` 传入 `AddSonicConfig`，整个应用即获得热加载能力。

---

## 第十四章：敏感数据处理

### 14.1 [Sensitive] 特性

标记 `[Sensitive]` 的属性在：

- `Clone()` 中正常复制
- `GetSchema()` 生成的 Schema 中包含 `"writeOnly": true`
- 序列化或日志时，可以通过 `ToSafeString()` 扩展方法自动替换为 `"***"`

### 14.2 安全输出

```csharp
public static class ConfigExtensions
{
    public static string ToSafeString<T>(this T config) where T : IConfigurable
    {
        // Source Generator 为每个类生成特定的脱敏打印逻辑
        return SafePrinter.Print(config); // 调用生成的方法
    }
}
```

---

## 第十五章：错误处理与诊断

### 15.1 加载错误

文件不存在或格式错误通常会使提供者的 `Load()` 抛出异常。可以使用 `Optional` 包装器将错误转换为空节点，使配置源可选：

```csharp
public static IConfigProvider Optional(this IConfigProvider provider)
    => new OptionalConfigProvider(provider);
```

这样即使某个文件缺失，也不会中断整个链。

### 15.2 类型转换与验证错误

在 `ApplySource` 中，类型转换失败不会立即抛出异常，而是记录到错误列表，最终在 `Build()` 时由 `Validate()`
返回。这样可以在一次运行中收集所有配置错误，避免“修复一个再出一个”的循环。

### 15.3 诊断工具

Sonic 提供一个 `ConfigDiagnostics` 工具，可扫描 DI 容器中所有 `IConfigurable` 服务，执行验证并返回健康报告，适合在健康检查端点使用。

---

## 第十六章：性能考量

- **零反射**：所有绑定、验证、Schema 生成代码都在编译时产生，运行时就是普通 C# 代码，无任何反射调用。
- **增量生成**：利用 `IIncrementalGenerator` 避免每次构建都重新生成全部代码，显著缩短编译时间。
- **ConfigNode 高效实现**：内部使用 `System.Text.Json` 的 `Utf8JsonReader` 或手写解析器，减少内存分配。
- **按需合并**：合并按源顺序逐个应用，不需要一次性加载所有源再做合并，内存占用可控。

---

## 第十七章：高级主题

### 17.1 自定义提供者

实现 `IConfigProvider` 接口即可创建自定义源，例如从数据库、Redis、远程 HTTP 服务加载配置。

### 17.2 配置快照与差异

利用 `IConfigurable.Clone()` 可以保存配置快照。Sonic 提供 `ConfigDiffer` 工具，比较两个配置对象生成差异报告，用于审计日志。

### 17.3 与 System.Text.Json 集成

Sonic 的配置对象可以直接序列化/反序列化，因为它们是普通 POCO 类。你可以使用 `System.Text.Json` 的 `JsonSerializer` 将
`Build()` 出的配置序列化，用于导出或调试。

---

## 第十八章：完整示例

### 18.1 定义配置

```csharp
[Config(SectionName = "app")]
public partial class AppConfig
{
    public string Urls { get; set; } = "http://localhost:5000";
    public int TimeoutSeconds { get; set; } = 30;
    public List<string> Features { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
}

[Config]
public partial class DatabaseConfig
{
    public string ConnectionString { get; set; }
    public int MaxPoolSize { get; set; } = 100;
}
```

### 18.2 程序入口

```csharp
var config = AppConfig.FromEnvironment()
    .Merge(ConfigSource.Select(
        ConfigSource.FromJson("local.json"),
        ConfigSource.FromJson("appsettings.json")))
    .Merge(ConfigSource.FromCli(args))
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSonicConfig(config);
var app = builder.Build();
app.Run();
```

### 18.3 配置文件示例

`appsettings.json`:

```json
{
  "$schema": "./Schemas/AppConfig.json",
  "urls": "http://0.0.0.0:8080",
  "timeoutSeconds": 60,
  "database": {
    "connectionString": "Server=...",
    "maxPoolSize": 200
  },
  "features": ["FeatureA", "FeatureB"]
}
```

### 18.4 热加载

```csharp
var watcher = AppConfig.FromJson("appsettings.json")
                       .Merge(ConfigSource.FromEnvironment())
                       .BuildWithReload();
services.AddSonicConfig(watcher);
// 任何注入 IOptionsMonitor<AppConfig> 的服务都能收到实时更新
```

---

## 第十九章：未来展望

- **远程配置源**：官方支持 Consul、Azure App Configuration 等提供者。
- **加密配置**：通过 `[Encrypted]` 特性自动解密。
- **AOT 编译**：确保所有生成代码与 NativeAOT 完全兼容。
- **可视化配置编辑器**：基于 Schema 自动生成 Web UI。
- **配置回滚**：与热加载结合，验证失败时自动回退到上一个有效配置。

---

## 第二十章：结语

Sonic.Configuration 是 Sonic 标准库的重要支柱，它将配置管理从手工作坊带入了工业化生产。通过一个简单的 `[Config]` 标记和
`IConfigurable` 接口，开发者从重复的样板代码中解放出来，专注于业务逻辑本身。它汲取了多语言生态的优秀范式，利用 .NET 的
Source Generator 技术实现了真正的极简与高性能的统一。

在 Sonic 的世界里，配置不再是负担，而是自然且愉悦的编程体验。