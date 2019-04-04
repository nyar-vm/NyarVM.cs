# 入门指南

## 环境搭建

| 工具 | 版本要求 | 说明 |
|:---|:---|:---|
| .NET SDK | 9.0+ | 核心运行时 |
| IDE | Rider / VS 2022 | 推荐使用 Rider |

## 构建项目

```bash
git clone https://github.com/your-org/NyarVM.cs.git
cd NyarVM.cs
dotnet restore
dotnet build
dotnet test
```

## 项目结构

```
NyarVM.cs/
├── projects/
│   ├── Nyar.Core/          # OA 基础设施：EGraph、重写引擎、成本模型
│   ├── Nyar.IR/            # 方言 OA 接口定义（由 Source Generator 生成）
│   ├── Nyar.EGraph/        # EGraph 工厂实现
│   ├── Nyar.PartialEval/   # 部分求值工厂实现
│   ├── Nyar.Optimizer/     # 优化器管线编排
│   ├── Nyar.Dialects/      # 方言库（Core/Standard/Tensor/...）
│   ├── Nyar.VM/            # NyarVM 运行时后端
│   ├── Nyar.Backends/      # 其他后端（JVM/WASM/Native）
│   └── Nyar.PSI/           # IDE 集成：PSI Builder 与分析
└── documentation/          # 文档
```

## 第一个 Nyar 程序

### 1. 定义方言

```csharp
[Dialect("arith")]
public partial interface IArith
{
    [Op("lit")] Expr<int> Lit(int value);
    [Op("add")] Expr<int> Add(Expr<int> left, Expr<int> right);
    [Op("mul")] Expr<int> Mul(Expr<int> left, Expr<int> right);
}
```

### 2. 编写程序（OA 多态函数）

```csharp
// 程序不依赖任何具体表示
static E MyProgram<IArithAlg<E>>(IArithAlg<E> alg) =>
    alg.Mul(alg.Add(alg.Lit(1), alg.Lit(2)), alg.Lit(3));
```

### 3. 求值

```csharp
var eval = new ArithEval();         // IArithAlg<int>
var result = MyProgram(eval);       // int: 9
Console.WriteLine(result);
```

### 4. 优化

```csharp
var egraphBuilder = new ArithEgraphBuilder();
var rootId = MyProgram(egraphBuilder);       // 构建 EGraph

var rules = new IRewriteRule[] { new ConstantFoldRule() };
egraphBuilder.Saturate(rules);               // 饱和优化

var costModel = new LatencyCostModel();
var optimal = egraphBuilder.Extract(rootId, costModel);
```

### 5. 生成代码

```csharp
// NyarVM 后端
var nyarBackend = new NyarVMBackend();
var nyarModule = nyarBackend.Encode(optimal);
// → .nyar 格式，可在 NyarVM 中执行

// WASM 后端
var wasmBackend = new WasmBackend("wasm32-wasi");
var wasmModule = wasmBackend.Encode(optimal);
// → .wasm 格式

// Native 后端
var nativeBackend = new NativeBackend("x86_64-pc-windows-msvc");
nativeBackend.Encode(optimal, "output.exe");
```

### 6. 同一程序，IDE 消费

```csharp
// PSI Builder（IDE 集成）
var psiBuilder = new ArithPsiBuilder();
var psiRoot = MyProgram(psiBuilder);   // ArithNode: Mul(Add(Lit(1), Lit(2)), Lit(3))

// 自动获得语法高亮、代码补全、跳转定义等 IDE 功能
```

## 定义自定义方言

```csharp
[Dialect("my-dialect")]
public partial interface IMyDialect : ICore
{
    [Op("custom")] Expr<int> CustomOp(Expr<int> input, Expr<string> config);
}

// Source Generator 自动生成 IMyDialectAlg<E> 接口
// 自动生成 MyDialectEgraphBuilder, MyDialectPEBuilder, MyDialectPsiBuilder
```

## 编写重写规则

```csharp
public class MyRules : IRewriteRule<IMyDialectAlg<EClassId>>
{
    public void Register(RewriteEngine<IMyDialectAlg<EClassId>> engine)
    {
        // CustomOp(x, "double") → Add(x, x)
        engine.Rule(alg => alg.CustomOp(alg.Any<int>(), alg.StringConst("double")))
              .Replace((x, _) => alg.Add(x, x));
    }
}
```

## Nyar.Intelligence 解析器入门

Nyar.Intelligence 提供手写解析器的基础设施。以下示例展示如何从零构建一个解析器：

### 1. 定义节点类型

```csharp
using Nyar.Intelligence.Tree;

public static class MyNodeKind
{
    public const NodeKind Module = new(1);
    public const NodeKind Function = new(2);
    public const NodeKind Statement = new(3);
    public const NodeKind Identifier = new(4);
    public const NodeKind Number = new(5);
}
```

### 2. 手写解析器

```csharp
public static GreenNode ParseModule(ref ParseContext<MyLanguage, MyContext> ctx)
{
    var b = new CstBuilder();
    b.StartNode(MyNodeKind.Module);

    while (ctx.Position < ctx.Source.Length)
    {
        ParseStatement(ref b, ref ctx);
    }

    b.EndNode();
    return b.Build();
}
```

### 3. 注册到 LanguageRegistry

```csharp
LanguageRegistry.Register("my-lang", new MyLanguage(), source =>
{
    var ctx = new ParseContext<MyLanguage, MyContext>(
        source, new MyLanguage(), new MyContext(), new DiagnosticSink());
    return ParseModule(ref ctx);
});
```

### 4. 使用

```csharp
var source = new StringSource("x + 42");
var root = LanguageRegistry.Parse("my-lang", source);

foreach (var node in root.Descendants())
{
    Console.WriteLine($"{node.Kind} @ {node.Span}");
}
```

> 更多解析基础设施的细节参见 [解析器辅助](./core-systems/parsing.md)、[增量重解析](./core-systems/incremental-reparse.md)、[语言注入](./core-systems/language-injection.md)。

## 下一步

- 阅读 [对象代数](./core-systems/object-algebras.md) 理解核心模式
- 阅读 [元编译器](./core-systems/meta-compiler.md) 了解编译管线
- 阅读 [PSI 集成](./core-systems/psi-integration.md) 了解 IDE 统一方案
- 阅读 [API 参考](./api-reference.md) 查看完整 API
- 阅读 [解析基础设施](./core-systems/data-model.md) 了解 Green/Red 树模型
- 阅读 [分层架构](./intelligence/layer.md) 了解 Nyar 体系各层职责