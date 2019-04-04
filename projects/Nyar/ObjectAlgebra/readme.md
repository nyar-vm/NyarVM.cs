# Nyar Object Algebra 设计文档

## 实施状态

| 组件                  | 状态      | 说明                                                    |
|:----------------------|:----------|:--------------------------------------------------------|
| `OperatorKey`         | ✅ 已实现 | `Nyar.ObjectAlgebra.OperatorKey`                        |
| `IOperatorDescriptor` | ✅ 已实现 | `Nyar.ObjectAlgebra.IOperatorDescriptor`                |
| `OperatorDescriptor`  | ✅ 已实现 | `Nyar.ObjectAlgebra.OperatorDescriptor`                 |
| `ENode`               | ✅ 已实现 | `Nyar.ObjectAlgebra.ENode`                              |
| `IDialectDefinition`  | ✅ 已实现 | `Nyar.ObjectAlgebra.IDialectDefinition`                 |
| `IDialectRuntime`     | ✅ 已实现 | `Nyar.ObjectAlgebra.IDialectRuntime`                    |
| `IReifier<E>`         | ✅ 已实现 | `Nyar.ObjectAlgebra.IReifier<E>`                        |
| `IPatternAlg<P>`      | ✅ 已实现 | `Nyar.ObjectAlgebra.IPatternAlg<P>`                     |
| `IPatternBuilder<P>`  | ✅ 已实现 | `Nyar.ObjectAlgebra.IPatternBuilder<P>`                 |
| `RewriteRule<T>`      | ✅ 已实现 | `Nyar.IR.Rewrite.RewriteRule<T>`                        |
| `DialectGenerator`    | ✅ 已重构 | 生成 Symbols/Reifier/PatternAlg/RuleDSL，不再生成节点类 |
| `AlgebraNode`         | ⛔ 已冻结 | `[Obsolete]`，禁止新增子类                              |
| `IDialect` (旧)       | ⛔ 已冻结 | `[Obsolete]`，请使用 `IDialectRuntime`                  |

## 1. 核心立场

Nyar 的 `Object Algebra` 不是“换一种语法写 AST/IR”，而是要彻底放弃“全局节点宇宙”这一思路。

真正的 OA 有三条不可退让的原则：

1. 语言构造以 **方言接口签名** 为中心，而不是以节点类层级为中心。
2. 程序首先是 **对某个 algebra 的多态构造**，不是某棵统一语法树。
3. 任何需要落地到存储、优化、持久化、EGraph 的 reification，都必须是 **开放的实现层**，不能重新退化成封闭 `enum` / `union` /
   `base class + record zoo`。

如果新增一个操作符需要修改中央节点类型，那么这不是 OA。

如果新增一个分析 Pass 需要给一个统一节点宇宙继续加 `switch`，这也不是 OA。

## 2. 要解决的问题

传统 IR 的困境在于它把两件本应分离的事情绑死在一起：

- **语义构造**：语言作者如何表达程序。
- **结构存储**：优化器和后端如何保存、比较、匹配这些程序。

一旦两者绑定为一个封闭节点体系，就会反复落入同样的困境：

- 加新语法要改中央节点定义。
- 加新方言要改中央匹配器、访问器、序列化器。
- 加新操作要改已有 Pass。
- 想做跨项目扩展时，会被一个“必须回到核心仓库改节点”的模型卡死。

Nyar 的正确方向不是“让节点生成自动化”，而是：

- 上层用 OA 表达语义。
- 下层用开放 reification 支持优化与存储。
- 两层之间通过受控桥接连接。

## 3. 双层架构

Nyar 的 OA 体系必须是一个双层系统。

### 3.1 上层：Final Encoding / Tagless Final

这是唯一的语义真相层。

- 每个方言通过 `[Dialect]` 标注的接口定义。
- 每个操作符只是接口中的一个方法。
- 程序是“可被任意 algebra 解释的构造过程”。
- 求值、格式化、高亮、类型检查、代码生成、部分求值，全部表现为 algebra 的实现。

这一层的关键词是：

- 开放语法
- 开放解释
- 不依赖统一节点类

### 3.2 下层：开放 Reification / EGraph Storage

这是实现层，不是语义中心。

- 用于 EGraph、持久化、缓存、规则匹配、跨进程边界。
- 节点由 `OperatorKey + children + payload` 表达。
- 新方言通过注册新的 operator descriptor 扩展。
- 不允许存在一个封闭的 `AlgebraNode` / `IKun` 子类宇宙来列举所有节点。

这一层的关键词是：

- 开放 operator 注册
- 统一存储
- 非封闭节点宇宙

### 3.3 两层边界

这两层绝不能混淆。

- OA 层负责“程序是什么意思、如何被构造、如何被解释”。
- Reification 层负责“程序如何被放进 EGraph、如何被匹配、如何被缓存和提取”。

换句话说：

- **程序不是节点树**
- **节点树只是某种实现层投影**

## 4. 核心抽象

### 4.1 `Term<T>` 只是类型占位，不是节点

```csharp
public readonly struct Term<T>
{
}
```

`Term<T>` 只承担“方言接口中的静态类型标记”职责。

它不是运行时节点，也不应该偷偷承载 `object? value` 或其他树结构引用。它的存在只是为了让开发者在声明方言接口时表达：

- 输入是什么类型
- 输出是什么类型
- 操作符之间的类型关系是什么

### 4.2 `[Dialect]` 与 `[Operator]`

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public sealed class DialectAttribute : Attribute
{
    public DialectAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class OperatorAttribute : Attribute
{
    public OperatorAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
```

它们的职责只有声明，不负责生成一整套封闭节点系统。

### 4.3 方言接口是签名，不是节点定义

```csharp
[Dialect("arith")]
public partial interface IArith<T>
{
    [Operator("const_i64")]
    Term<long> Const(long value);

    [Operator("add_i64")]
    Term<long> Add(Term<long> left, Term<long> right);

    [Operator("mul_i64")]
    Term<long> Mul(Term<long> left, Term<long> right);
}
```

源生成器据此生成的是 algebra 签名，而不是节点基类的子类列表：

```csharp
public interface IArithAlg<E> : IAlgebra<E>
{
    E Const(long value);
    E Add(E left, E right);
    E Mul(E left, E right);
}
```

### 4.4 `IAlgebra<E>` 只是统一标记

```csharp
public interface IAlgebra<E>
{
}
```

它只承担“这是一个 algebra”的角色，不承担节点访问器、判别联合、共享基类等传统 IR 职责。

## 5. 程序的真正表示

程序在 OA 中首先不是 `Node`，而是“可被应用到任意 algebra 上”的值。

最小模型如下：

```csharp
public interface IArithTerm<T>
{
    E Apply<E>(IArithAlg<E> alg);
}
```

更一般地，源生成器可以为每个方言聚合接口生成统一的 term 入口：

```csharp
public interface IStandardTerm<T>
{
    E Apply<E>(IStandardAlg<E> alg);
}
```

其中 `IStandardAlg<E>` 由多个子方言 algebra 组合而成：

```csharp
public interface IStandardAlg<E> :
    ICoreAlg<E>,
    IControlAlg<E>,
    IEffectAlg<E>,
    IDataAlg<E>
{
}
```

这意味着：

- 加新语法 = 扩一个新方言接口或聚合接口
- 加新解释 = 写一个新的 algebra 实现
- 不需要改中央节点类型

## 6. 多维解释器

同一个程序可以被不同 algebra 消费，得到不同结果。

### 6.1 求值器

```csharp
public sealed class ArithEval : IArithAlg<long>
{
    public long Const(long value)
    {
        return value;
    }

    public long Add(long left, long right)
    {
        return left + right;
    }

    public long Mul(long left, long right)
    {
        return left * right;
    }
}
```

### 6.2 Pretty Printer

```csharp
public sealed class ArithPretty : IArithAlg<string>
{
    public string Const(long value)
    {
        return value.ToString();
    }

    public string Add(string left, string right)
    {
        return $"({left} + {right})";
    }

    public string Mul(string left, string right)
    {
        return $"({left} * {right})";
    }
}
```

### 6.3 类型检查、Linter、格式化、代码生成

它们都应该是 algebra 实现，而不是统一节点树上的访问器。

如果某个实现必须依赖结构检查，也应该先明确地进入 reification 层，而不是把 OA 偷偷变回 AST。

## 7. 开放 Reification：真正可用于 EGraph 的模型

EGraph、缓存、规则匹配确实需要一个可哈希、可比较、可持久化的结构表示。但这个表示必须是开放的。

### 7.1 不允许的错误做法

以下设计全部不合格：

- 一个全局 `abstract record AlgebraNode`
- 一个统一的 `IKun` 判别联合，内部列举所有操作符
- 为每个 operator 生成 `sealed record Xxx : AlgebraNode`
- 通过 `switch` / `is` / `visit` 把所有方言硬塞进一个 closed-world 体系

这只是“自动生成的传统 IR”，不是 OA。

### 7.2 正确的开放节点模型

```csharp
public readonly record struct OperatorKey(Guid DialectId, int OperatorId);

public interface IOperatorDescriptor
{
    OperatorKey Key { get; }
    string Name { get; }
    int Arity { get; }
    Type? PayloadType { get; }
}

public sealed record ENode(
    IOperatorDescriptor Operator,
    ImmutableArray<Id> Children,
    object? Payload);
```

这个模型的关键是：

- 节点结构是统一的
- operator 集合是开放的
- 新方言只需注册新的 descriptor
- 中央存储层无需知道未来所有 operator

### 7.3 `DialectRegistry` 的正确职责

`DialectRegistry` 不是“统一节点类型的目录”，而是运行时总线：

- 注册 dialect metadata
- 注册 operator descriptor
- 注册规则、降级关系、成本钩子
- 验证依赖图
- 构建聚合规则与成本模型

示意接口：

```csharp
public interface IDialectDefinition
{
    string Name { get; }
    Guid Id { get; }
    IReadOnlyList<IOperatorDescriptor> Operators { get; }
}

public interface IDialectRuntime
{
    IDialectDefinition Definition { get; }
    IReadOnlyList<IRewriteRule> Rules { get; }
    IReadOnlyList<ICostModelHook> CostHooks { get; }
    IReadOnlyList<string> LoweringTargets { get; }
}
```

## 8. OA 到 EGraph 的桥接

OA 不排斥 EGraph，但进入 EGraph 必须是显式 `reify`，而不是直接把 OA 语义等同于统一节点类。

### 8.1 Reifier

```csharp
public sealed class ArithReifier : IArithAlg<Id>
{
    private readonly EGraph<ENode> _graph;

    public ArithReifier(EGraph<ENode> graph)
    {
        _graph = graph;
    }

    public Id Const(long value)
    {
        return _graph.Add(ArithSymbols.Const(value));
    }

    public Id Add(Id left, Id right)
    {
        return _graph.Add(ArithSymbols.Add(left, right));
    }

    public Id Mul(Id left, Id right)
    {
        return _graph.Add(ArithSymbols.Mul(left, right));
    }
}
```

这里的 `ArithSymbols` 返回开放 `ENode`，而不是 `new AlgebraNode.Add(...)`。

### 8.2 Extractor 的正确方向

Extractor 的输出不应该被重新绑回一个封闭 IR。

正确的做法有两种：

1. 提取成某个方言聚合 term，再交给后续 algebra 消费。
2. 提取成开放 `ENode` 树，再由专门的 `rebuild algebra` 桥接回 final 形式。

无论哪种方式，都不应该要求“先回到一个全局节点类宇宙”。

## 9. 重写规则的正确写法

规则同样不能依赖一个统一节点类宇宙。

### 9.1 错误写法

```csharp
public sealed class AddZeroRule : IRewriteRule<AlgebraNode>
{
    public bool TryApply(AlgebraNode node, out AlgebraNode result)
    {
        if (node is AlgebraNode.Add(var left, var right) &&
            left is AlgebraNode.Const(0))
        {
            result = right;
            return true;
        }

        result = node;
        return false;
    }
}
```

这依然是传统 IR。

### 9.2 正确写法

规则应基于开放 pattern algebra 或 descriptor 驱动 matcher。

```csharp
public interface IArithPatternAlg<P>
{
    P Any(string name);
    P Const(long value);
    P Add(P left, P right);
    P Mul(P left, P right);
}

public static class ArithRules
{
    public static RewriteRule AddZeroLeft(IArithPatternAlg<Pattern> p)
    {
        var x = p.Any("x");
        return RewriteRule.Create(
            p.Add(p.Const(0), x),
            x);
    }
}
```

规则系统对中央内核的要求应该只有：

- 能注册 operator descriptor
- 能构造 pattern
- 能执行替换

而不是“知道每个方言的所有节点类”。

## 10. Source Generator 的正确职责

源生成器应该生成这些内容：

- `I<Dialect>Alg<E>`
- 聚合 algebra 接口
- `I<Dialect>Term<T>` 或等价 term 入口
- `OperatorDescriptor` / `Symbols`
- `Reifier`
- `PatternAlg`
- `Rule DSL`
- 从开放 `ENode` 重建为 algebra 调用的桥接器

源生成器不应该生成这些内容：

- `abstract AlgebraNode`
- `sealed record Add : AlgebraNode`
- `IKun` 风格统一节点判别联合
- 任何必须随着方言增长而持续修改的中央节点宇宙

## 11. 完整管线

正确的全管线如下：

1. 文本前端解析源码，得到前端 AST。
2. 前端 AST lowering 到某个 dialect aggregate term。
3. term 可被不同 algebra 消费：
  - evaluator
  - type checker
  - formatter
  - codegen
  - reifier
4. `reifier` 将 term 投影为开放 `ENode` 并写入 `EGraph<ENode>`。
5. 规则系统在 `EGraph<ENode>` 上工作，依赖 descriptor 和 pattern algebra，而不是封闭节点类。
6. extractor 从 `EGraph<ENode>` 中提取最佳程序。
7. 提取结果桥接回 term 或直接桥接到目标后端 algebra。

这里没有任何一步要求“必须回到一个统一节点大类”。

## 12. 工程准则

以下约束必须长期成立：

- 禁止新增 `AlgebraNode` 式中央节点宇宙。
- 禁止新增 `IKun` 式全局判别联合来列举全部方言节点。
- 禁止 Source Generator 产出“方言节点类 zoo”作为核心语义模型。
- 禁止规则系统以 `switch` 中央节点类型为扩展机制。
- 禁止后端以“识别所有中央节点子类”为唯一输入方式。

允许的扩展方式只有：

- 扩展方言接口
- 扩展 algebra 实现
- 扩展 descriptor 与 symbols
- 扩展 pattern algebra
- 扩展 runtime registry

## 13. 一句话总结

Nyar 的 OA 不是“用接口声明，再自动生成一套更漂亮的节点类”。

Nyar 的 OA 应当是：

- **上层以 final encoding 解决 expression problem**
- **下层以开放 reification 支撑优化与存储**
- **两层通过桥接连接，但绝不重新坍缩回封闭 IR**
