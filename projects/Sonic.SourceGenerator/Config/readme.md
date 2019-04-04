# Sonic.Config.SourceGenerator

Sonic.Config 的 Roslyn 源代码生成器，为标记了 `[ConfigSection]` 的类型自动生成编译期配置绑定代码。

---

## 定位

`Sonic.Config.SourceGenerator` 是一个 `IIncrementalGenerator` 实现，在编译时扫描所有 `[ConfigSection]`
标记的类型，并为其生成基于环境变量的强类型配置加载代码。敏感字段通过 `[Secret]` 特性在 `ToSafeString()` 方法中自动遮蔽。

---

## 核心实现

### `ConfigGenerator` — 配置代码生成器

实现 `IIncrementalGenerator` 接口，在编译管线的增量阶段执行：

1. 扫描所有带有 `[ConfigSection]` 特性的类型
2. 收集每个配置类型的属性和字段及其类型信息
3. 检测 `[Secret]` 标记的敏感字段
4. 生成特化加载代码

### 生成内容

- **`Load()` 静态方法** — 从环境变量读取配置值。环境变量键名格式为 `{SECTION_NAME}_{FIELD_NAME}`
  （全大写）。读取逻辑经编译期特化，避免运行时的反射开销
- **`ToSafeString()` 实例方法** — 返回配置的安全字符串表示，标记了 `[Secret]` 的字段值被替换为 `"***"`，适合日志输出
- **`appsettings.json` 默认值嵌入** — 支持编译期读取 `appsettings.json`，将其中配置值嵌入为默认值

---

## 使用方式

安装 `Sonic.Config` NuGet 包后自动启用，无需额外配置。Source Generator 将作为分析器（analyzer）随包分发。

---

## 依赖

- `Microsoft.CodeAnalysis.CSharp` 4.11.0 — Roslyn C# 分析 API
- `Microsoft.CodeAnalysis.Analyzers` 3.3.4 — Roslyn 分析器约定
- 目标框架：`netstandard2.0`
- 无其他运行时依赖