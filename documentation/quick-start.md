# 快速开始

## 环境要求

| 工具 | 版本要求 | 说明 |
|:---|:---|:---|
| .NET SDK | 9.0+ | 核心运行时 |
| IDE | Rider / VS 2022 | 推荐使用 Rider |

## 核心概念速览

Nyar 基于**对象代数**（Object Algebras）模式。以下是一个完整的端到端示例，展示"一处定义，四处生效"——同一个程序函数，传入不同的工厂实例，依次用于求值、优化、IDE 语法树构建和代码生成。

### 方言定义

```csharp
[Dialect("arith")]
public partial interface IArith
{
    [Op("lit")] Expr<int> Lit(int value);
    [Op("add")] Expr<int> Add(Expr<int> left, Expr<int> right);
    [Op("mul")] Expr<int> Mul(Expr<int> left, Expr<int> right);
}
```

### 程序函数（与表示无关）

```csharp
static E MyProgram<IArithAlg<E>>(IArithAlg<E> alg) =>
    alg.Mul(alg.Add(alg.Lit(1), alg.Lit(2)), alg.Lit(3));
```

### 求值

```csharp
var result = MyProgram(new ArithEval()); // int: 9
```

### 送入优化器

```csharp
var rootId = MyProgram(new ArithEgraphBuilder());
egraph.Saturate(rules);
var optimal = egraph.Extract(rootId, costModel);
```

### IDE 语法树

```csharp
var psiRoot = MyProgram(new ArithPsiBuilder()); // ArithNode
// 自动获得语法高亮、代码补全
```

### 代码生成

```csharp
new NativeBackend().Encode(optimal, "output.exe");
new WasmBackend().Encode(optimal, "output.wasm");
```

## 实施路线图

Nyar 的 OA 架构迁移分为三个阶段：

### 第一阶段：OA 核心落地

**目标**：建立方言 OA 接口定义规范，实现 Source Generator 自动生成工厂接口和 Builder 骨架。

**交付物**：
- `[Dialect]` / `[Op]` Source Generator
- Core 方言的完整 OA 接口和 Builder 实现
- EGraph Builder 和 PE Builder 的 OA 化

### 第二阶段：IDE 与编译器统一

**目标**：基于 OA 接口生成 PSI 节点，实现 IDE 功能。

**交付物**：
- PSI Builder 自动生成
- 语法高亮、代码补全的 OA 工厂实现
- 类型检查、引用分析的 OA 扩展

### 第三阶段：全平台后端标准化

**目标**：所有后端统一为 OA 工厂实现。

**交付物**：
- NyarVM、JVM、WASM、Native 后端统一为 `IBackend<TInput, TOutput>`
- 封闭世界优化（去虚拟化版本生成）


## 解析器快速开始 (Nyar.Intelligence)

Nyar.Intelligence 提供了一套纯语法解析基础设施。以下是使用 Green/Red 树模型构建解析器的示例：

### 1. 定义语言

```csharp
using Nyar.Intelligence.Tree;

public class MyLanguage : Language
{
    public override string Name => "MyLang";
}
```

### 2. 构建语法树

```csharp
var b = new CstBuilder();
b.StartNode(MyNodeKind.Expr);
b.AddToken(MyNodeKind.Identifier, new TextSpan(0, 5));
b.AddToken(MyNodeKind.Plus, new TextSpan(5, 1));
b.AddToken(MyNodeKind.Identifier, new TextSpan(6, 3));
b.EndNode();
GreenNode green = b.Build();
```

### 3. 创建语法树

```csharp
var source = new StringSource("var x = 42");
var tree = new SyntaxTree(source, green);
var root = tree.GetRedRoot();

foreach (var desc in root.Descendants())
{
    Console.WriteLine($"{desc.Kind} @ {desc.Span}");
}
```

### 4. 增量编辑

```csharp
var edit = new Edit(new TextSpan(4, 1), "y");
var newTree = tree.Edit(edit, incrementalParsers);
```

> 关于解析基础设施的更多细节，参见 [数据模型](./core-systems/data-model.md)、[解析器辅助](./core-systems/parsing.md)、[设计哲学](./intelligence/design-philosophy.md)。

## 下一步

1. 阅读 [项目简介](./introduction.md) 了解从 Expression Problem 到 OA 的完整叙事
2. 阅读 [对象代数](./core-systems/object-algebras.md) 理解核心模式
3. 查看 [入门指南](./getting-started.md) 开始实际编码
4. 阅读 [解析基础设施](./core-systems/data-model.md) 了解 Green/Red 树模型
5. 阅读 [分层架构](./intelligence/layer.md) 了解各层职责划分