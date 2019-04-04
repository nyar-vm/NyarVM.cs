# Nyar.Julia — Julia 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Julia 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 多重派发 → Witness Table + JIT 去虚拟化

Julia 的核心语义是 **多重动态派发**（multiple dynamic dispatch）。Nyar 的 Witness Table + JIT 去虚拟化本质上也是派发优化，但
Julia 的 `MethodInstance` 特化与 Nyar 的"部分求值 + 固化"几乎同构。

**测试重点：**

- Julia 的 `Method` 选择算法（基于类型签名的最特化匹配）能否表示为 EGraph 重写规则？
- `MethodInstance` 的 JIT 特化缓存与 Nyar 的"固化机制"（将常用特化版本序列化为新规则）的对比
- Julia 的 `invoke` / `invoke_latest` 与 Nyar Witness Table 的版本控制（热更新）兼容性

### 2. 科学计算生态 → Tensor 方言

Julia 的 `Flux.jl`、`DifferentialEquations.jl`、`SciML` 是科学计算领域的标杆生态。Tensor 方言需要对接大量实际科学计算代码，Julia
是理想的测试案例。

**测试重点：**

- Julia `Array` 的抽象与 Nyar Tensor 方言 `Placeholder/TensorConst` 的融合
- 自动微分（Zygote.jl / Enzyme.jl）与 Nyar `Grad` 节点的语义对齐
- GPU 内核生成（CUDA.jl / KernelAbstractions.jl）与 Nyar Tensor 方言降级到 CUDA/Metal/WebGPU 后端的对比

### 3. 元编程与编译期计算

Julia 的宏系统和 `generated functions` 是"元编译器"概念的另一实现路径。对比 Julia 的 JIT 编译策略与 Nyar 的 EGraph
饱和策略，可以验证 Nyar 成本模型的有效性。

**测试重点：**

- Julia `@generated` 函数的编译期计算与 Nyar `PartialEvaluate` 的特化边界对比
- 宏展开（macro expansion）与 Nyar 意图图构建阶段的元编程能力
- Julia 的 `CodeInfo` IR 与 Nyar Core 方言的表达能力对比

## 对标 Nyar 需求

| Julia 特性   | Nyar 对应缺口           | 优先级 |
|:-------------|:------------------------|:-------|
| 多重派发     | Witness Table 优化验证  | P0     |
| JIT 特化     | 部分求值 + 固化机制验证 | P0     |
| 科学计算生态 | Tensor 方言实际测试案例 | P1     |
| 自动微分     | `Grad` 节点语义对齐     | P1     |
| `@generated` | 编译期计算边界定义      | P2     |
| 分布式计算   | `Fork` 效应扩展         | P2     |

## 参考资源

- [Julia Documentation: Metaprogramming](https://docs.julialang.org/en/v1/manual/metaprogramming/)
- [Julia Documentation: Methods](https://docs.julialang.org/en/v1/manual/methods/)
- [SciML: Open Source Software for Scientific Machine Learning](https://sciml.ai/)
- [Zygote.jl: Automatic Differentiation in Julia](https://github.com/FluxML/Zygote.jl)
