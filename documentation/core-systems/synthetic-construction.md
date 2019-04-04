# 程序化构造与合成

应用场景：重构、代码修复、错误恢复插入虚拟节点。

---

## 使用 CstBuilder 脱离解析环境

```csharp
var b = new CstBuilder();
b.StartNode(MyKind.BinaryExpr);
b.AddToken(MyKind.Identifier, new TextSpan(0, 5)); // 实际偏移可能为虚拟
b.AddToken(MyKind.Plus, new TextSpan(5, 1));
b.AddToken(MyKind.Identifier, new TextSpan(6, 3));
b.EndNode();
GreenNode synthetic = b.Build();
```

- 虚拟 token 可指定任意 `TextSpan`，源文本可选（若偏移超出范围，获取文本时返回空）。
- 合成的 `GreenNode` 可插入增量编辑流，替换旧子树。

---

## 强类型 AST 的工厂方法（用户手写）

用户在自定义的 `BinaryExpr` 类中添加：

```csharp
public static BinaryExpr Create(ExpressionSyntax left, SyntaxToken op, ExpressionSyntax right) {
    var b = new CstBuilder();
    b.StartNode(MyKind.BinaryExpr);
    b.AddChild(left.Green);
    b.AddChild(op.Green);
    b.AddChild(right.Green);
    b.EndNode();
    return new BinaryExpr(b.Build(), tree: null, offset: 0);
}
```

后续挂接到树时提供正确的 `SyntaxTree` 和偏移即可。