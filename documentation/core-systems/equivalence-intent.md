# 等价意图

## 从 OpCode 到多态函数

### 旧范式：意图 = 数据结构

旧架构中，"意图"是一个具体的数据结构——`IIntentNode` 包含 `OpCode` 枚举和 `IReadOnlyList<IOperand>`。优化器扫描这个结构，匹配模式，应用变换。这带来了根本性问题：

- 构造和操作紧耦合：构造程序的代码与消费程序的代码使用同一套类型
- 无法在编译期区分"构造时"和"分析时"
- Visitor 模式遍历是唯一能与意图结构交互的方式

### 新范式：意图 = 多态程序函数

在 OA 架构下，意图不再是一个具体的数据结构，而是一个**接受工厂接口的多态函数**：

```csharp
// 意图：不依赖于任何具体表示
static E SortIntent<ISortAlg<E>>(ISortAlg<E> alg, E input) =>
    alg.Sort(input);

static E JoinIntent<IQueryAlg<E>>(IQueryAlg<E> alg, E left, E right, E condition) =>
    alg.Join(left, right, condition);

static E ConvIntent<ITensorAlg<E>>(ITensorAlg<E> alg, E input, E weights) =>
    alg.Conv2D(input, weights, alg.Const(3), alg.Const(1)); // stride=3, padding=1
```

**关键洞察**：程序不"是" AST，程序不"是"优化图。程序是一个多态函数，其具体表示由传入的工厂实例决定。

## 等价的定义

### OA 下的等价语义

两个意图 `P1<Alg<E>>` 和 `P2<Alg<E>>` 等价，当且仅当**对于所有可能的工厂实现 `alg`**，`P1(alg)` 和 `P2(alg)` 在所有可观测行为上无法区分。

在实践中，等价通过**公理**和**重写规则**来声明：

```csharp
// 声明等价规则：Add(a, b) ≈ Add(b, a)（交换律）
engine.Rule<ICoreAlg<EClassId>>(alg => alg.Add(alg.Any<int>(), alg.Any<int>()))
      .Replace((a, b) => alg.Add(b, a));

// 声明等价规则：Sort(Sort(list)) ≈ Sort(list)（幂等律）
engine.Rule<IDataAlg<EClassId>>(alg => alg.Sort(alg.Sort(alg.Any())))
      .Replace((list) => alg.Sort(list));
```

### 等价层次

意图可以有三个抽象层次，**共存于同一 EGraph 中**：

| 层级 | 特征 | 示例 OA 方法 |
|:---|:---|:---|
| **HIR**（What） | 领域语义，接近用户意图 | `alg.Sort(list)`, `alg.Join(R, S, cond)`, `alg.Conv2D(img, kernel)` |
| **MIR**（How） | 控制流/数据流，算法选择 | `alg.ForLoop(i, n, body)`, `alg.QuickSort(list)`, `alg.MatMul(A, B)` |
| **LIR**（Where） | 机器资源，指令级操作 | `alg.Load(addr)`, `alg.SIMD_Add(a, b)`, `alg.Branch(cond, t, f)` |

"磁感线"隐喻依然成立：HIR、MIR、LIR 节点可以在同一 EGraph 中共存，等价重写引擎在所有层级间自由穿梭——既可以从高层降级，也可以从低层升格。

### 等价规则的来源

| 来源 | 示例 | 适用性 |
|:---|:---|:---|
| **代数公理** | 交换律、结合律、分配律 | 所有方言 |
| **方言语义** | `Conv2D + BatchNorm + ReLU → FusedConvBNReLU` | 特定方言 |
| **目标架构** | `Mul(a, 2) → ShiftLeft(a, 1)` | 特定后端 |
| **固化知识** | PE 发现的最优分块参数 | 从运行中学习 |
| **用户声明** | 自定义领域规则 | 应用特定 |

## 成本模型

### OA 下的成本模型

成本模型本身也是一个 OA 工厂：

```csharp
public interface ICostModel<E>
{
    CostVector Cost(E node);
    OptimizationGoal Goal { get; }
}

// 延迟优先的成本模型
public class LatencyCostModel : ICostModel<EClassId>
{
    public CostVector Cost(EClassId id) => egraph.GetCost(id);
    public OptimizationGoal Goal => OptimizationGoal.MinimizeLatency;
}
```

成本向量 `CostVector` 是多元组：时间、空间、能量、带宽……用户可指定约束（如"时间最优但空间不超过 100MB"）。

### 优化即选择

优化器的任务是从每个等价类中选择成本最低的表示：

```
等价类: { QuickSort, MergeSort, RadixSort, InsertionSort }
  │
  ▼ 成本模型评估
  │
  ├─ 小输入 (n<100):   InsertionSort 成本最低  → 选择
  ├─ 中等输入:          QuickSort 成本最低     → 选择
  └─ 大输入 + 整数键:   RadixSort 成本最低     → 选择
```

优化器根据静态分析和 profiling 数据估计输入规模，自动做出选择。
