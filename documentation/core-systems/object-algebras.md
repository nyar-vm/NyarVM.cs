# 对象代数

## 为什么需要对象代数

### Expression Problem：编译器构造的根本困境

编译器设计始终面临一个结构性问题：**如何同时容易地添加新语言构造（数据类型）和新分析/优化 pass（操作），且不修改已有代码？**

这就是 Expression Problem（EP）。传统方案两难全：

| 范式 | 添加新语言构造 | 添加新分析 Pass | 代价 |
|:---|:---|:---|:---|
| 面向对象（类层次） | ✅ 容易：新增子类 | ❌ 困难：侵入所有已有类 | Visitor 模式缓解但不根治 |
| 函数式（模式匹配） | ❌ 困难：修改所有函数 | ✅ 容易：新增函数 | 泛型/类型类缓解但不根治 |
| OpCode 枚举 + Visitor | ❌ 困难：修改枚举 + 所有 Visitor | ✅ 容易：新增 Visitor | 违反开闭原则，枚举臃肿 |

Nyar 旧架构采用 OpCode 枚举 + Visitor 模式，导致：
- 添加一个语言构造需要修改 `OpCode` 枚举、所有 `Visitor` 基类、所有具体 Visitor 实现
- 枚举值随方言增长爆炸式膨胀
- 类型安全无法在编译期保证（`OpCode.Add` 可能接收任意类型的操作数）

### 对象代数的解答

**对象代数**（Object Algebras）将语言构造定义为**工厂接口的方法**，而非具体类。程序本身成为一个多态函数，接受工厂实例，调用其方法来构造表示：

```csharp
// 语言定义：一个工厂接口，每个方法对应一个语言构造
public interface IArithAlg<E>
{
    E Lit(int value);
    E Add(E left, E right);
    E Mul(E left, E right);
    E Eq(E left, E right);
    E If<T>(E cond, E then, E else);
}

// 程序：多态函数，不依赖具体表示
static E SampleProgram<IArithAlg<E>>(IArithAlg<E> alg) =>
    alg.Add(alg.Lit(1), alg.Mul(alg.Lit(2), alg.Lit(3)));
```

同一段程序，传入不同的工厂实现，即可获得完全不同的行为。

### 双向可扩展性

对象代数在 OOP 中模拟了函数式语言的数据类型多态性，从而**同时支持两个维度的独立扩展**：

**维度一：新增操作（新 Pass）**——实现新的工厂类：

```csharp
// 求值器：E = int，直接计算结果
public class ArithEval : IArithAlg<int>
{
    public int Lit(int value) => value;
    public int Add(int l, int r) => l + r;
    public int Mul(int l, int r) => l * r;
    public bool Eq(int l, int r) => l == r;
    public T If<T>(bool c, T t, T e) => c ? t : e;
}

// 美化打印：E = string
public class ArithPretty : IArithAlg<string>
{
    public string Lit(int value) => value.ToString();
    public string Add(string l, string r) => $"({l} + {r})";
    public string Mul(string l, string r) => $"({l} * {r})";
    public string Eq(string l, string r) => $"({l} == {r})";
    public string If<T>(string c, string t, string e) => $"if {c} then {t} else {e}";
}

// 类型检查：E = Type
public class ArithTypeCheck : IArithAlg<IType> { ... }

// 代码生成：E = Instruction[]
public class ArithCodeGen : IArithAlg<IEnumerable<Instruction>> { ... }
```

**维度二：新增语言构造**——定义扩展接口：

```csharp
// 扩展：增加变量绑定（无需修改 IArithAlg）
public interface IVarAlg<E> : IArithAlg<E>
{
    E Var(string name);
    E Let(string name, E rhs, Func<E, E> body);
}
```

已有工厂（如 `ArithEval`）可以选择升级以支持新构造（通过组合或子类化），新工厂可以同时实现 `IVarAlg`。旧程序无需知晓新构造即可继续工作。

### 与 OpCode 枚举的对比

| 维度 | OpCode 枚举 + Visitor | 对象代数 |
|:---|:---|:---|
| 添加操作（新 Pass） | ✅ 新增 Visitor 实现 | ✅ 新增工厂实现 |
| 添加构造（新方言） | ❌ 修改 OpCode 枚举 + 所有 Visitor | ✅ 扩展接口 |
| 类型安全 | ❌ 运行时检查，无类型保证 | ✅ 编译期泛型约束 |
| 程序可组合性 | ❌ 构造与操作紧耦合 | ✅ 程序与表示完全解耦 |
| 方言组合 | ❌ 枚举值冲突风险 | ✅ 接口继承自然组合 |
| 作用域安全 | ❌ 需手动管理环境/De Bruijn | ✅ HOAS：宿主语言闭包 |

---

## 核心机制

### 表示类型 E：一个程序，无限表示

泛型参数 `E` 是对象代数的核心。它在不同工厂实现中被绑定为不同具体类型：

| 工厂实现 | E 绑定为 | 用途 |
|:---|:---|:---|
| `ArithEval` | `int` / `T` | 直接求值 |
| `ArithPretty` | `string` | 美化打印 |
| `ArithAst` | `AstNode` | 构造语法树（IDE PSI） |
| `ArithEgraph` | `EClassId` | 送入 EGraph 饱和优化 |
| `ArithPE` | `(bool Static, object Val, object? Residual)` | 部分求值 |
| `ArithCodeGen` | `IEnumerable<Instruction>` | 生成目标代码 |
| `ArithAnalysis` | `AnalysisResult<T>` | 静态分析、类型推断 |

关键洞察：**程序不"是"AST，程序不"是"优化图，程序是一个接受工厂的多态函数。** 具体表示由调用方选择。

### HOAS：高阶抽象语法

传统 IR 处理变量绑定时，需要 De Bruijn 索引或显式环境传递，容易出错且不直观。对象代数利用宿主语言的闭包天然处理作用域：

```csharp
// HOAS 风格：body 是 C# 的 Func，闭包自动管理作用域
public interface IVarAlg<E> : IArithAlg<E>
{
    E Var(string name);
    E Let(string name, E rhs, Func<E, E> body);
}

// 使用示例：let x = 1 + 2 in x * x
static E Program<IVarAlg<E>>(IVarAlg<E> alg) =>
    alg.Let("x", alg.Add(alg.Lit(1), alg.Lit(2)),
        x => alg.Mul(x, x));
```

`x` 作为 C# 的闭包变量，其生命周期和可见性由编译器保证。不同的工厂实现可以选择是否"打开"闭包：求值器直接调用 `body(rhsValue)`，AST 构造器调用 `body(VarNode("x"))` 生成语法节点。

### 方言组合：接口继承链

Nyar 的方言系统天然映射为对象代数接口的继承链：

```csharp
// Core 方言：最基础的操作
public interface ICoreAlg<E> : IArithAlg<E>, IControlFlowAlg<E>, IMemoryAlg<E> { }

// Standard 方言：在 Core 之上增加 I/O 和字符串
public interface IStandardAlg<E> : ICoreAlg<E>, IIOAlg<E>, IStringAlg<E> { }

// Game 方言：在 Standard 之上增加游戏专用构造
public interface IGameAlg<E> : IStandardAlg<E>, IPhysicsAlg<E>, IRenderAlg<E> { }
```

这种继承结构意味着：
- 任何接受 `ICoreAlg<E>` 的程序函数，也可以接受 `IStandardAlg<E>` 或 `IGameAlg<E>` 的工厂实例
- 已有的 Core 分析 Pass（如常量折叠）无需修改即可用于 Game 方言的程序
- 方言组合时不会出现 OpCode 枚举的命名冲突

### 程序即数据：工厂组合

由于程序是接受工厂的函数，我们可以在工厂层面做组合：

```csharp
// 组合工厂：求值 + 日志
public class LoggingArithEval : IArithAlg<int>
{
    private readonly ArithEval _inner = new();
    public int Lit(int value) { var r = _inner.Lit(value); Log($"Lit({value}) = {r}"); return r; }
    public int Add(int l, int r) { var result = _inner.Add(l, r); Log($"Add({l}, {r}) = {result}"); return result; }
    // ...
}
```

这种装饰器模式使得横切关注点（日志、缓存、性能计数）可以无侵入地附加到任何工厂实现。

---

## Nyar 中的应用全景

### 统一四大应用场景

对象代数使 Nyar 得以用**同一套方言接口**驱动四大应用场景：

```
                        ┌──────────────────────────┐
                        │  方言 OA 接口（唯一定义）   │
                        │  IDialectAlg<E>           │
                        └──────────┬───────────────┘
                                   │
        ┌──────────────┬───────────┼───────────┬──────────────┐
        ▼              ▼           ▼           ▼              ▼
   ┌─────────┐  ┌──────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐
   │ 编译器   │  │  IDE     │  │ 解释器   │  │ 分析器    │  │ 扫描器   │
   │         │  │          │  │          │  │           │  │          │
   │ EGraph  │  │ PSI 工厂  │  │ NyarVM   │  │ 类型推断  │  │ Pattern  │
   │ PE 工厂  │  │ 语法高亮  │  │ JVM 后端 │  │ 引用解析  │  │ Match   │
   │ 成本模型 │  │ 代码补全  │  │ WASM后端 │  │ 效应分析  │  │ 等价搜索 │
   │ 规则引擎 │  │ 重构引擎  │  │ 原生后端 │  │ 活跃分析  │  │          │
   └─────────┘  └──────────┘  └─────────┘  └──────────┘  └──────────┘
```

**关键原则**：方言 OA 接口是唯一的事实来源（Single Source of Truth）。编译器、IDE、解释器、分析器都是同一接口的不同工厂实现。添加一种新语言构造，只需扩展接口，IDE 就会自动获得补全支持，编译器自动获得优化能力。

### 从应用视角理解 OA

| 应用 | 工厂实现 | E 的类型 | 消费方 |
|:---|:---|:---|:---|
| **编译器/优化器** | `EgraphBuilder`, `PEBuilder`, `CostModelBuilder` | `EClassId`, `(bool, object, object?)`, `CostVector` | 后端代码生成器 |
| **IDE/编辑器** | `PsiBuilder`, `SyntaxHighlightBuilder` | `PsiElement`, `ClassifiedSpan[]` | IDE 插件 |
| **解释器/VM** | `NyarVMInterp`, `JitCompiler` | `Value`, `CompiledFunction` | 运行时 |
| **静态分析** | `TypeInferBuilder`, `EffectAnalyzer` | `IType`, `EffectSet` | Linter、验证器 |
| **扫描/搜索** | `PatternMatchBuilder`, `EquivSearchBuilder` | `Pattern`, `MatchResult` | 重构工具 |

---

## Source Generator 落地

### 为何需要 Source Generator

对象代数在理念上优雅，但手写所有工厂实现极其繁琐。C# 的 Source Generator 可以在编译时分析 `[Dialect]` 标记的接口，自动生成：

1. **泛型工厂接口**：`IArithAlg<E>` 等
2. **AST 节点类 + PSI Builder**：`ArithNode` 系列 + `ArithPsiBuilder`
3. **EGraph Builder**：`ArithEgraphBuilder : IArithAlg<EClassId>`
4. **PE Builder**：`ArithPEBuilder : IArithAlg<(bool, object, object?)>`
5. **分析 Builder 骨架**：类型推断、效应分析的抽象基类
6. **方言组合器**：自动合并多个方言接口
7. **封闭世界优化版**：分析所有已知工厂，生成去虚拟化的 `switch` 版本

### 用户体验

开发者只需声明方言接口：

```csharp
[Dialect("arith")]
public partial interface IArith
{
    [Op("lit")] Expr<int> Lit(int value);
    [Op("add")] Expr<int> Add(Expr<int> left, Expr<int> right);
}
```

Source Generator 自动生成：
- `IArithAlg<E>` 泛型工厂接口
- `ArithAstBuilder`（PSI 构造器）
- `ArithEgraphBuilder`（优化器前端）
- `ArithPEBuilder`（部分求值器前端）
- `ArithInterpreter`（解释器骨架）
- IDE 分析器插件模板
- 单元测试骨架

开发者只需实现**操作语义**（如求值器的具体计算逻辑）和**优化规则**（声明式重写），其余全部自动生成。
