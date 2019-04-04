# 增量重解析

当源文本发生编辑时，Nyar.Intelligence 通过增量重解析机制避免全文件重新解析，未受影响的子树完全重用。

---

## 重解析流程

1. 计算编辑影响的最小 `GreenNode`（最深层节点，其 `Width` 覆盖编辑范围）。
2. 从该节点对应的旧源文本与新源文本，调用用户为 `NodeKind` 注册的**重解析函数**。
3. 若成功（返回非 null），用新 `GreenNode` 替换旧子树，向上更新父节点宽度；若失败（返回 null），扩大一层节点范围，再次尝试，直到根节点。
4. 未受影响的子树完全重用（引用相同 `GreenNode`），避免重新解析。

---

## 用户注册

```csharp
public delegate GreenNode? IncrementalParser(ISource source, TextSpan span, ISyntaxContext context, out bool changed);
```

- `changed` 指示输出是否与输入结构不同（若相同，可保守返回原节点以共享）。
- 重解析函数由用户手写，应设计为纯函数，依赖 `source` 和 `context`。

---

## TreeChangeEvent — 语法树变更通知

```csharp
public readonly struct TreeChangeEvent {
    public SyntaxTree OldTree { get; }
    public SyntaxTree NewTree { get; }
    public TextSpan ChangedSpan { get; }
    public IReadOnlyList<GreenNode> ReplacedNodes { get; }
}
```

- 面向订阅者（如 LSP 诊断、语义缓存层）精确告知受影响范围。
- 非强制：可由 `SyntaxTree` 的实现选择是否触发。