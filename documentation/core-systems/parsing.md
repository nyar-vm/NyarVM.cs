# 解析器辅助：CstBuilder 与 ParseContext

Nyar.Intelligence 不提供解析器实现，但为手写解析器提供两个核心辅助工具。

---

## CstBuilder — 构建 Green 树

```csharp
public ref struct CstBuilder {
    public void StartNode(NodeKind kind);
    public void EndNode();
    public void AddToken(TokenKind kind, TextSpan sourceRange);  // 叶子 token
    public void AddChild(GreenNode node);                        // 子树
    public GreenNode Build();                                    // 出栈根节点
}
```

- `ref struct` 保证只存在于栈上，零堆分配。
- 用法示例（由用户解析器调用，不写在框架内）：

```csharp
GreenNode ParseExpr(ref CstBuilder b, ref ParseContext ctx) {
    b.StartNode(MyKind.Expr);
    ParseTerm(ref b, ref ctx);
    while (ctx.Current.Kind == TokenKind.Plus) {
        ctx.Advance();
        ParseTerm(ref b, ref ctx);
    }
    b.EndNode();
    return b.Build();
}
```

- `CstBuilder` 不绑定任何 `Language`，保持无状态构建器角色。

---

## ParseContext — 携带状态与穿透信息

```csharp
public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext {
    public ISource Source { get; }
    public int Position { get; set; }
    public Token Current { get; }
    public TLanguage Language { get; }
    public TContext Context { get; }          // 用户定义的上下文（如符号表接口）
    public DiagnosticCollector Diagnostics { get; } // 错误收集

    public void Advance();
    public void SkipTo(TokenKind kind);
    // 可选错误恢复基元（基于公开API实现，可替换）
    public bool Recover(Action<ParseContext<TLanguage, TContext>> parse, string message);
}
```

- 词法器由用户完全手写，只需提供 `Current` 和 `Advance`。
- `TContext` 实现 `ISyntaxContext`（空接口，由用户定义），可传递解析时需要的外部信息（类型名列表、运算符优先级表等）。
- Nyar.Intelligence **不实现** `ISyntaxContext`，只负责传递。
- `TLanguage` 携带方言/版本配置，Nyar.Intelligence 只搬运，不解析开关含义。

### 泛型参数说明

推荐模式为 `ParseContext<TLanguage, TContext>`，同时携带 `Language` 和语义上下文。实际 API 留待实现时选定一种重载策略。用户也可将 `Language` 作为 `TContext` 的一部分，Nyar.Intelligence 不做强制。