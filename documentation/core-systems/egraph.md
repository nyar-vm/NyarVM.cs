# EGraph 饱和优化引擎

## EGraph 作为 OA 工厂

在 Nyar 的 OA 架构中，EGraph 引擎是**方言接口的一种工厂实现**。它将 `E` 绑定为 `EClassId`（等价类标识符），每个 OA 方法调用都在 EGraph 中创建一个节点并加入对应的等价类。

```csharp
// 方言接口（唯一定义）
public interface ICoreAlg<E>
{
    E Int32(int value);
    E Add(E left, E right);
    E Mul(E left, E right);
}

// EGraph 工厂实现：E = EClassId
public class CoreEgraphBuilder : ICoreAlg<EClassId>
{
    private readonly EGraph _egraph;

    public EClassId Int32(int value) =>
        _egraph.Add(new ENode(Op.Int32, value));

    public EClassId Add(EClassId left, EClassId right) =>
        _egraph.Add(new ENode(Op.Add, left, right));

    public EClassId Mul(EClassId left, EClassId right) =>
        _egraph.Add(new ENode(Op.Mul, left, right));
}
```

程序函数无需任何修改即可送入 EGraph：

```csharp
static E Program<ICoreAlg<E>>(ICoreAlg<E> alg) =>
    alg.Add(alg.Int32(1), alg.Mul(alg.Int32(2), alg.Int32(3)));

var egraphBuilder = new CoreEgraphBuilder();
var rootId = Program(egraphBuilder);    // 返回 EClassId
egraphBuilder.Saturate(rules);          // 饱和优化
var optimal = egraphBuilder.Extract(rootId, costModel); // 提取最优
```

## 为什么需要 EGraph

传统编译器使用哈希一致性（hash consing）共享语法相同的表达式，但无法处理**语义等价**。EGraph 通过 union-find 将等价表达式合并为 eclass，每个 eclass 包含多个 enode（具体语法形式）。重写规则将新的 enode 加入 eclass，逐步扩展等价集。

**饱和优化**：重复应用规则直到没有新 enode 产生。此时，每个 eclass 包含了所有可推导的等价形式。

## 核心 API

```csharp
public class EGraph
{
    // 添加节点，返回其 eclass
    public EClass Add(ENode node);

    // 合并两个 eclass（声明等价）
    public void Union(EClass a, EClass b);

    // 饱和：重复应用规则直到不动点
    public void Saturate(IReadOnlyList<IRewriteRule> rules);

    // 从 eclass 中提取成本最低的表示
    public ENode Extract(EClass root, ICostModel costModel);
}

public readonly record struct EClass(int Id);
public readonly record struct ENode(OpCode Op, params EClass[] Children);
```

## 与成本模型的集成

每个 enode 关联一个成本（可动态计算）。提取最优表示时，从每个 eclass 中选择最低成本的 enode，递归构建表达式。

对于循环和递归，成本模型需要处理规模（如循环次数）。可以使用符号成本或分析模型。

## 驯服 EGraph 膨胀

OA 架构为膨胀控制带来了新手段：

### 工厂级过滤

不同 OA 工厂实现自然地过滤了不相关节点。PE 工厂只处理静态已知部分，IDE PSI 工厂只关注语法结构——它们不会把对方关心的节点放入 EGraph。

### 上下文敏感规则激活

每条规则携带**能力门**（Capability Gate）：

```csharp
rule InsertionSort_Lowering
    when (input.Size < 64) && (input.ElementType == I32)
    apply (Sort input) -> (InsertionSort input)
```

PE 引擎在添加规则前计算静态已知条件，过滤掉不相关规则。

### 渐进式成本剪枝

Nyar 在每次规则应用后为每个 e-class 计算成本下界。如果某个 e-node 的成本下界已高于该 e-class 的最优已知成本，它被标记为 Dead，后续规则不再将其作为匹配根。

例如：基数排序在小数据上常数开销大，当成本模型检测到 n < 128 时，基数排序变体会被立即剪枝。

### 提取时的智能搜索

不需要让 EGraph 完全饱和。只需让 EGraph 包含足够多的等价类成员，使提取算法能用**束搜索**（Beam Search）或**蒙特卡洛树搜索**（MCTS）找到近似最优解。

Nyar 的提取器是一个规划器，可以在提取过程中动态实例化某些降级规则。
