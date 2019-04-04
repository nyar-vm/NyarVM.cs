# 可选工具层

以下工具不属于 Nyar.Intelligence 核心，随 Nyar.Intelligence 分发，用户可按需使用。

---

## LineIndex

从 `ISource` 构建行起始偏移数组，提供偏移与行/列之间的双向转换。

```csharp
public sealed class LineIndex {
    public LineIndex(ISource source);
    public int LineCount { get; }
    public (int Line, int Column) GetLineColumn(int offset);
    public int GetOffset(int line, int column);
    public int GetLineStart(int line);
    public int GetLineEnd(int line);
}
```

- 行号和列号均从 1 开始。
- 内部使用二分查找，效率 O(log n)。

---

## DiagnosticCollector

简单错误收集器，收集语法错误。

---

## IndentSensitiveLexer 辅助

提供缩进栈维护和 `INDENT`/`DEDENT` 产生逻辑，便于解析缩进语言（如 Python）。

---

## TextWriter 缩进帮助

管理当前缩进深度，辅助格式化输出。

---

## SourceMapBuilder

构建源映射（`original span` -> `generated span`），用于编译器输出调试信息。

---

## 错误恢复组合器

`SkipTo`、`Expect`、`Recover` 等，为不自行编写恢复逻辑的用户提供便利。这些方法是纯语法糖，基于公开 API 实现，不访问内部状态。