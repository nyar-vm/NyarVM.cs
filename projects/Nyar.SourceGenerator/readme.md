# Nyar Attribute + Source Generator 补充设计文档

**版本**：2.1  
**状态**：设计草案  
**摘要**：本文档是《Nyar 架构完整设计规范》的补充，聚焦于如何使用 C# 的 Attribute 和 Roslyn Source Generator
实现全编译期安全的元编译器内部组件。涵盖代数接口自动生成、重写规则的类型安全声明、指令集的穷尽发射与解码、成本模型的自动绑定、部分求值器静态性分析、后端降级的强制覆盖，以及跨模块一致性诊断等核心机制。所有设计均遵循“零字符串逻辑、完全静态验证”的原则，目标是将
Nyar 内核算法的正确性验证前置到开发阶段。

---

## 1. 引言

### 1.1 文档动机

《Nyar 架构完整设计规范》定义了以 Object Algebra 为核心的可扩展意图表示、EGraph 饱和优化、部分求值代码生成以及可选的 NyarVM
运行时。然而，在实际工程实现中，如何高效且无错地定义大量的 **重写规则**、 **成本模型**、 **指令集**、 **后端降级**
等组件，成为维护性的关键瓶颈。传统做法往往使用字符串标识规则模式、手写庞大的 switch 分发、手工注册各类回调，不仅枯燥易错，而且无法利用现代
IDE 的静态分析能力。

本补充文档提出一套 **以 Attribute 标记 + Source Generator 自动生成**的架构，将所有配置和元数据从字符串或外部 DSL
中解放出来，提升为强类型、可编译检查的 C# 代码。开发者只需用 Attribute 描述“意图”，框架在编译时自动生成具体的逻辑胶水代码，确保任何不一致或遗漏都会导致编译错误，从而将
Nyar 打造成一个“编译即正确”的元编译器框架。

### 1.2 核心原则

- **零字符串逻辑**：所有模式匹配、指令助记符等不再依赖运行时字符串解析，均由强类型语法树或枚举表示。
- **全编译期验证**：通过 Source Generator 和 Roslyn Analyzer，在编译时完成接口实现完整性、规则变量绑定、指令操作数一致性等检查，杜绝运行时意外。
- **开发者友好**：属性标记简洁直观，代码生成透明，IDE 智能提示、重构、跳转定义等工具无缝支持。
- **可扩展性保留**：Object Algebra 的双向独立扩展依然有效；任何新代数接口、新后端均可通过添加相应的 Attribute
  和生成器支持而无需修改核心框架。

### 1.3 与核心规范的关系

本补充文档不修改核心规范中定义的 IKun 意图模型（现更名为各种 `I*Algebra` 接口）、EGraph 工作流或部分求值引擎。它仅阐述如何通过编译期元编程技术，
**简化这些组件的定义和维护**。阅读前应熟悉主文档中的术语和架构。

---

## 2. Nyar Attribute 系统概述

### 2.1 设计哲学

Nyar Attribute 不是运行时注解，而是 **编译期指令**。它们被 Source Generator 消费，生成额外的 C#
源代码文件，这些文件随后参与正常的编译流程。Attribute 分为以下几类：

- **标记性 Attribute**：标识一个方法或类属于哪种组件，如 `[RewriteRule]`、`[Instruction]`。
- **元数据 Attribute**：提供附加信息，如 `[CostFor]`、`[StaticAware]`。
- **约束性 Attribute**：触发分析器检查，例如 `[MustCover]`。

### 2.2 源生成器工作流

```
源文件 (带 Attribute)
        │
        ▼
Roslyn 分析器 (实时诊断)
        │
        ▼
Source Generator 执行
        │
        ▼
生成 C# 代码 (添加到编译)
        │
        ▼
完整项目编译
```

- **分析器**：在编辑器中即时反馈错误，如规则变量未绑定、指令枚举项缺少操作数说明。
- **生成器**：在编译早期生成分发代码、Builder、解码器等，其输出可被项目其他部分引用。

### 2.3 命名约定

为保持与主文档一致，本补充文档采用以下命名：

- **代数接口**：`IArithAlgebra<T>`, `ITensorAlgebra<T>`, `IRelationalAlgebra<T>` 等，无 IKun 前缀。
- **规则标识**：`"i32.const-fold"`、`"tensor.conv-bn-fusion"` 等点分隔字符串，仅用于属性参数，不参与逻辑。
- **指令助记符**：`"i32.add"`, `"control.br"`，同样仅用于显示/调试。
- **枚举值**：`Add = 0x01` 等，在 `Instruction` 属性中给出。

---

## 3. 代数接口与意图图构建

### 3.1 接口定义

前端语言通过实现各种 `I*Algebra<T>` 接口来生成意图图。在 Nyar 内部，`T` 通常为 `ENode`（EGraph 节点 ID）。这些接口是 Object
Algebra 的核心，每个方法代表一种意图构造器。例如：

```csharp
public interface IArithAlgebra<T>
{
    T Int32(int value);
    T Float32(float value);
    T Add(T left, T right);
    T Sub(T left, T right);
    T Mul(T left, T right);
    T Div(T left, T right);
}

public interface ITensorAlgebra<T>
{
    T Conv2D(T input, T weight, Strides strides, Padding pad);
    T MatMul(T a, T b);
    T Relu(T x);
}
```

此处 `Strides`、`Padding` 是强类型的结构体或枚举，避免使用魔法数字。

### 3.3 静态检查

- 返回值 Attribute 必须与代数接口的一个方法匹配，否则编译错误。
- 参数 Attribute 引用的变量名（如 `"input"`）必须在方法参数列表中定义，否则 Analyzer 报错。
- 参数类型与代数接口期望的类型（如 `Strides`）不匹配时，生成器会产生编译错误。

---

## 4. 重写规则系统

### 4.1 目标与挑战

重写规则是 EGraph 优化的燃料。传统 egg 库使用字符串 S-expression 描述模式，易出现拼写错误且无法利用 IDE。Nyar 要求规则定义完全用
C# 方法体表达，模式与替换均采用强类型代数调用，源生成器从中提取模式结构并生成高效匹配器。

### 4.2 规则定义语法

规则定义在静态类中，使用 `[RewriteRule]` 标记。模式方法返回 `I*Algebra<int>` 或泛型载体 `R`，其中 `int`
或具体类型仅用于驱动语法结构，不是实际执行类型。Nyar 规定使用 `Var<T>` 表示模式变量。

```csharp
public static class ArithRules
{
    [RewriteRule(Name = "i32.const-fold")]
    public static IArithAlgebra<int> Pattern(IArithAlgebra<int> a, Var<int> x, Var<int> y)
        => a.Add(a.Int32(x), a.Int32(y));

    [RewriteReplacement]
    public static IArithAlgebra<int> Replacement(IArithAlgebra<int> a, Var<int> x, Var<int> y)
        => a.Int32(x + y);
}
```

为了更流畅的体验，可以使用运算符重载和 using static：

```csharp
using static Nyar.Rules.DSL; // 提供 Const, x, y 等工厂

[RewriteRule(Name = "i32.commutative")]
public static IArithAlgebra<int> Pattern(IArithAlgebra<int> a, Var<int> x, Var<int> y)
    => x + y;

[RewriteReplacement]
public static IArithAlgebra<int> Replacement(Var<int> x, Var<int> y)
    => y + x;
```

`x + y` 通过隐式转换为 `IArithAlgebra<int>` 的 `Add` 调用。源生成器分析表达式树，识别出这是一个 `Add` 节点。

### 4.3 Source Generator 处理流程

1. **扫描**：找到所有带 `[RewriteRule]` 的方法组（每个规则由模式+替换两个方法组成）。
2. **语法树提取**：解析模式方法体的 Expression Syntax。识别 `a.Add(...)` 等调用，提取操作符和子模式。对于运算符重载，还原为接口方法调用。
3. **变量分析**：收集所有 `Var<T>` 类型的参数，建立变量列表。确认所有变量在替换中都得到使用（或允许通配符丢弃）。
4. **生成匹配器**：为每个模式生成一个高效的 `IMatcher` 实现。匹配器在 EGraph 中搜索模式，使用 `EMatching` 技术（如无回溯的指令序列）。
5. **生成替换构建器**：将替换方法体翻译为 `ENode` 构造代码，直接生成新的 e-node。

生成的代码将被编译进当前程序集，供 EGraph 饱和循环使用。

### 4.4 静态检查（Analyzer）

Nyar 提供一个配套的 Roslyn Analyzer，在编辑时实施以下规则：

- **NRW001**：模式方法必须返回代数接口类型，且其类型参数与替换一致。
- **NRW002**：模式中使用的所有 `Var<T>` 必须在参数列表中声明；不允许未定义变量。
- **NRW003**：替换中引用的所有变量必须出现在模式中（闭合性）。
- **NRW004**：模式中不能调用非代数接口方法（防止副作用）。
- **NRW005**：规则名称在同一个程序集中必须唯一。
- **NRW006**：模式不能是纯变量（避免退化）。

这些规则确保了规则的正确性，无需等待运行时测试。

### 4.5 条件规则

通过额外参数引入条件表达式：

```csharp
[RewriteRule(Name = "i32.div-by-zero")]
public static IArithAlgebra<int> Pattern(IArithAlgebra<int> a, Var<int> x)
    => a.Div(x, a.Int32(0));

[RewriteGuard]
public static bool Guard(Var<int> x) => false; // 不能除零，规则无效，实际可通过静态分析

// 实际条件用法：使用额外约束参数
[RewriteRule(Name = "i32.add-zero")]
public static IArithAlgebra<int> Pattern(IArithAlgebra<int> a, Var<int> x)
    => a.Add(x, a.Int32(0));

[RewriteCondition(Pattern = "x.IsNonNegative")] // 例子
```

条件内部可使用 `Var<T>` 的成员，生成器翻译成 e-class 数据检查。

---

## 5. 指令集定义与生成

### 5.1 指令枚举设计

NyarStandard IR 指令集被定义为一个 `enum NyarStdOp : ushort`，每个成员附带 `[Instruction]` Attribute，提供助记符和操作数格式：

```csharp
public enum NyarStdOp : ushort
{
    [Instruction("i32.add", OpKind.RR | OpKind.RI)] Add = 0x01,
    [Instruction("i32.mul", OpKind.RR | OpKind.RI)] Mul = 0x02,
    [Instruction("control.br", OpKind.Label)]       Br = 0x10,
    [Instruction("control.brcond", OpKind.RR | OpKind.Label)] BrCond = 0x11,
    [Instruction("tensor.conv2d", OpKind.VarArgs)] Conv2D = 0x20,
    // ...
}
```

`OpKind` 枚举使用 `[Flags]`：

```csharp
[Flags]
public enum OpKind
{
    None = 0,
    RR = 1 << 0,    // 寄存器-寄存器
    RI = 1 << 1,    // 寄存器-立即数
    Label = 1 << 2, // 跳转目标
    VarArgs = 1 << 3 // 可变参数
}
```

### 5.2 源生成器输出

基于枚举定义，生成器产生以下代码：

#### a) 强类型发射器

```csharp
public class NyarStdEmitter
{
    private readonly List<Instruction> instructions = new();
    
    // 为每个 OpKind 组合生成方法
    public void EmitAdd_RR(Register dest, Register src1, Register src2)
    {
        instructions.Add(new Instruction(NyarStdOp.Add, dest, src1, src2));
    }
    public void EmitAdd_RI(Register dest, Register src1, int imm)
    {
        instructions.Add(new Instruction(NyarStdOp.Add, dest, src1, ImmOperand.Create(imm)));
    }
    // 类似 EmitBr(Label target) 等
}
```

方法名由源生成器根据助记符和 `OpKind` 组合自动合成，所有参数类型强类型。不提供不合法组合（如 `EmitAdd_Label` 不会生成）。

#### b) 解码器

```csharp
public static class NyarStdDecoder
{
    public static Instruction Decode(ReadOnlySpan<byte> code)
    {
        var op = (NyarStdOp)code[0];
        return op switch
        {
            NyarStdOp.Add => DecodeAdd(code),
            NyarStdOp.Mul => DecodeMul(code),
            // ... 穷尽匹配
            _ => throw new InvalidOperationException()
        };
    }
}
```

生成器确保 switch 涵盖所有枚举值，否则编译错误（利用 C# 的穷尽模式匹配警告作为错误）。

#### c) 反汇编器

```csharp
public static string Disassemble(Instruction instr)
{
    return instr.Op switch
    {
        NyarStdOp.Add => $"i32.add {instr.Dest}, {instr.Src1}, {instr.Src2}",
        // ...
    };
}
```

助记符从 `[Instruction]` 的字符串参数获取，仅用于显示。

### 5.3 操作数验证

`Instruction` 结构体存储操作数列表，其构造函数是生成的，根据操作数类型自动验证。如：

```csharp
public readonly struct Instruction
{
    public NyarStdOp Op { get; }
    private readonly Operand[] operands;
    // 生成的构造函数：
    internal Instruction(NyarStdOp op, Register dest, Register src1, Register src2) { ... }
    internal Instruction(NyarStdOp op, Register dest, Register src1, int imm) { ... }
}
```

外部只能通过发射器的方法创建指令，编译期保证操作数与 `OpKind` 一致。

### 5.4 扩展指令集

新增指令只需在枚举中加一项，提供正确的 `[Instruction]` 属性。生成器立即为该指令生成所有符合 `OpKind`
组合的发射方法、解码分支、反汇编字符串。如果遗漏某些必要信息（如忘记指定 `OpKind`），分析器报错。

---

## 6. 成本模型

### 6.1 基于接口的成本定义

成本模型接口直接与代数接口对应，方法名一致。例如：

```csharp
public interface ICostModel
{
    double Add(IReadOnlyList<double> childCosts);
    double Int32(IReadOnlyList<double> childCosts);
    double Conv2D(IReadOnlyList<double> childCosts);
    // ... 每个代数接口方法对应一个成本方法
}
```

该方法接口由 Source Generator 根据所有已知的 `I*Algebra` 接口自动生成一个 `partial interface ICostModel`。因此，当添加新的代数接口（如
`ITensorAlgebra`）时，`ICostModel` 随之扩展，所有已有的成本实现类会立即出现编译错误，要求实现新成员。

### 6.2 成本实现类

开发者实现 `ICostModel` 以提供具体成本：

```csharp
public class GpuCostModel : ICostModel
{
    public double Add(IReadOnlyList<double> childCosts) => 0.5 + childCosts.Sum();
    public double Int32(IReadOnlyList<double> childCosts) => 0.0;
    public double Conv2D(IReadOnlyList<double> childCosts) 
        => 100.0 + childCosts[0] * childCosts[1]; // 粗略
    // 必须实现所有方法
}
```

### 6.3 自动绑定

EGraph 提取时，需要根据 e-node 的操作符调用对应的成本方法。框架维护一个
`Dictionary<System.Reflection.MethodInfo, Func<IReadOnlyList<double>, double>>` 映射。此映射由生成器在静态构造函数中填充：

```csharp
static CostBinding()
{
    map[typeof(IArithAlgebra<>.Add)] = costModel.Add;
    // ...
}
```

绑定使用 `typeof` 和 `nameof` 的强类型引用，绝不使用字符串。若某个代数方法在 `ICostModel` 中没有对应实现，则编译失败。

### 6.4 诊断分析器

分析器检查所有成本实现类，确保：

- 实现了 `ICostModel` 的所有成员（接口本身强制）。
- 没有额外未绑定到任何代数方法的方法，可能提示命名错误。
- 成本方法参数类型正确。

---

## 7. 部分求值器

### 7.1 静态感知属性

部分求值器（PE）负责将解释器特化。传统实现需要大量手动判断“参数是静态还是动态”。Nyar 通过 `[StaticAware]` Attribute
简化：开发者只需写普通的解释逻辑，标记方法为静态感知，生成器自动生成优化分支。

示例：

```csharp
public class MyPE : IArithAlgebra<Code>
{
    [StaticAware]
    public Code Add(Code left, Code right)
    {
        // 编写时假设参数可能是静态或动态，生成通用代码
        return Code.Emit("i32.add", left, right);
    }
}
```

生成器为每个 `[StaticAware]` 方法生成一个包装器：

```csharp
Code Add_Wrapper(Code left, Code right)
{
    if (left.IsConstant && right.IsConstant)
    {
        // 静态求值：调用解释器的静态版本（由生成器根据方法体特化生成）
        return Code.Constant(EvaluateStaticAdd(left.Value, right.Value));
    }
    else
    {
        // 动态路径：调用用户编写的原始方法
        return user.Add(left, right);
    }
}
```

### 7.2 静态求值生成

生成器分析 `[StaticAware]` 方法体，尝试推导“如果所有参数都是常量，结果是什么”。对于简单情况（如 `Code.Emit("i32.add", a, b)`
），生成器知道该操作在静态时可以直接计算（`a + b`）而不发射指令。这需要在生成器内建针对常用指令的静态解释逻辑，或者由用户通过
`[StaticInterpretation]` 提供静态实现：

```csharp
[StaticAware]
[StaticInterpretation(typeof(MyStaticInterp))]
public Code Add(Code left, Code right) => ...;

class MyStaticInterp : IArithAlgebra<int>
{
    public int Add(int l, int r) => l + r;
}
```

这样，生成器可以用 `MyStaticInterp.Add` 处理常量折叠。

### 7.3 诊断

- 所有参数都必须通过 `Code` 类型传递；若方法签名包含非 Code 参数，分析器警告。
- `[StaticAware]` 方法必须属于实现了某代数接口的类。

---

## 8. 后端降级框架

### 8.1 抽象基类

每个目标后端定义一个抽象基类，继承自 `BackendBase<T>`，其中 `T` 是后端特定指令类型（如 `CLRILCode`, `CudaKernel`）。基类为所有
NyarStd 指令提供抽象方法：

```csharp
public abstract class BackendBase<T>
{
    // IArithAlgebra 对应的降级方法
    public abstract T Add(T left, T right);
    public abstract T Int32(int v);
    // ITensorAlgebra 对应的方法
    public abstract T Conv2D(T input, T weight, Strides strides, Padding pad);
    // 控制流
    public abstract T Br(Label target);
    public abstract T BrCond(T cond, Label trueLbl, Label falseLbl);
    // ... 涵盖所有可能的意图节点和 NyarStd 指令
}
```

这些方法由生成器根据所有已知代数接口和指令集自动生成在 `BackendBase<T>` 中，确保完备性。

### 8.2 实现后端

具体后端只需继承并实现所有抽象方法：

```csharp
public class GpuBackend : BackendBase<CudaKernel>
{
    public override CudaKernel Add(CudaKernel l, CudaKernel r) => CudaKernel.Binary("add", l, r);
    public override CudaKernel Int32(int v) => CudaKernel.Constant(v);
    // 必须全部实现
}
```

如果忘记实现某个方法，编译器立即报错。

### 8.3 默认实现与选择性覆盖

为了减轻开发负担，基类可以为常见情况提供虚方法而非抽象。例如，`Int32` 常量降级在多数后端都是生成常量加载，基类可提供
`public virtual T Int32(int v) => DefaultConstant(v);`。开发者可以重写或使用默认。

但通过 Analyzer，框架可以配置为“强制覆盖”，确保所有关键指令都被显式实现，避免因默认行为导致性能问题。

### 8.4 多后端成本模型切换

后端降级器本身也可以用于成本模型：基类提供 `CostFor` 方法，根据后端特性返回代价。例如 `GpuBackend` 可重写
`GetCostFor(IArithAlgebra.Add)` 返回 GPU 特定成本，从而在 EGraph 提取时自动选择最佳后端。

---

## 9. 全局一致性验证

### 9.1 跨模块引用检查

Nyar 编译流程中，不同的组件（规则、成本、后端）可能分布在不同的项目或文件中。Source Generator 可以在整个编译范围内收集信息，执行以下全局验证：

- **未覆盖成本节点**：检查所有可能出现在规则中的代数操作，是否在成本模型中有对应方法。如果缺少，生成编译警告（或错误）并提示添加。
- **未降级指令**：检查所有 `NyarStdOp` 枚举值是否在后端基类中有对应的抽象/虚方法。实际上基类由生成器自动生成，保证覆盖，但若有手动添加的指令未及时更新生成器配置，则报错。
- **规则冲突检测**：分析规则模式，发现两个规则产生相同模式的替换，且成本无法区分，可能导致提取非确定性，发出警告。

---

## 11. 示例：构建一个 DSL 优化管道

假设我们要实现一个简单的算术 DSL 优化器，目标后端是打印数学表达式。

### 11.1 定义代数接口

```csharp
public interface IArithAlgebra<T>
{
    T Int(int v);
    T Add(T l, T r);
    T Mul(T l, T r);
}
```

### 11.2 定义指令集（本示例不需要虚拟机，故省略）

### 11.3 编写重写规则

```csharp
public static class Rules
{
    [RewriteRule(Name = "arith.const-fold")]
    public static IArithAlgebra<int> Pattern(IArithAlgebra<int> a, Var<int> x, Var<int> y)
        => a.Add(a.Int(x), a.Int(y));

    [RewriteReplacement]
    public static IArithAlgebra<int> Replacement(Var<int> x, Var<int> y)
        => a.Int(x + y);

    [RewriteRule(Name = "arith.add-comm")]
    public static IArithAlgebra<int> CommPattern(IArithAlgebra<int> a, Var<int> x, Var<int> y)
        => x + y; // 重载

    [RewriteReplacement]
    public static IArithAlgebra<int> CommReplacement(Var<int> x, Var<int> y)
        => y + x;
}
```

### 11.4 成本模型

```csharp
public class SimpleCost : ICostModel
{
    public double Int(IReadOnlyList<double> c) => 0;
    public double Add(IReadOnlyList<double> c) => 1 + c.Sum();
    public double Mul(IReadOnlyList<double> c) => 2 + c.Sum();
}
```

### 11.5 后端降级

```csharp
public class ExprPrinter : BackendBase<string>
{
    public override string Int(int v) => v.ToString();
    public override string Add(string l, string r) => $"({l} + {r})";
    public override string Mul(string l, string r) => $"({l} * {r})";
}
```

### 11.6 集成与编译

提交意图图，EGraph 使用规则进行饱和，成本模型提取最优图，部分求值器将优化后的图传递给 `ExprPrinter`
生成字符串。所有规则、成本、后端的正确性都在编译期得到保证。若以后添加 `Sub` 操作，只需在 `IArithAlgebra` 添加方法，此时
`ICostModel` 自动要求实现 `Sub` 成本，`BackendBase<string>` 强制要求实现 `Sub` 降级，否则项目编译失败。这确保了 DSL 演进的同步性。

---

## 12. 附录

### 附录 A：诊断码完整列表

（扩展第 9 节内容，列出所有 NRW/NRI/NRC/NRB/NRA 代码，每个带有详细解释和建议。）

### 附录 B：Source Generator 技术细节

- 使用 `IIncrementalGenerator` 提高性能。
- 规则模式分析采用 Roslyn 语法树访问器，识别已知代数接口调用。
- 生成的代码使用 `partial` 类与用户代码合并。
- 支持多语言（C#、F#）前端（通过语言无关的 Attribute 模型，但生成器目前针对 C#）。

### 附录 C：API 快速参考

- `[RewriteRule(Name = string name)]`：标记模式方法。
- `[RewriteReplacement]`：标记替换方法。
- `[RewriteGuard]`：标记条件方法（可选）。
- `[Instruction(string mnemonic, OpKind kind)]`：标记指令枚举成员。
- `[CostFor(Type algebraInterface, string methodName)]`：手动绑定成本（通常不需要，自动绑定已处理）。
- `[StaticAware]`：启用部分求值优化。
- `[IntentExpression]`：标记意图构建方法（高级用法，一般前端 Parser 直接使用代数接口）。

---

## 13. 结语

本补充文档详尽阐述了 Nyar 的 Attribute + Source Generator 体系如何彻底消除字符串依赖和手动注册，将元编译器内部组件的开发转变为强类型、IDE
友好的体验。通过编译期代码生成与静态分析，Nyar 实现了“写即正确”的极致可靠性，同时保留了 Object Algebra
提供的无限可扩展性。这一设计为构建下一代语言与优化器框架开辟了工业级的工程实践路径。

---

**文档结束**  
*本补充文档与《Nyar 架构完整设计规范》主文档配合使用，构成 Nyar 框架的完整设计蓝图。*