# 数据模型

Nyar.Intelligence 数据模型由五个核心类型组成，它们共同构成了不可变语法树的基础。

---

## ISource — 只读源文本

```csharp
public interface ISource {
    char this[int index] { get; }
    int Length { get; }
    string Substring(Range range);
}
```

- 不可变，不提供 `Replace`。编辑通过 `Edit` 描述对象传递。
- 索引单元：用户定义。可以是 `byte`、`char`（UTF-16 代码单元）等。所有位置信息均以此为单位。
- 实现：可以是简单 `string` 包装、rope、编辑器快照等。

### Edit — 描述编辑操作

```csharp
public readonly struct Edit {
    public TextSpan OldSpan { get; }
    public string NewText { get; }
}
```

> **修订说明**：早期设计中 `ISource` 包含 `Replace` 方法，暗示可变性。现已去除，`ISource` 变为纯只读接口。编辑表示由 `Edit` 描述结构承担，由上层传递编辑意图。理由：语法层不应参与源数据修改，保证不可变性，适配编辑器只读快照或版本化源。

---

## TextSpan — 纯偏移范围

```csharp
public readonly struct TextSpan {
    public int Start { get; }   // 字节/单元偏移
    public int Length { get; }  // 字节/单元长度
    public int End => Start + Length;
}
```

- 不包含行/列。行列转换由 `Nyar.Intelligence.Utilities.LineIndex` 等可选工具提供。

---

## GreenNode — 不可变语法节点

```csharp
public abstract class GreenNode {
    public abstract NodeKind Kind { get; }
    public abstract int Width { get; }          // 占用的源长度（字节/单元）
    public abstract GreenNode? GetChild(int index);
    public abstract int ChildCount { get; }
    // 内部实现：叶节点存储 token 文本或仅长度；内部节点存储子节点列表
}
```

- 不可变，因此可安全地在多处共享（如重复的表达式）。
- 子树重用：同一个 `GreenNode` 实例可被多棵树引用，完全无副作用。
- 构建方式：通过 `CstBuilder`。

---

## RedNode — 带绝对位置的视图

```csharp
public readonly struct RedNode {
    internal GreenNode Green { get; }
    internal SyntaxTree Tree { get; }
    internal int Start { get; }          // 绝对偏移

    public NodeKind Kind => Green.Kind;
    public TextSpan Span => new TextSpan(Start, Green.Width);
    public RedNode? Parent { get; }      // 惰性计算，从 Tree 中查找
    public RedNode GetChild(int index) { /* ... */ }
    // 便利查询：Descendants, Ancestors, FindToken 等
}
```

- `RedNode` 是轻量值类型（`readonly struct`），栈上友好。
- 每次查询生成新的 RedNode（复制成本极低），不更改内部状态。
- Parent 通过树遍历或缓存计算（若启用缓存），默认惰性向上查找。

---

## SyntaxTree — 树的根源

```csharp
public class SyntaxTree {
    public ISource Source { get; }
    public SyntaxRoot PrimaryRoot { get; }
    public IReadOnlyList<SyntaxRoot> AllRoots { get; }
    public SyntaxRoot GetRoot(string languageId);

    public RedNode GetRedRoot() => new RedNode(Root, this, 0);

    // 增量编辑入口
    public SyntaxTree Edit(Edit edit, IncrementalParserRepo parsers);
}
```

- `Edit` 返回一棵**全新的** `SyntaxTree`，旧树不变，满足不可变性。
- 内置节点缓存（可选），加速 RedNode 构造和父节点查找。

### SyntaxRoot — 语法树根基类

```csharp
public abstract class SyntaxRoot : SyntaxNode {
    // 语言顶层结构的基类，无文件语义
}
```

> **修订说明**：早期设计提及 `SourceFile` 或模糊的树根概念。现已修订为纯语法的 `SyntaxRoot`，不包含任何文件/路径/IO 信息。用户派生如 `TypeScriptSyntaxRoot`、`JavaScriptSyntaxRoot` 等。`SyntaxTree` 持有 `SyntaxRoot`，而非文件对象。

### 多 SyntaxRoot 支持

`SyntaxTree` 可暴露多个独立 `SyntaxRoot`，对应同一源的不同语言根（主要根 + 注入的子语言根）。

- 注入的子解析器产出的 `GreenNode` 子树，除了挂载到主树外，也可包装为独立 `SyntaxRoot`，通过 `AllRoots` 查询。
- 不创建文件：完全在语法树内部完成，不依赖文件抽象。