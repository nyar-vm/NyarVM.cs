# Sonic.Data.SourceGenerator

Sonic.Data 的 Roslyn 源代码生成器，为标记了 `[GenerateSerialize]` 和 `[GenerateDeserialize]` 的类型自动生成序列化/反序列化代码。

---

## 定位

`Sonic.Data.SourceGenerator` 包含两个 `IIncrementalGenerator` 实现，在编译时分别生成序列化和反序列化方法。生成的代码直接调用
`IDataWriter`/`IDataReader` 的原语方法，消除运行时的反射遍历开销。

---

## 核心实现

### `SerializeGenerator` — 序列化代码生成器

实现 `IIncrementalGenerator` 接口，扫描 `[GenerateSerialize]` 标记的类型：

1. 收集所有公共可读属性及其类型信息
2. 按属性名称生成写入调用序列
3. 生成 `Serialize(IDataWriter writer)` 实例方法

### `DeserializeGenerator` — 反序列化代码生成器

实现 `IIncrementalGenerator` 接口，扫描 `[GenerateDeserialize]` 标记的类型：

1. 收集所有公共可写属性及其类型信息
2. 生成对象读取循环：进入对象 → 逐属性名匹配 → 按类型读取 → 构造对象
3. 生成 `Deserialize(IDataReader reader)` 静态工厂方法

### 生成的代码能力

- **基础类型支持** — `string`、`int`、`long`、`bool`、`double`，按属性类型自动匹配对应的读写方法
- **复杂类型支持** — `Option<T>`、`List<T>`、`Dictionary<TKey, TValue>` 等集合类型，生成对应的嵌套读写逻辑
- **对象嵌套** — 嵌套的自定义类型自动递归调用其自身的序列化/反序列化方法

---

## 使用方式

安装 `Sonic.Data` NuGet 包后自动启用，无需额外配置。Source Generator 将作为分析器（analyzer）随包分发。

---

## 依赖

- `Microsoft.CodeAnalysis.CSharp` 4.11.0 — Roslyn C# 分析 API
- `Microsoft.CodeAnalysis.Analyzers` 3.3.4 — Roslyn 分析器约定
- 目标框架：`netstandard2.0`
- 无其他运行时依赖