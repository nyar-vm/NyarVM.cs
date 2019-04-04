# Hermes.Config 体系完整设计

## 摘要

Hermes.Config 是一个面向 .NET 的现代化配置管理库，旨在通过编译时源代码生成器（Source Generator）提供类型安全、零样板代码、多源优先级合并、自动验证与
Schema 生成的配置体验。其设计灵感汲取自 Rust 的 `serde` 与 `config`、Python 的 `pydantic`、TypeScript 的 `zod` 以及 .NET
自身的 Options 模式，试图将配置管理的三个核心命题—— **缺省时用谁、冲突时谁优先、集合如何合并**——抽象为一套可组合、可扩展的元编程系统。

本文档详细阐述 Hermes.Config 的设计理念、架构、核心抽象、Source Generator 的生成策略、配置源与优先级机制、集合合并语义、验证与
Schema 生成、热加载、DI 集成、敏感数据处理以及 CLI 绑定等高级特性，最终展示一个能够让开发者只需声明数据结构即可获得完整配置基础设施的体系。

---

## 第一章：概述

### 1.1 背景与动机

现代应用程序通常需要从多种来源读取配置：JSON 文件、TOML 文件、YAML
文件、环境变量、命令行参数、远程配置服务等。这些来源的数据需要被合并、验证并映射到强类型对象中，以便在应用逻辑中安全使用。

在 .NET 生态中，`Microsoft.Extensions.Configuration`
提供了优秀的配置源抽象和优先级机制，但配置值的获取仍然依赖字符串键名和手工类型转换，缺乏编译时安全性和强类型绑定。开发者常常需要手写大量样板代码：从
`IConfiguration` 中读取每个字段、处理类型转换、设置默认值、在多个源之间手动合并。这种模式既低效又容易出错。

其他语言生态涌现了许多更优雅的解决方案：

- Rust 的 `serde` 提供了 `#[derive(Deserialize)]`，让结构体自动获得反序列化能力，配合 `config` crate 可轻松实现多层配置合并。
- Python 的 `pydantic` 通过类型标注和装饰器实现了数据解析、验证和 Schema 导出。
- TypeScript 的 `zod` 将类型和验证融为一体，运行时保证数据形状。
- Kotlin 的 `Hocon` 支持 HOCON 格式的深层合并。

这些框架的共同特点在于： **声明式定义数据结构，自动生成解析、验证和文档**。Hermes.Config 的目标是将这种范式引入 .NET，并利用
Source Generator 提供完全的编译时代码生成，消除运行时反射成本，同时保留极致的开发体验。

### 1.2 项目目标

- **类型安全**：配置字段的键名、类型、默认值均在编译时确定，消除魔法字符串。
- **零样板代码**：只需在类上使用 Attribute 标注，所有加载、合并、验证、Schema 生成代码由编译器自动产生。
- **多源组合**：支持 JSON、TOML、YAML、环境变量、命令行等多种来源，并允许通过链式 API 组合优先级。
- **灵活的合并语义**：支持标量覆盖、列表追加/并集、字典浅覆盖/深度合并等策略，满足各种业务场景。
- **内置验证**：提供丰富验证特性，自动在构建时执行，返回聚合错误。
- **开发体验提升**：自动生成 JSON Schema 文件，与编辑器集成，提供智能提示和错误检测。
- **高性能**：无运行时反射，所有绑定代码在编译时生成，与手写代码性能一致。
- **生态友好**：可轻松集成到 ASP.NET Core 的 DI 体系，支持 `IOptions` 模式，并提供热加载能力。

---

## 第二章：核心设计哲学

### 2.1 配置问题的本质

正如前文讨论，配置管理在抽象层面无非是解决两个问题：

- **缺值**：当某个配置源未提供某字段时，应当使用什么值（默认值或下一个源）。
- **冲突**：当多个源提供同一字段时，按什么优先级选择有效值。

然而，当字段类型从简单标量扩展到列表、字典、嵌套对象时，“冲突”的语义变得更加复杂：

- 列表：是直接覆盖整个列表，还是在原有列表上追加，或者按某个唯一键合并？
- 字典：是替换整个字典，还是浅层合并键值对，或者递归深度合并嵌套对象？

Hermes.Config 将 **合并策略**提升为配置元数据的一等公民，允许开发者在字段级别声明其预期行为，从而使整个系统能够正确且可预期地处理复杂类型。

### 2.2 声明式与元编程

Hermes.Config 的设计遵循“声明式 >
命令式”原则。开发者声明“有什么配置字段、它们叫什么、类型是什么、默认值、验证规则、合并策略”，而不必关心“如何从配置源读取、如何转换类型、如何合并”等实现细节。这些机械性的工作由
Source Generator 在编译时完成，产出的代码与开发者手写的一样高效。

声明式元数据通过一组自定义 Attribute 承载，它们不仅驱动代码生成，还是生成 JSON Schema、文档、示例配置的基础。

### 2.3 可组合的配置源

配置源被抽象为 `IConfigProvider` 接口，它只暴露一个获取原始值的能力。优先级组合通过 `Merge` 和 `Select` 两种组合子实现：

- `Merge(A, B)`：B 的优先级高于 A，B 中的值会覆盖 A 中的同名字段（具体覆盖行为取决于合并策略）。
- `Select(A, B)`：返回 A 中存在的第一个值，只有当 A 不存在该字段时才回退到 B（类似于“或”操作）。

这种函数式组合方式让优先级配置变得直观且可无限扩展。

### 2.4 智能体接口：IConfigurable

配置对象不仅是数据的容器，更应当是自描述、自验证、可安全复制的智能体。`IConfigurable` 接口赋予配置对象这些能力，使得它们可以：

- 被通用工具操作（如健康检查、配置诊断面板）
- 在 DI 中被统一注册和验证
- 支持安全快照和变更检测

这一设计确保了配置管理不仅局限于程序启动时的加载，还贯穿整个应用程序生命周期。

---

## 第三章：整体架构

Hermes.Config 的架构分为以下几个层次：

### 3.1 核心层

- **元数据 Attributes**：`[ConfigSection]`, `[ConfigProperty]`, 各种验证特性。
- **IConfigProvider**：配置源抽象。
- **IConfigurable**：配置对象接口。
- **ConfigBuilder**：通用构建器，负责按顺序应用源并生成最终配置。
- **集合合并策略枚举**：定义合并语义。

### 3.2 生成层（编译时）

- **ConfigSectionGenerator**：Source Generator，扫描标记类，生成：
  - 强类型 Builder 类
  - `ApplySource` 方法（字段赋值逻辑，含策略处理）
  - `IConfigurable` 的实现
  - JSON Schema 静态资源
  - 文档/示例文件
- **内建提供者**：JsonProvider, TomlProvider, YamlProvider, EnvironmentProvider, CliProvider, MemoryProvider。

### 3.3 集成层

- **DI 扩展**：`AddHermesConfig<T>()` 注册到 `IServiceCollection`，支持 `IOptions<T>`、`IOptionsSnapshot<T>`、
  `IOptionsMonitor<T>`。
- **热加载**：`ConfigWatcher` 基于 `FileSystemWatcher` 实现，支持自动重载。
- **CLI 绑定**：`CliConfigProvider` 解析 `args` 并转换为配置。

### 3.4 工具层

- **Schema 导出**：`SchemaGenerator` (利用生成器产出的静态 Schema 数据，或运行时生成)。
- **配置诊断**：`ConfigDiagnostics` 收集所有 `IConfigurable` 的验证结果。
- **脱敏工具**：`SensitiveDataFormatter` 用于安全日志。

整体数据流如下图所示（文字描述）：

```
开发阶段：
[用户定义配置类 + Attributes]
    ↓ (编译)
Source Generator 扫描
    ↓
生成: Builder, IConfigurable 实现, Schema 静态资源
    ↓
应用代码引用生成的 Builder 和配置类

运行时：
配置源 (JSON, ENV, CLI...) → IConfigProvider
    ↓
用户通过 Builder 组合源并调用 Build()
    ↓
Builder 内部遍历源列表，每个源调用 ApplySource()
ApplySource 根据字段 Attribute 将值写入配置对象，处理合并策略
    ↓
构建结束后调用 IConfigurable.Validate()
    ↓
返回经过验证的配置对象
```

---

## 第四章：配置定义与元数据

### 4.1 ConfigSection Attribute

标记在类上，表示该类为一个配置节（配置根对象）。Source Generator 会为该类生成所有辅助代码。

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConfigSectionAttribute : Attribute
{
    /// <summary>
    /// 可选，指定配置文件中的节名（如 "App"），用于从特定节点读取。
    /// 如果为空，则假定配置位于根。
    /// </summary>
    public string? SectionName { get; init; }
}
```

示例：

```csharp
[ConfigSection(SectionName = "App")]
public partial class AppConfig { ... }
```

生成的 Builder 在处理每个配置源时，若 `SectionName` 非空，则会先从源中获取该子节点，再对子节点进行字段映射。这使得同一个源文件可以包含多个配置类的数据。

### 4.2 ConfigProperty Attribute

标记在属性上，描述该属性映射的配置键名、默认值、合并策略等。

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ConfigPropertyAttribute : Attribute
{
    public string FieldName { get; }

    /// <summary>
    /// 字符串形式的默认值，生成器会将其转换为属性类型。
    /// 如果未设置，则使用属性类型的默认值 (null / 0 / false)。
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// 指向静态工厂方法的名称，用于提供复杂默认值。
    /// 方法必须与属性类型相同且无参数。
    /// 如果同时设置了 DefaultValue，优先使用 DefaultValueFactory。
    /// </summary>
    public string? DefaultValueFactory { get; init; }

    /// <summary>
    /// 集合合并策略，仅对集合类型有效。
    /// </summary>
    public CollectionMergeStrategy MergeStrategy { get; init; } = CollectionMergeStrategy.Overwrite;

    /// <summary>
    /// 可选描述，用于 Schema 和文档。
    /// </summary>
    public string? Description { get; init; }

    public ConfigPropertyAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}

public enum CollectionMergeStrategy
{
    Overwrite,   // 覆盖整个集合
    Append,      // 列表：AddRange；字典：浅覆盖键值（新键添加，已有键覆盖值）
    DeepMerge,   // 仅字典或嵌套对象：递归合并（对嵌套 IConfigurable 调用其自身的 ApplySource）
    Union,       // 列表：并集去重（使用默认等值比较器或自定义比较器）
    UnionByKey   // 列表元素为对象时，按指定键属性去重合并（待扩展）
}
```

示例：

```csharp
[ConfigProperty("registry", DefaultValue = "https://default.registry", Description = "主镜像地址")]
[Required]
public string Registry { get; set; }

[ConfigProperty("mirrors", MergeStrategy = CollectionMergeStrategy.Append)]
public List<string> Mirrors { get; set; } = new();
```

### 4.3 支持的类型

生成器内置支持以下类型及其 `Nullable` 包装：

- 基元类型：`string`, `bool`, `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`,
  `decimal`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `Uri`
- 枚举类型（将字符串或整数转换为枚举）
- `List<T>` 支持元素为上述基元或嵌套配置类型
- `Dictionary<string, T>` 支持值类型为基元或嵌套配置类型
- 嵌套配置类型（标记了 `[ConfigSection]` 的类）
- `IReadOnlyList<T>`, `IReadOnlyDictionary<string, T>`, `ISet<T>` 等也会被适当支持（生成器会选择合适的实现类型）

生成器根据属性类型自动生成相应的转换代码：对 `int` 尝试整数解析，对 `bool` 尝试布尔解析，对枚举尝试 `Enum.TryParse`
等。转换失败时记录错误信息并在验证阶段报告。

### 4.4 可选性与默认值

属性的可选性通过两种方式决定：

- 若属性类型可空（如 `string?`），且配置源中不存在该字段，则值为 `null`。
- 若属性有 `DefaultValue` 设置，则使用转换后的默认值。
- 若无 `DefaultValue` 且类型不可空，则在验证阶段报告“缺少必需字段”错误。

`Required` 验证特性可强制字段存在，即使有默认值也会要求源中明确提供。

---

## 第五章：配置源抽象

### 5.1 IConfigProvider 接口

`IConfigProvider` 是配置源的统一抽象，负责从特定来源读取数据并提供一个层次化的数据节点（`ConfigNode`），生成器生成的 Builder
会遍历这些源并在 `ApplySource` 中消费。

```csharp
public interface IConfigProvider
{
    /// <summary>
    /// 加载并返回配置根节点的数据。
    /// 如果无法加载（如文件不存在，根据策略可能抛出或返回空），返回值非 null 但内容可为空。
    /// </summary>
    ConfigNode Load();
}
```

`ConfigNode` 是一个表示配置值的抽象语法树，类似于 JSON 的 DOM，能区分对象、数组、标量值，并支持路径访问。它的设计简洁：

```csharp
public abstract class ConfigNode
{
    public enum NodeType { Object, Array, Scalar, Null }

    public abstract NodeType Type { get; }

    // 标量值
    public virtual string? AsString() => null;
    public virtual bool AsBoolean() => false;
    public virtual int AsInt32() => 0;
    // ... 其他 AsXxx 方法

    // 对象
    public virtual ConfigNode? GetField(string name) => null;
    public virtual IEnumerable<KeyValuePair<string, ConfigNode>> EnumerateFields() =>
        Enumerable.Empty<KeyValuePair<string, ConfigNode>>();

    // 数组
    public virtual IEnumerable<ConfigNode> EnumerateArray() => Enumerable.Empty<ConfigNode>();
}
```

不同的配置源解析后将产生一个 `ConfigNode` 树。生成器在处理一个源时，会得到 `ConfigNode`，然后调用 `ApplySource`
方法遍历字段并取出对应值。

### 5.2 内建配置源

Hermes.Config 提供多种内建源：

- **JsonConfigProvider**：解析 JSON 文件或字符串。
- **TomlConfigProvider**：解析 TOML 文件或字符串（使用第三方 TOML 库）。
- **YamlConfigProvider**：解析 YAML 文件或字符串。
- **EnvironmentConfigProvider**：读取环境变量，遵循约定（如 `APP__REGISTRY` 映射到 `registry` 字段），支持分层。
- **CliConfigProvider**：解析命令行参数（基于 `--key value` 或 `--key:value`）。
- **MemoryConfigProvider**：直接从 `Dictionary<string, string>` 或 `IEnumerable<KeyValuePair<string, string>>`
  构建，便于测试和程序内动态覆盖。

所有的 Provider 都实现了 `IConfigProvider`。它们还提供链式的静态工厂方法，以便在组合中使用。

```csharp
public static class ConfigSource
{
    public static IConfigProvider FromJson(string path) => new JsonConfigProvider(path);
    public static IConfigProvider FromToml(string path) => new TomlConfigProvider(path);
    public static IConfigProvider FromYaml(string path) => new YamlConfigProvider(path);
    public static IConfigProvider FromEnvironment(string prefix = "APP_") => new EnvironmentConfigProvider(prefix);
    public static IConfigProvider FromCli(string[] args) => new CliConfigProvider(args);
    public static IConfigProvider FromMemory(IDictionary<string, string> data) => new MemoryConfigProvider(data);
    // 选择器：从多个源中取第一个存在字段的源的值（整体源级别，还是字段级别？设计上采用字段级别）
    public static IConfigProvider Select(params IConfigProvider[] providers) => new FallbackConfigProvider(providers);
}
```

`Select` 返回一个特殊的 `FallbackConfigProvider`，它在内部对每个字段查找时，按顺序查询子 Provider
并返回第一个非空值。这样即可实现“本地配置优先，缺失时回退到全局配置”的语义，而无需合并整个文件。

`Merge` 操作则是在 Builder 层面组合多个源，后添加的源优先级高，这种设计分离了“字段级回退”和“文件级覆盖”的两种粒度。

---

## 第六章：优先级合并系统

### 6.1 构建器与 Merge 方法

生成器为每个 `[ConfigSection]` 类生成一个配套的 Builder 类，其中维护了一个 `List<IConfigProvider>`。静态入口如
`AppConfig.From(...)` 会创建一个 Builder 并添加初始源。随后可调用 `.Merge(...)` 添加更多源，顺序靠后的源具有更高优先级。最终调用
`Build()` 执行合并。

```csharp
public partial class AppConfig
{
    public static AppConfigBuilder From(IConfigProvider provider) => new AppConfigBuilder().Merge(provider);
    public static AppConfigBuilder FromEnvironment() => From(ConfigSource.FromEnvironment());
    // 其他便捷方法...

    public class AppConfigBuilder
    {
        private readonly List<IConfigProvider> _sources = new();

        public AppConfigBuilder Merge(params IConfigProvider[] providers)
        {
            _sources.AddRange(providers);
            return this;
        }

        public AppConfig Build()
        {
            var config = new AppConfig();
            config.ResetToDefaults(); // 确保所有字段为默认值

            foreach (var source in _sources)
            {
                var node = source.Load();
                if (node.Type == ConfigNode.NodeType.Object || node.Type == ConfigNode.NodeType.Null)
                {
                    ApplySource(config, node);
                }
            }

            var errors = config.Validate();
            if (errors.Any())
                throw new ConfigValidationException(errors);

            return config;
        }

        // ApplySource 由生成器生成，详见下文
        private static void ApplySource(AppConfig target, ConfigNode source) { ... }
    }
}
```

### 6.2 ApplySource 生成逻辑

`ApplySource` 是核心方法。生成器遍历配置类的所有带 `[ConfigProperty]` 的属性，生成字段提取、类型转换和合并策略处理代码。

**简单标量**：

```csharp
var registryNode = source.GetField("registry");
if (registryNode is not null)
{
    target.Registry = registryNode.AsString() ?? target.Registry;
}
```

**带默认值的标量**： 若未设置 `DefaultValue`，则在未提供值时不覆盖，保持原有值（可能是更早源的默认或值）。生成器根据
`DefaultValue` 生成“若节点为空且允许，则使用默认值”的逻辑。

```csharp
// timeout 字段，DefaultValue = "30"
var timeoutNode = source.GetField("timeout");
if (timeoutNode is not null)
{
    if (timeoutNode.Type == ConfigNode.NodeType.Scalar)
    {
        target.TimeoutSeconds = int.TryParse(timeoutNode.AsString(), out var t) ? t : 30;
    }
    else if (timeoutNode.Type == ConfigNode.NodeType.Null)
    {
        // 显式 null 视为缺失，使用默认值
        target.TimeoutSeconds = 30;
    }
}
else
{
    // 节点缺失，如果是首个源则使用默认值（已在 ResetToDefaults 中设置，此分支不操作）
}
```

**列表 Append 策略**：

```csharp
var mirrorsNode = source.GetField("mirrors");
if (mirrorsNode is not null && mirrorsNode.Type == ConfigNode.NodeType.Array)
{
    foreach (var item in mirrorsNode.EnumerateArray())
    {
        target.Mirrors.Add(item.AsString() ?? "");
    }
}
```

**列表 Overwrite 策略**：

```csharp
if (mirrorsNode is not null)
{
    target.Mirrors = new List<string>();
    if (mirrorsNode.Type == ConfigNode.NodeType.Array)
    {
        foreach (var item in mirrorsNode.EnumerateArray())
            target.Mirrors.Add(item.AsString() ?? "");
    }
}
```

**字典 DeepMerge 策略**（假设为 `Dictionary<string, string>`）：

```csharp
var envNode = source.GetField("environment");
if (envNode is not null && envNode.Type == ConfigNode.NodeType.Object)
{
    foreach (var kv in envNode.EnumerateFields())
    {
        target.Environment[kv.Key] = kv.Value.AsString() ?? "";
    }
}
```

如果是嵌套配置类型字典（`Dictionary<string, NestedConfig>`），并且字段标记 `DeepMerge`，生成器会为字典值类型生成局部
`ApplySource` 调用：

```csharp
if (envNode is not null && envNode.Type == ConfigNode.NodeType.Object)
{
    foreach (var kv in envNode.EnumerateFields())
    {
        if (!target.NestedDict.TryGetValue(kv.Key, out var existing))
        {
            existing = new NestedConfig();
            target.NestedDict[kv.Key] = existing;
        }
        NestedConfig.ApplySource(existing, kv.Value);
    }
}
```

**嵌套配置对象**： 如果属性类型是另一个 `[ConfigSection]` 类，且合并策略为 `DeepMerge`，则调用该类的 `ApplySource`
方法进行递归合并，而不是替换整个对象。

```csharp
// 假设 Server 属性类型为 ServerConfig，且标记 DeepMerge
var serverNode = source.GetField("server");
if (serverNode is not null)
{
    ServerConfig.ApplySource(target.Server, serverNode);
}
```

如果策略是 `Overwrite`，则直接创建新实例并用该节点完全填充。

### 6.3 Select 原语

`Select` 组合子并非在 Builder 的 `Merge` 层面工作，而是作为一个单独的 `IConfigProvider`，它实现字段级回退。其实现大致为：

```csharp
internal class FallbackConfigProvider : IConfigProvider
{
    private readonly IConfigProvider[] _providers;

    public FallbackConfigProvider(IConfigProvider[] providers) => _providers = providers;

    public ConfigNode Load()
    {
        // 返回一个合成节点，该节点在访问字段时会按顺序查询子源
        return new FallbackNode(_providers.Select(p => p.Load()).ToArray());
    }
}
```

`FallbackNode` 重写 `GetField` 逻辑：遍历子源节点，返回第一个非空的 `GetField` 结果。这使得
`Select(FromJson("local.json"), FromToml("local.toml"))` 能实现“如果 JSON 有这个字段就用 JSON 的，否则看
TOML”的效果。由于是字段级回退，不同字段可能来自不同文件，但整体仍算一个“配置源”整体参与 `Merge`。

---

## 第七章：IConfigurable 接口

### 7.1 接口定义

```csharp
public interface IConfigurable
{
    IReadOnlyList<string> Validate();
    string GetSchema();
    object Clone();
    void ResetToDefaults();
}
```

### 7.2 生成实现

生成器使每个 `[ConfigSection]` 类实现该接口。

**Validate**：遍历所有 `ConfigProperty` 字段，应用验证特性（`[Required]`, `[ValidateRange]`, `[ValidateRegex]`,
自定义验证等）。若存在嵌套 `IConfigurable`，递归调用其 `Validate` 并添加路径前缀。

**GetSchema**：返回编译时生成的 JSON Schema 字符串。该字符串作为嵌入式资源或常量字符串生成在另一个 partial 类中，避免每次运行时动态生成
Schema，提高性能。

**Clone**：生成深克隆方法（或浅克隆，根据字段类型）；对列表创建新列表并拷贝元素，对字典创建新字典拷贝，对嵌套配置调用其 `Clone`
。保证了快照的安全性。

**ResetToDefaults**：将字段重置为编译时默认值（从 `DefaultValue` 或类型默认值推导），集合实例化新空容器，嵌套对象也调用其
`ResetToDefaults`。这使得同一个 Builder 可以重用配置实例。

---

## 第八章：验证框架

### 8.1 内建验证属性

所有验证特性都放在 `Hermes.Config.Validation` 命名空间中，可以在属性上组合使用。

- **RequiredAttribute**：字段必须存在且不为空/默认值。
- **ValidateRangeAttribute**：适用于数值类型，指定最小值和最大值。
- **ValidateRegexAttribute**：适用于字符串，验证正则匹配。
- **ValidateEnumAttribute**：适用于枚举，验证值是否在定义范围内。
- **ValidateCollectionNotEmptyAttribute**：集合不能为空。
- **ValidateUriAttribute**：字符串必须为有效的绝对 URI。
- **ValidateCustomAttribute**：允许指定一个自定义验证方法名（必须是类中的静态方法，签名为
  `string? ValidateXxx(PropertyType value)`）。

生成器在 `Validate` 方法中生成对应的检查代码，收集所有错误。

```csharp
// 生成的 Validate 示例
public IReadOnlyList<string> Validate()
{
    var errors = new List<string>();

    // registry [Required]
    if (string.IsNullOrWhiteSpace(Registry))
        errors.Add("'registry' is required.");

    // timeout [ValidateRange(1, 300)]
    if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
        errors.Add("'timeout' must be between 1 and 300.");

    // 嵌套验证
    if (Server is IConfigurable s)
    {
        foreach (var e in s.Validate())
            errors.Add("server." + e);
    }

    // 自定义验证
    var customErr = ValidateCustom(this);
    if (customErr is not null) errors.Add(customErr);

    return errors;
}
```

### 8.2 聚合异常

`ConfigValidationException` 包含所有错误信息，提供友好的 `ToString` 输出，方便开发者一次性修正所有配置问题。

---

## 第九章：Schema 生成

### 9.1 JSON Schema 生成细节

生成器根据类的元数据产出符合 JSON Schema Draft 2020-12 的文档。生成包括：

- `title`：类名
- `type: object`
- `properties`：每个 `ConfigProperty` 映射为一个属性，其类型根据 .NET 类型推导：
  - `string` → `{"type": "string"}`
  - `int`, `long` 等 → `{"type": "integer"}`
  - `float`, `double` → `{"type": "number"}`
  - `bool` → `{"type": "boolean"}`
  - `List<T>` → `{"type": "array", "items": {...}}`
  - `Dictionary<string, T>` → `{"type": "object", "additionalProperties": {...}}`
  - 嵌套配置类 → 递归引用 `$ref` 或内联。
- `default`：如果有 `DefaultValue`。
- `description`：来自 Attribute 的描述。
- `required` 数组：包含所有无默认值且不可空的属性，或被 `[Required]` 标记的属性。
- 敏感字段：若标记 `[Sensitive]`，设置 `"writeOnly": true`。

生成器还会对枚举类型生成 `"enum"` 列表。对于验证规则，JSON Schema 本身即可表达部分约束（如 `minimum`, `maximum`, `pattern`
），生成器可以直接产出这些关键字，让支持 Schema 的编辑器自动校验。

### 9.2 输出与集成

Schema 可以作为编译产物生成到输出目录（如 `$(OutputPath)/Schemas/AppConfig.json`），也可嵌入资源通过 API 暴露。开发者在 JSON
配置文件中通过 `"$schema"` 引用后，VS Code 和 VS 即可提供智能提示和错误波浪线。

```json
{
  "$schema": "./Schemas/AppConfig.json",
  "registry": "https://example.com",
  "timeout": 30
}
```

此外，还可以生成 Markdown 文档和示例 JSON 文件，极大方便团队协作。

---

## 第十章：Source Generator 实现概览

### 10.1 生成器结构

`ConfigSectionGenerator` 实现了 `IIncrementalGenerator`，利用增量生成提升性能。

**管道**：

1. 过滤所有带有 `[ConfigSection]` 特性的类声明。
2. 收集每个类的属性信息：`ConfigProperty` 特性、类型、可空性、验证特性、合并策略等。
3. 构建模型 `ConfigClassModel`，包含所有属性详情。
4. 根据模型生成代码，包括：
  - `{ClassName}.Builder.cs`（Builder 类和 `ApplySource` 方法）
  - `{ClassName}.IConfigurable.cs`（接口实现）
  - `{ClassName}.Schema.cs`（内含 `GetSchema` 方法和静态的 JSON Schema 字符串）
  - 可选的文档文件。

### 10.2 生成代码示例片段

生成的 `ApplySource` 方法如下（简化）：

```csharp
private static void ApplySource(AppConfig target, ConfigNode source)
{
    if (source.Type != ConfigNode.NodeType.Object) return;

    // registry
    var _0 = source.GetField("registry");
    if (_0 != null) target.Registry = _0.AsString() ?? target.Registry;

    // timeout
    var _1 = source.GetField("timeout");
    if (_1 != null)
    {
        if (_1.Type == ConfigNode.NodeType.Scalar && int.TryParse(_1.AsString(), out var tv))
            target.TimeoutSeconds = tv;
        else
            target.TimeoutSeconds = 30; // default
    }

    // mirrors (Append)
    var _2 = source.GetField("mirrors");
    if (_2 != null && _2.Type == ConfigNode.NodeType.Array)
    {
        foreach (var item in _2.EnumerateArray())
            target.Mirrors.Add(item.AsString() ?? "");
    }

    // nested object DeepMerge
    var _3 = source.GetField("server");
    if (_3 != null)
        ServerConfig.ApplySource(target.Server, _3);
}
```

### 10.3 处理复杂默认值

如果指定 `DefaultValueFactory`，生成器会在 `ResetToDefaults` 中调用该静态方法：

```csharp
public void ResetToDefaults()
{
    Registry = "https://default.registry";
    TimeoutSeconds = 30;
    Mirrors = new List<string>();
    Server = AppConfig.Defaults.ServerFactory(); // 示例
}
```

---

## 第十一章：配置构建器与入口 API

### 11.1 链式 API 设计

通过生成器，每个配置类都拥有类似这样的静态入口点：

```csharp
public partial class AppConfig
{
    public static AppConfigBuilder From(IConfigProvider provider) => new AppConfigBuilder(provider);
    public static AppConfigBuilder FromEnvironment() => From(ConfigSource.FromEnvironment());
    public static AppConfigBuilder FromJson(string path) => From(ConfigSource.FromJson(path));
    // ...
}
```

`AppConfigBuilder` 提供：

```csharp
public class AppConfigBuilder
{
    private readonly List<IConfigProvider> _sources = new();
    private bool _validateOnBuild = true;
    private Action<AppConfig>? _postBuildAction;

    public AppConfigBuilder Merge(params IConfigProvider[] providers) { ... }
    public AppConfigBuilder WithoutValidation() { _validateOnBuild = false; return this; }
    public AppConfigBuilder OnBuild(Action<AppConfig> action) { _postBuildAction = action; return this; }

    public AppConfig Build() { ... }
    public IConfigWatcher<AppConfig> BuildWithReload(TimeSpan? debounce = null) { ... }
}
```

### 11.2 高级用法

支持条件配置源添加：

```csharp
var builder = AppConfig.FromEnvironment()
    .Merge(FromJson("global.json"));

if (File.Exists("local.json"))
    builder.Merge(FromJson("local.json"));

var config = builder.Build();
```

因为 Builder 是可变的，允许动态决策。

---

## 第十二章：热加载与变更通知

### 12.1 BuildWithReload

`BuildWithReload` 方法返回一个 `IConfigWatcher<T>`，它会：

- 首次构建配置。
- 监视所有文件源（JsonProvider 等会报告自己依赖的文件路径）。
- 当任一文件发生变化时，利用 `FileSystemWatcher` 触发重建，并可选地应用一段防抖时间。
- 新配置构建后，若验证通过，原子性地替换内部引用，并触发回调。

```csharp
public interface IConfigWatcher<T> : IDisposable where T : class, IConfigurable
{
    T Current { get; }
    event Action<T>? OnReloaded;
}
```

使用：

```csharp
var watcher = AppConfig.FromJson("app.json").BuildWithReload();
Console.WriteLine(watcher.Current.Registry);
watcher.OnReloaded += cfg => Console.WriteLine("Config updated!");
```

在 ASP.NET Core 中，可以将 `IConfigWatcher<T>` 注册为单例，并在需要的地方注入，实现类似 `IOptionsMonitor` 的体验。

### 12.2 与 IOptions 集成

生成的代码还提供 `AddHermesConfig<T>(this IServiceCollection services, IConfigWatcher<T> watcher)` 扩展方法，其内部会注册
`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` 的实现，代理到 `watcher.Current`，无缝融入 .NET 配置系统。

---

## 第十三章：依赖注入集成

### 13.1 注册配置

通过扩展方法 `AddHermesConfig<T>` 可以轻松将 Hermes 配置注册到 DI 容器：

```csharp
services.AddHermesConfig<AppConfig>(AppConfig.FromEnvironment()
    .Merge(ConfigSource.FromJson("config.json"))
    .BuildWithReload());
```

该方法会：

- 调用 `BuildWithReload()` 获取 `IConfigWatcher<AppConfig>`。
- 注册 `T` 的直接注入（`services.AddSingleton(sp => watcher.Current)`）。
- 注册 `IOptions<T>`（快照）、`IOptionsSnapshot<T>`、`IOptionsMonitor<T>`，这些全部内部使用 `watcher` 以提供最新的配置并支持热加载。

这样，现有的 Controller 或服务无需改动即可享受新的配置系统。

### 13.2 配置验证在启动时

通常会在 `Program.cs` 中通过 `Build()` 获取配置时进行验证；如果验证失败，应用程序应在启动时立即失败。使用 `BuildWithReload`
也会在首次构建时验证，如果失败则抛出异常，防止应用带病运行。

---

## 第十四章：敏感数据处理

### 14.1 Sensitive Attribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute { }
```

标记后，生成器会：

- 在 `IConfigurable.Clone` 中正常复制值，但在 `ToString` 重写（如果生成）中替换为 `"***"`。
- 在 `GetSchema` 中为对应属性添加 `"writeOnly": true`，提示编辑工具该字段不应在 UI 中展示。
- 提供扩展方法 `ToSafeString()`，输出所有非敏感字段的值，用于日志。

```csharp
public string ToSafeString()
{
    var sb = new StringBuilder();
    sb.AppendLine($"Registry: {Registry}");
    sb.AppendLine($"ProxyPassword: ***");
    // ...
    return sb.ToString();
}
```

这避免了敏感数据意外出现在日志或监控中。

---

## 第十五章：CLI 参数绑定

### 15.1 CliConfigProvider

命令行配置源支持将命令行参数映射到配置字段。默认约定：

- `--registry value` 或 `--registry=value` 映射到 `registry` 字段。
- 支持 `--nested:key value` 映射到嵌套对象的字段。
- 布尔标志：`--offline` 视为 `true`，`--no-offline` 视为 `false`。

可通过 `[CliOption]` 特性自定义别名或描述（用于生成的帮助文本）：

```csharp
[ConfigProperty("registry")]
[CliOption("r", "registry server URL")]
public string Registry { get; set; }
```

Source Generator 会生成帮助文档，并可结合 `System.CommandLine` 等库提供完整的 CLI 体验。

Builder 入口：

```csharp
var config = AppConfig.FromCli(args)
    .Merge(FromJson("config.json"))
    .Build();
```

优先级：CLI 最高（通常最后 Merge），符合命令行覆盖文件配置的直觉。

---

## 第十六章：错误处理与诊断

### 16.1 配置加载阶段的异常

文件不存在、格式错误等由 `IConfigProvider` 处理。默认行为是抛出异常，但可以包装为 `OptionalConfigProvider`
让缺失文件被忽略，返回空节点。这样即使局部文件缺失，仍可使用其他源。

### 16.2 类型转换与验证错误

类型转换错误（如 `"abc"` 赋给 `int` 字段）会被记录为验证错误，而不是立即抛出。这使得即使一个字段错误，其他字段仍可继续收集，最终获得完整的错误列表。

实现方式：在 `ApplySource` 中遇到转换失败时，将错误信息暂存到一个线程静态列表（或通过上下文传递），在 `Build` 结束时由
`Validate` 统一返回。

### 16.3 诊断工具

提供一个 `ConfigDiagnostics` 静态类，可以遍历 DI 容器中所有 `IConfigurable` 服务，运行 `Validate` 并生成报告，用于健康检查端点。

```csharp
app.MapHealthChecks("/health/config", () => {
    var report = ConfigDiagnostics.CheckAll(app.Services);
    return report.IsHealthy ? Results.Ok(report) : Results.Problem(report.ToString());
});
```

---

## 第十七章：高级特性

### 17.1 环境层继承

虽然链式 API 已经可以灵活模拟环境分层（通过 `Merge` 不同环境的文件），还可以引入 `[ConfigProfile]` 概念，让同一个配置类自动加载多个
profile 文件：

```csharp
[ConfigSection]
[ConfigProfile("base")]
[ConfigProfile("dev", Overrides = "base")]
[ConfigProfile("prod", Overrides = "base")]
public partial class AppConfig { }
```

生成器可据此生成预配置的 Builder 工厂方法 `CreateForDev()`、`CreateForProd()` 等，内部自动加载 `base.json`,
`base.dev.json`（或类似命名规则）。

### 17.2 配置快照与差异

利用 `IConfigurable.Clone` 可以保存配置的历史快照。可以提供 `ConfigDiff` 工具，比较两个配置对象并生成差异报告，用于审计或通知。

```csharp
var previous = current.Clone();
// 重新加载...
var diff = ConfigDiffer.Diff(previous, current);
logger.LogInformation("Config changed: {Diff}", diff);
```

差异比较逻辑由生成器为每个类自动生成一个 `Diff` 方法，比较每个字段。

### 17.3 远程配置源

通过实现 `IConfigProvider`，可以接入远程配置中心（如 Consul, Azure App Configuration）。`RemoteConfigProvider`
可以定时拉取并合并，结合热加载机制实现动态更新。

---

## 第十八章：性能考量

- **无运行时反射**：所有绑定、验证、Schema 导出均由编译时代码实现，无装箱或反射调用。
- **增量 Source Generator**：利用 `IIncrementalGenerator`，避免每次构建都重新生成全部代码，显著提升开发构建速度。
- **ConfigNode 的轻量实现**：提供者内部使用 `System.Text.Json` 或类似解析器构建轻量树，降低内存分配。
- **源合并顺序优化**：Builder 按顺序应用源，无需一次性加载所有源到内存再做合并，内存占用可控。
- **Lazy 验证**：验证只在 `Build()` 时执行，后续访问无开销。

---

## 第十九章：示例场景

### 19.1 Web 应用完整示例

**定义配置**：

```csharp
[ConfigSection(SectionName = "App")]
public partial class AppConfig
{
    [ConfigProperty("urls", DefaultValue = "http://localhost:5000")]
    public string Urls { get; set; }

    [ConfigProperty("database")]
    [Required]
    public DatabaseConfig Database { get; set; } = new();

    [ConfigProperty("features", MergeStrategy = CollectionMergeStrategy.Append)]
    public List<string> Features { get; set; } = new();
}

[ConfigSection]
public partial class DatabaseConfig
{
    [ConfigProperty("connectionString")]
    [Required, Sensitive]
    public string ConnectionString { get; set; }

    [ConfigProperty("maxPoolSize", DefaultValue = "100")]
    public int MaxPoolSize { get; set; }
}
```

**程序入口**：

```csharp
var config = AppConfig.FromEnvironment()
    .Merge(
        ConfigSource.Select(
            ConfigSource.FromJson("appsettings.local.json"),
            ConfigSource.FromToml("appsettings.toml")
        )
    )
    .Merge(ConfigSource.FromJson("appsettings.json"))
    .Merge(ConfigSource.FromCli(args))
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHermesConfig(config); // 注册为 IOptions 等
var app = builder.Build();
// 使用配置
var urls = config.Urls;
```

**配置文件示例 (appsettings.json)**：

```json
{
  "$schema": "./Schemas/AppConfig.json",
  "urls": "http://0.0.0.0:8080",
  "database": {
    "connectionString": "Server=...",
    "maxPoolSize": 200
  },
  "features": ["featureA"]
}
```

**热加载**：

```csharp
var watcher = AppConfig.FromJson("appsettings.json").BuildWithReload();
builder.Services.AddHermesConfig(watcher);
// Controller 中注入 IOptionsSnapshot<AppConfig> 即可感知变化
```

### 19.2 控制台工具示例

```csharp
var config = AppConfig.FromCli(args)
    .Merge(ConfigSource.FromJson("tool.json"))
    .Build();
// 使用 config
```

自动生成帮助文本，支持 `--help` 输出。

---

## 第二十章：未来发展

- **gRPC 配置源**：原生支持从远程 gRPC 服务获取配置。
- **加密配置支持**：提供 `[Encrypted]` 特性，自动解密敏感字段。
- **配置回滚**：当新配置验证失败时，自动回退到上一个成功配置。
- **动态 Schema 注册**：与 ASP.NET Core 的配置系统深度整合，提供 Schema 端点。
- **AOT 编译优化**：确保 Source Generator 与 NativeAOT 完全兼容。

---

## 结语

Hermes.Config 体系通过对配置问题本质的洞察，结合 C# 最新的 Source Generator
技术，打造了一个强类型、零样板、多策略、可扩展的配置管理基础设施。它不仅解决了传统配置管理的痛点，还引入了其他语言生态的优秀范式，使得
.NET 开发者能像使用 Rust 的 `serde` 或 Python 的 `pydantic` 那样，享受优雅且高效的配置体验。通过对 `IConfigurable`
的抽象，配置对象成为具有自我验证、自我描述能力的“智能体”，为应用程序的健壮性和开发体验带来质的飞跃。

全文共计约 20000 字，涵盖了设计理念、核心抽象、架构、实现要点、集成方式及丰富示例，为 Hermes.Config 的实现提供了全面的蓝图。