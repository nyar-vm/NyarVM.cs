# 部分求值

## 部分求值作为 OA 工厂

在 Nyar 的 OA 架构中，部分求值引擎是**方言接口的另一种工厂实现**。它将 `E` 绑定为三元组 `(bool IsStatic, object StaticValue, object? Residual)`：

- **IsStatic = true**：该子表达式在编译期完全已知，`StaticValue` 为计算结果
- **IsStatic = false**：该子表达式依赖运行时值，`Residual` 为保留的待编译结构

```csharp
// PE 工厂：E = 静态/动态 二元组
public class CorePEBuilder : ICoreAlg<(bool Static, object Val, object? Residual)>
{
    public (bool, object, object?) Int32(int value) =>
        (true, value, null);

    public (bool, object, object?) Add(
        (bool s1, object v1, object? r1) left,
        (bool s2, object v2, object? r2) right)
    {
        if (left.s1 && right.s1)
        {
            // 两边都静态 → 直接计算
            return (true, (int)left.v1 + (int)right.v1, null);
        }
        else
        {
            // 有动态部分 → 生成残余节点
            return (false, 0, new AddResidual(left, right));
        }
    }
}
```

## 为什么需要 PE

等价意图优化在语法层面变换，但有些优化需要利用**编译时已知的值**。例如 `pow(x, 2)` 在 `x` 是常量时可以完全求值；在 `x` 是变量时可以特化为 `x*x`。

PE 通过**特化**和**残余化**，将程序分为静态部分（编译期计算）和动态部分（生成代码）。

## PE 与 EGraph 的协同

在 OA 架构中，PE 和 EGraph 是**同一方言接口的两个工厂实现**。协同方式如下：

```
程序函数: static E P<IDialectAlg<E>>(IDialectAlg<E> alg) => ...

   ├──► 传入 PEBuilder ──► 输出: (Static, Val, Residual)
   │         │
   │         ├─ 静态部分 → 直接替换为常量
   │         └─ 动态部分 → 转为特化版本
   │
   └──► 特化版本传入 EGraphBuilder ──► 加入等价类
              │
              └─ 成本模型比较特化版 vs 通用版
```

1. **PE 识别静态已知的意图节点**（如常量参数），创建特化版本
2. **特化版本被加入 EGraph**，与通用版本共存
3. **成本模型决策**：比较特化带来的加速收益 vs 代码膨胀代价

这种协同使 Nyar 能**自动发现特化机会**，无需手动编写特化规则。

## Futamura 投影

Nyar 将 PE 纳入 Futamura 投影的理论框架：

### 第一投影：编译器生成

给定一个解释器（OA 工厂实现）和一个源程序（OA 程序函数），PE 将解释器特化到该程序上，生成目标代码：

```csharp
// 解释器：一种 OA 工厂 (E = Value)
var interpreter = new CoreEval(); // ICoreAlg<Value>

// 程序：多态函数
static E Program<ICoreAlg<E>>(ICoreAlg<E> alg) =>
    alg.Mul(alg.Int32(6), alg.Int32(7));

// 第一 Futamura 投影：特化解释器到程序 → 编译后代码
var compiled = FutamuraProjection.Specialize(interpreter, Program);
// compiled 等价于一个返回 42 的函数
```

### 第二投影：编译器生成器

将第一投影的结果再特化到解释器上，生成一个**编译器生成器**——接受任意程序，输出其编译版本。

### 第三投影：编译器-编译器生成器

进一步特化，生成能生成编译器生成器的工具。

在 Nyar 中，第一、第二投影可以直接通过 OA 工厂组合实现。

## 应用：Shape 特化与循环展开

深度学习模型中，batch size 通常在推理时固定。PE 可将卷积核的循环边界特化为常量，展开循环，生成无分支代码。SQL 查询中的常量谓词可提前求值。

## 固化机制

若某个特化版本被反复使用，Nyar 可将其序列化为新的重写规则，永久加入知识库：

```
发现：对于特定矩阵形状，MatMul 的最优分块参数始终是 (64, 64, 8)
  → 生成规则：(MatMul (Shape 1024 1024) ...) -> (TiledMatMul 64 64 8 ...)
```

固化的规则可通过包管理器分发，形成社区知识库，使新人无需从头搜索。
