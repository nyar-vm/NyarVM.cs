# Valkyrie.Compiler.Mir

`MIR` 是 `Valkyrie` 的共享优化层。 这一层的目标不是贴近语法，也不是直接贴近某个目标平台，而是把 `HIR` 降成可统一分析、优化、抽取的中层语义。

当前 `MIR` 的核心承载是 `EGraph<IKun>`。

## MIR 的一句话定义

`MIR` 负责把“语言级显式语义”转换成“后端共享、可重写、可提取”的中间表示。

## 为什么需要 MIR

如果从 `HIR` 直接落到 `LIR`，会有两个明显问题：

- 共享优化机会太少，很多控制流和表达式语义来不及统一。
- `jvm/wasm/clr` 的公共语义会被迫在后端重复实现。

`MIR` 的作用就是在这之间插一层“共享收束层”：

- 用统一的 `IKun` 节点表达控制流、调用、模式、常量、序列等。
- 用 `EGraph` 承载等价程序形态。
- 用提取器选出适合继续 lowering 的结果。

## 本目录负责什么

- 将 `HIR` 降为 `EGraph<IKun>`。
- 表达共享控制语义，例如 `Choice`、`Repeat`、`Match`、`Call`、`Seq`。
- 承载共享优化与提取流程。
- 保存函数签名、导出信息、witness 分派绑定等后续 lowering 需要的模块级元数据。

## 本目录不负责什么

- 不保留源码级糖衣。
- 不直接发射目标平台指令。
- 不负责对象最终布局、二进制编码或打包。
- 不在这里发明运行时专属协议来补语言缺口。

## 与相邻层的边界

### 与 `HIR` 的边界

- `HIR` 关心类型、trait、imply、方法、继承等语言级关系。
- `MIR` 开始关心“这些语义如何在共享 IR 中表达”。

### 与 `LIR` 的边界

- `MIR` 追求共享优化与语义统一。
- `LIR` 追求后端可消费的一阶低层表示。
- `MIR` 不应该提前做太多目标平台承诺。

### 文本类型约束

- `MIR` 只能接收已经在 `HIR` 确定好的文本类型。
- `MIR` 中允许出现的正式文本类型仅限确定编码或确定语义的类型，例如 `utf8`、`utf16`、`utf32`、`c_str`。
- 宽泛 `string` 不属于 `MIR` 的正式类型系统，也不能在这一层被归一化成某个默认编码。
- 如果 `HIR` 仍然把 legacy `string` 带到 `MIR`，应直接报错，禁止继续 lowering。

## 为什么 MIR 不是 Full CPS

`Valkyrie` 虽然有 `effect/handler/resume` 的长期方向，但 `MIR` 当前不应默认变成 full CPS，原因是：

- `kont` 只对方法执行过程有意义，不是所有值的底层形态。
- `row` 是公开方法能力，不是 continuation 结构。
- `jvm/wasm/clr` 最终都更适合消费显式控制流和一阶表示。

因此这里的正确路线是：

- 把需要共享的控制语义显式化。
- 在必要时对局部 region 做 continuation 化或状态机化。
- 但不把整个 `MIR` 设计成 continuation 污染的常驻表示。

## 方法 Row 视角下的 MIR

由于 `Valkyrie` 的结构语义是“公开方法 row”，`MIR` 的核心也应围绕调用和分派，而不是围绕字段。

这意味着：

- 结构能力在 `MIR` 中主要体现在调用点、分派方式和 witness 绑定上。
- [`MirWitnessDispatchBinding`](/projects/Valkyrie/Compiler/Mir/MirWitnessDispatchBinding.cs) 是方法能力分派的元数据，不是字段布局描述。
- 如果某个语法点想引入新运行时协议，必须先证明它能在 `MIR` 层形成三后端共享语义；否则宁可暂缓，也不要硬接半套实现。

## 当前代码落点

当前关键入口：

- [`MirBuilder`](/projects/Valkyrie/Compiler/Mir/MirBuilder.cs) 负责从 `HIR` 构建 `MIR` 并运行优化管线。
- [`HirToMirLowerer`](/projects/Valkyrie/Compiler/Mir/HirToMirLowerer.cs) 负责主要 lowering。
- [`MirModule`](/projects/Valkyrie/Compiler/Mir/MirModule.cs) 负责模块级图、提取结果、签名和 witness 元数据。

## 目录说明

```text
Mir/
├── readme.md                  # 本规范
├── MirBuilder.cs              # MIR 构建入口
├── HirToMirLowerer.cs         # HIR -> EGraph<IKun>
├── MirModule.cs               # 模块根结构与模块级元数据
├── MirTree.cs                 # 提取后的 MIR 树
├── MirWitnessDispatchBinding.cs # witness 分派绑定
├── MirOptimizationPipeline.cs # 优化管线
├── SaturationEngine.cs        # 饱和引擎
├── Extractor.cs               # 最优程序提取
├── DefaultCostModel.cs        # 成本模型
├── ICostModel.cs              # 成本模型接口
├── IRewriteRule.cs            # 重写规则接口
└── OptimizationStats.cs       # 优化统计
```

## 对后续语法特性推进的要求

新增 `Valkyrie` 语法点进入三后端时，优先满足这几个条件：

1. 在 `MIR` 层能找到共享语义，而不是后端特例。
2. 不引入新的半套 runtime 协议。
3. 能落入现有 `IKun` 节点或非常自然的扩展节点。
4. 若涉及 `effect/kont`，优先做受限、局部、可去函数化的表示。

这也是为什么像 `if/while/loop/is/!` 这类特性适合优先推进，而 `loop in` 这种需要新 iterable/index/length 协议的能力应更谨慎。

## 禁止事项

- 禁止把 `MIR` 当成某个单一后端的前端缓存。
- 禁止在 `MIR` 里偷偷塞目标平台专属语义。
- 禁止为了赶进度在这里硬加半套 runtime 协议。
- 禁止把所有控制语义一股脑改写成 full CPS。

## 一句话定位

`MIR` 的职责是：把 `Valkyrie` 的高层语义收束成可共享优化、可稳定抽取、对 `jvm/wasm/clr` 同时友好的中层语义，而不是提前坍缩成某个后端的特殊形态。
