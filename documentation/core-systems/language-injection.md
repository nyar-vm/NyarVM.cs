# 语言注入（多语言交织）

Nyar.Intelligence 允许一个 `SyntaxTree` 包含子树，其叶子节点可能由另一个语言的解析器产生。

---

## 机制

- 主解析器在遇到可注入 token 时，调用 `InjectLanguage(languageId, sourceRange)`。
- Nyar.Intelligence 根据语言 ID 获取注册的解析器，为该范围创建子 `ISource`，调用子解析器产出子 `GreenNode`。
- 将子 `GreenNode` 作为当前节点的子节点挂入 CST。子树的全局偏移会自动通过注入点的基偏移调整。
- 转义处理（如字符串中的 `\n`）由词法器负责，子解析器看到的已是处理后的 token 流，偏移映射保持一致。

---

## 使用示例

用户编写（非框架代码）：

```csharp
// 在解析器中，当遇到模板字符串 ${...} 时
ctx.Advance(); // 跳过 ${
var childGreen = Oak.Inject(ctx, "javascript-expression", innerRange);
b.AddChild(childGreen);
```

Nyar.Intelligence 提供 `Inject` 辅助方法（调用已注册的语言解析器），不干预具体语言解析逻辑。