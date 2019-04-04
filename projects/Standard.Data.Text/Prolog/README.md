# Nyar.Prolog — Prolog/Datalog 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Prolog/Datalog 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. Unification → EGraph eclass 合并

Prolog 的统一化（unification）是逻辑编程的核心操作。Nyar EGraph 的 `union-find` 机制在算法层面与 unification
高度相似，但缺少理论层面的互通。

**测试重点：**

- Prolog 的 term unification 能否直接复用或加速 Nyar EGraph 的 eclass 合并？
- 逻辑变量（logic variables）与 Nyar 意图图中待填充的" holes "的对应关系
- Prolog 的递归查询与 Nyar 意图图循环结构（如 `Repeat` 节点）的处理对比

### 2. 回溯 → 效应系统扩展

Core 方言文档提到"逻辑变量 → ChoicePoint 效应"，但没有实际实现。Prolog 的深度优先回溯是 ChoicePoint 效应的天然参考实现。

**测试重点：**

- Prolog 的 `choice point` / `trail stack` 与 Nyar 效应处理器的状态保存/恢复机制
- 约束逻辑编程（CLP）与 Nyar `TypeAnnotation` 携带约束的扩展
- 回溯与 Nyar 代数效应 `Resume` 的语义统一

### 3. Datalog → Data 方言前端 + 程序分析

Datalog 是关系代数的超集，Data 方言的 `Scan/Join/Filter` 可以直接从 Datalog 前端生成。形式化验证领域（Proof 方言）也大量使用
Datalog 做程序分析。

**测试重点：**

- Datalog 规则到 Nyar Data 方言 HIR 层的直接转换
- 递归 Datalog（如 Soufflé）的半朴素求值与 Nyar EGraph 饱和优化的协同
- 将 Datalog 作为 Nyar 重写规则系统的元语言（规则即数据）

## 对标 Nyar 需求

| Prolog/Datalog 特性 | Nyar 对应缺口       | 优先级 |
|:--------------------|:--------------------|:-------|
| Unification         | EGraph 合并算法增强 | P1     |
| 回溯 / ChoicePoint  | 效应系统扩展        | P1     |
| Datalog             | Data 方言前端       | P2     |
| 约束逻辑编程        | 类型约束扩展        | P2     |
| 元解释器            | 元编译器自举验证    | P2     |

## 参考资源

- [The Art of Prolog (Sterling & Shapiro)](https://mitpress.mit.edu/9780262193382/the-art-of-prolog/)
- [Soufflé: A Datalog Synthesis Tool](https://souffle-lang.github.io/)
- [Datalog 2.0](https://datalog20.org/)
