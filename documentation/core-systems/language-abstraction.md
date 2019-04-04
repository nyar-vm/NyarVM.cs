# Language 抽象 — 语法特性开关与方言控制

`Language` 是用户定义的类，代表一种具体的语言方言（如 `TypeScriptLanguage`、`JavaScriptLanguage`、`JSXLanguage`），负责声明该方言下**哪些语法特性开启**、**词法行为差异**（如是否支持装饰器、管道符）、**解析时可用的选项**。它是解析器的"配置对象"，通过 `ParseContext` 注入，让手写解析器在不修改控制流的条件下自适应不同方言。

Nyar.Intelligence 不提供 `Language` 的默认实现，只给出一个极薄的基础类/接口，所有具体逻辑由用户手写。

---

## 设计

```csharp
// Oak 提供的抽象基类
public abstract class Language {
    public abstract string Name { get; }
    // 可选：解析器工厂，用于增量/注入
    // 实际解析器仍由用户手写，Language 只是挂载点
}
```

用户可以自由扩展：

```csharp
public class TypeScriptLanguage : Language {
    public override string Name => "TypeScript";

    // 特性开关
    public bool DecoratorsEnabled { get; init; }
    public bool ConstEnumsEnabled { get; init; }
    public bool ImportTypeSyntaxEnabled { get; init; }
    // ... 其他语法选项

    // 约定：Language 不包含解析方法，只提供选项
}
```

在 `ParseContext` 中使用时，通过泛型参数传入：

```csharp
public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext { … }
```

然后手写解析器里可以直接查询开关：

```csharp
if (ctx.Language is TypeScriptLanguage ts && ts.DecoratorsEnabled) {
    TryParseDecorator(ref b, ref ctx);
}
```

Nyar.Intelligence 只在 `ParseContext` 中搬运 `Language`，不负责解析特性开关如何影响解析，那是你手写的范畴。

---

## 与 ISyntaxContext 的关系

`Language` 解决的是**静态配置**（同一语言的不同版本/方言），它不变化或仅在文件加载时确定。而 `ISyntaxContext` 更偏向**动态语义信息**（如作用域内类型名、运算符表），可能在解析同一文件时改变。两者分层清晰：

| 概念 | 性质 | 示例 |
|:---|:---|:---|
| `Language` | 静态配置 | 装饰器开关、严格模式、版本特性 |
| `ISyntaxContext` | 动态语义 | 作用域内类型名、运算符优先级表 |

用户可根据需要组合，`ParseContext<TLanguage, TContext>` 的 `TContext` 可同时包含 `Language` 引用和语义上下文。

---

## Nyar.Intelligence 不做什么

- 不定义 `Language` 的具体子类，不提供 `JavaLanguage`、`RustLanguage` 等。
- 不解析语言特性开关，不生成任何 if/else 逻辑。
- 不限制 `Language` 的属性类型（全是用户定义）。
- 不强制 `Language` 必须用于增量重解析：重解析时接收相同的 `Language` 实例即可。