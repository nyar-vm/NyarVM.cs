# LanguageRegistry — 语言 ID → 解析器

全局注册机制，将语言标识符映射到解析能力。

---

## 设计

```csharp
public static class LanguageRegistry {
    public static void Register(string languageId, Func<ISource, SyntaxRoot> parser);
    public static SyntaxRoot Parse(string languageId, ISource source);
}
```

- 用户自行关联 `languageId` 与具体 `Language` 实例（通常在解析器工厂内）。
- `LanguageRegistry.Parse()` 本身就是一个工厂入口。

---

## Nyar.Intelligence 不做什么

- 不管文件扩展名映射（`.ts` → `"typescript"` 由上层定义）。
- 不实现任何具体语言的解析逻辑。