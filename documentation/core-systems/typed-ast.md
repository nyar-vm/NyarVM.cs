# 强类型 AST：手写而非生成

Nyar.Intelligence 提供强类型 AST 的抽象基座，但所有具体类型和 Visitor 方法均由用户手写，不使用代码生成器。

---

## 基类 SyntaxNode

```csharp
public abstract class SyntaxNode {
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Offset { get; }
    public TextSpan Span => new TextSpan(Offset, Green.Width);

    protected SyntaxNode ChildNode<T>(int index) where T : SyntaxNode { /* 从树中构造 */ }
    protected SyntaxToken ChildToken(int index) { /* ... */ }

    public abstract void Accept(SyntaxVisitor visitor);
}
```

- 不包含任何代码生成逻辑。
- 用户手工派生，如：

```csharp
public sealed class BinaryExpr : ExpressionSyntax {
    public ExpressionSyntax Left => ChildNode<ExpressionSyntax>(0);
    public SyntaxToken OperatorToken => ChildToken(1);
    public ExpressionSyntax Right => ChildNode<ExpressionSyntax>(2);
    public override void Accept(SyntaxVisitor v) => v.VisitBinaryExpr(this);
}
```

---

## 手写 Visitor

```csharp
public abstract class SyntaxVisitor {
    public virtual void VisitDefault(SyntaxNode node) { }
    // 用户为每个节点类型手写虚方法
    public virtual void VisitBinaryExpr(BinaryExpr node) => VisitDefault(node);
}
```

- Nyar.Intelligence 只提供 `SyntaxVisitor` 空壳，所有分派方法全由用户手写。
- 确保完全控制，零生成污染。

---

## SyntaxRoot — 语法树根基类

```csharp
public abstract class SyntaxRoot : SyntaxNode {
    // 语言顶层结构的基类，无文件语义
}
```

用户派生如 `TypeScriptSyntaxRoot`、`JavaScriptSyntaxRoot` 等。