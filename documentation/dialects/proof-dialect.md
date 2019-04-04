# Proof 方言

## 概述

Proof 方言利用 Nyar 的 EGraph 天然适合表达等式推理的特性，将证明检查与构造意图化，支持与程序优化协同。

## 节点定义

### HIR 层

| 节点 | 描述 | 示例 |
|------|------|------|
| Theorem | 声明待证明命题 | `(Theorem name prop)` |
| Proof | 证明项 | `(Proof tactic goal)` |
| Tactic | 证明策略 | `(Tactic Rewrite eq)` |

### MIR 层

| 节点 | 描述 | 示例 |
|------|------|------|
| RewriteRule | 有向等价重写规则 | `(RewriteRule pattern target)` |
| Induction | 归纳法证明步骤 | `(Induction var base step)` |

### LIR 层

| 节点 | 描述 |
|------|------|
| Check | 将证明发送给外部求解器 |
| Extract | 从证明中提取可执行代码 |

## 等价规则与协同

### 优化与证明同构

优化器应用的每条重写规则都可以产生一个 Proof 节点（记录重写序列）。这使得优化后的程序可以附带正确性证书。

### 规则终止性检查

证明方言的规则可用于分析重写系统的合流性与终止性，从而提前拒绝导致非终止的规则组合。

## 降级路径

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| Coq/Lean | 交互式证明脚本 | 形式化验证 |
| SMT-LIB | 自动化求解 | Z3 等求解器 |
| OCaml | 可执行代码 | 从构造性证明提取算法 |

## 逻辑编程支持

Nyar 通过代数效应将逻辑编程的核心机制（逻辑变量、合一、回溯）纳入意图图，而非引入独立的 Logic 方言。

### 核心效应

| 效应 | 操作 | 语义 |
|------|------|------|
| ChoicePoint | Create | 在搜索树中创建一个选择点，记录当前执行状态 |
| ChoicePoint | Backtrack | 回退到最近的选择点，尝试下一条子句 |
| Unify | Unify | 合一两个项，失败时触发 Backtrack |
| Unify | FreshVar | 引入新的逻辑变量 |

### CPS 变换方案

Prolog 程序可转换为 Continuation-Passing Style（CPS）意图图。每个谓词调用接受两个续延：

- **成功续延**（Success Cont）：当前谓词成功时继续执行
- **失败续延**（Fail Cont）：当前谓词失败时触发回溯

```
// Prolog: append([], L, L).
// 转换为 Nyar 意图图：
(Lambda (x y z success fail)
  (Branch (Unify x EmptyList)
    (Call success (Unify y z))
    (Call fail)))
```

变换后的代码属于 MIR 层，包含大量 Branch 和 Call，可享受常规优化（尾调用消除、部分求值）。

### VM 原语模式

对于性能要求极高的逻辑引擎，Nyar VM 提供原生回溯栈：

- `ChoicePoint` 效应处理器在 VM 栈上压入选择帧
- `Backtrack` 效应处理器恢复寄存器和栈指针
- 与 CPS 模式相比，避免了闭包分配开销

### 与 Proof 方言的协同

逻辑程序的正确性证明可通过 Proof 方言表达：

- 每个 Horn 子句对应一个 `Theorem` 节点
- `Unify` 操作生成等式证明项
- `Resolution` 步骤由 `Trans` 和 `Subst` 组合子构造

## 依赖类型支持

TypeAnnotation 可携带一个 ProofTerm，证明两个类型表达式等价（例如 `Vector (m+n)` 与 `Vector (n+m)` 的相等性）。EGraph 合并 e-class 时会生成相应证明。

DepType 方言定义证明组合子：

| 节点 | 说明 |
|------|------|
| Refl | 自反性 |
| Sym | 对称性 |
| Trans | 传递性 |
| Cong | 同余性 |
| Subst | 替换 |

当其他方言（如 Core 的 Add 交换律）被应用时，DepType 方言的规则会自动生成 Sym 证明项，确保类型正确性。
