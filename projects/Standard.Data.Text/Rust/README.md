# Nyar.Rust — Rust 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Rust 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 内存安全语义 → Nyar IR 效应系统

Rust 的所有权（ownership）和借用（borrow）系统是工业级内存安全的标杆。Nyar Core 方言目前只有 `Alloc/Free/Load/Store`
四个内存原子，完全没有生命周期、所有权转移、可变/不可用区分的语义。

**测试重点：**

- Rust 的 `&T` / `&mut T` / `Box<T>` 能否映射为 Nyar IR 的 `EffectSet` 扩展？
- 借用检查器的区域推断（region inference）与 Nyar 的 `TypeAnnotation` 携带依赖证明的机制是否同构？
- `drop` 语义与 Nyar 的 `Free` 节点如何对接（确定性析构 vs GC）？

### 2. MIR → Nyar IR 同构验证

`rustc` 的中间表示 MIR（Mid-level IR）与 Nyar 的意图图层级（HIR/MIR/LIR）在结构上高度相似。

**测试重点：**

- 将 `rustc` 的 MIR `BasicBlock` / `Terminator` / `Rvalue` 直接转换为 Nyar Core 方言的可行性
- MIR 的 `BorrowCheck` 结果能否编码为 Nyar 的 `MemoryOrder` 或效应约束？
- `async/await` 状态机与 Nyar 代数效应 `Perform/Handle` 的语义差异与统一路径

### 3. AOT 后端与自举路径

Rust 后端直接输出 LLVM IR，Nyar 的 AOT 工作流明确将 LLVM IR 列为可选后端。Rust 的零成本抽象和编译期计算能力与 Nyar 的"
部分求值 + 固化"哲学高度契合。

**测试重点：**

- Nyar 优化后的意图图通过 Rust 作为桥梁输出 LLVM IR 的端到端路径
- `const fn` / `const eval` 与 Nyar `PartialEvaluate` 引擎的特化策略对比
- 长期：Nyar 核心能否用 Rust 重写并实现自举（第 N 阶目标）

## 对标 Nyar 需求

| Rust 特性     | Nyar 对应缺口                   | 优先级 |
|:--------------|:--------------------------------|:-------|
| 所有权系统    | Core 方言无生命周期语义         | P0     |
| MIR           | HIR/MIR/LIR 层级验证            | P0     |
| LLVM 后端     | AOT 工作流后端扩展              | P1     |
| `async/await` | 代数效应与 Rust Future 语义映射 | P1     |
| `const fn`    | 部分求值引擎验证                | P2     |
| Trait 系统    | Witness Table 与类型类统一      | P2     |

## 参考资源

- [rustc-dev-guide](https://rustc-dev-guide.rust-lang.org/)
- [MIR 文档](https://rustc-dev-guide.rust-lang.org/mir/index.html)
- [RustBelt: Securing the Foundations of the Rust Programming Language](https://plv.mpi-sws.org/rustbelt/)
