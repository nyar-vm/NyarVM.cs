# PSI 集成：IDE 统一方案

## 问题：编译器与 IDE 的割裂

传统开发工具中，编译器和 IDE 是两套独立的系统：
- 编译器有自己的 IR、AST、分析逻辑
- IDE 有自己的 PSI（Program Structure Interface）、语法高亮、代码补全

这导致：添加一种语言特性时，编译器和 IDE 必须**各自**更新代码，容易遗漏和不一致。

## 答案：从方言 OA 自动生成 PSI

Nyar 的 OA 架构使 IDE 集成变得自然：方言接口是唯一的事实来源。PSI 树、语法高亮、代码补全、重构引擎——都是同一方言接口的不同工厂实现。

### 方言定义 → PSI 节点生成

给定方言接口，Source Generator 自动生成对应的 PSI 节点类：

```csharp
// 方言定义（开发者手写）
[Dialect("arith")]
public partial interface IArith
{
    [Op("lit")] Expr<int> Lit(int value);
    [Op("add")] Expr<int> Add(Expr<int> left, Expr<int> right);
    [Op("if")]  Expr<T> If<T>(Expr<bool> cond, Expr<T> then, Expr<T> else);
}
```

Source Generator 自动生成：

```csharp
// 自动生成：PSI 节点类
public abstract class ArithNode : PsiElement
{
    public abstract TResult Accept<TResult>(IArithVisitor<TResult> visitor);
}

public sealed class LitNode : ArithNode
{
    public int Value { get; }
    // ...
}

public sealed class AddNode : ArithNode
{
    public ArithNode Left { get; }
    public ArithNode Right { get; }
    // ...
}

// 自动生成：PSI Builder（OA 工厂：E = ArithNode）
public sealed class ArithPsiBuilder : IArithAlg<ArithNode>
{
    public ArithNode Lit(int value) => new LitNode(value);
    public ArithNode Add(ArithNode left, ArithNode right) => new AddNode(left, right);
    // ...
}
```

### 分析操作也是 OA 扩展

所有 IDE 分析操作都是 `IPsiAlg` 接口的方法：

```csharp
// 分析操作：扩展方言接口
public interface IArithAnalysis : IArithAlg<PsiElement>
{
    IType TypeOf(PsiElement expr);
    ControlFlowGraph BuildCFG(PsiElement body);
    IReadOnlyList<Diagnostic> Validate(PsiElement expr);
    ISymbol ResolveReference(PsiElement refExpr);
}
```

不同后端可以实现不同的分析策略（如 NyarVM 分析 vs WASM 分析）。

## 统一应用矩阵

方言 OA 接口一处定义，四处生效：

| 功能 | OA 工厂实现 | 消费方 |
|:---|:---|:---|
| **语法高亮** | `SyntaxHighlightBuilder : IArithAlg<ClassifiedSpan[]>` | IDE 编辑器 |
| **代码补全** | `CompletionBuilder : IArithAlg<CompletionItem[]>` | IDE 补全引擎 |
| **跳转定义** | `ReferenceResolver : IArithAlg<Symbol>` | IDE 导航 |
| **重构** | `RefactoringBuilder : IArithAlg<RefactoringAction[]>` | IDE 重构引擎 |
| **诊断/Error** | `DiagnosticBuilder : IArithAlg<Diagnostic[]>` | IDE 错误提示 |
| **大纲/结构** | `StructureBuilder : IArithAlg<StructureElement[]>` | IDE 大纲视图 |

## IDE 管线

```
源文件
   │
   ▼  ┌────────────────────────────┐
   │  │ Oak 解析：文本 → AST        │
   │  └────────────────────────────┘
   │
  AST (Oak 的数据结构)
   │
   ▼  ┌────────────────────────────┐
   │  │ AST → OA 程序函数（前端转换）│
   │  └────────────────────────────┘
   │
  OA 程序函数
   │
   ├──► 传入 PsiBuilder ──► PSI 树 → IDE 语法高亮、大纲
   ├──► 传入 TypeChecker ──► 类型信息 → IDE 错误提示、补全
   ├──► 传入 ReferenceResolver ──► 引用图 → IDE 跳转定义
   └──► 传入 EGraphBuilder ──► 优化后的表示 → 编译器后端
```

关键点：**IDE 和编译器共享同一 OA 程序函数**。IDE 的分析结果可以被编译器复用（例如类型信息指导优化），编译器的优化结果也可以反哺 IDE（例如常量折叠结果用于死代码灰显）。

## PSI 的可恢复性

借鉴 JetBrains PSI 的设计，Nyar 生成的 PSI 树支持**错误恢复**：即使源代码包含语法错误，PSI 树仍然完整（错误节点被标记为 `ErrorElement`），IDE 功能不会因局部错误而完全失效。

## 增量更新

OA 架构天然支持增量：程序函数不变，但可以重新传入修改部分的工厂。PSI 树基于差异更新，只重建受影响的子树。分析缓存基于内容寻址，输入未变则结果直接复用。

## 方言扩展的 IDE 影响

当开发者定义新方言时：

```csharp
[Dialect("tensor")]
public partial interface ITensor : ICore
{
    [Op("conv2d")] Expr<Tensor> Conv2D(Expr<Tensor> input, Expr<Tensor> kernel,
                                        Expr<int> stride, Expr<int> padding);
}
```

IDE 自动获得：
- `Conv2D` 的语法高亮
- `Conv2D` 的参数补全（input, kernel, stride, padding）
- 参数类型不匹配的即时错误提示
- 悬停提示显示 `Conv2D` 的签名和文档

**无需为 IDE 单独编写任何代码。**
