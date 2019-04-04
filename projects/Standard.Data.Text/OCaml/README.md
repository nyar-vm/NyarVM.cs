# Nyar.OCaml — OCaml 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 OCaml 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 模块化与 Functor → Nyar 方言扩展机制

OCaml 的模块系统（签名、结构、Functor）是工业级最强大、最灵活的模块化机制。Nyar 的方言系统目前通过"方言ID + 操作码"
扩展，缺少模块级别的组合与抽象能力。

**测试重点：**

- OCaml `Functor` 的参数化模块能否映射为 Nyar 方言的"参数化方言"？
- 模块签名（`module type`）与 Nyar 方言的接口约束（Interface Constraint）的统一
- 第一 class 模块与 Nyar Witness Table 的动态模块加载对比

### 2. OCaml 5.0 Effect → Nyar 代数效应

OCaml 5.0 引入的原生 `effect` 是工业级代数效应的最新实现，与 Nyar 的 `Perform/Handle` 节点在语义上直接对应。

**测试重点：**

- OCaml `effect E : int -> string` 与 Nyar 效应类型定义的映射
- `continue` / `discontinue` 与 Nyar 效应处理器的恢复语义
- OCaml 的多核调度（domains）与 Nyar VM 的轻量级线程 `Fork` 效应的对比

### 3. Coq 生态 → Proof 方言

Coq 证明助手基于 OCaml 构建，Nyar 的 Proof 方言明确将 Coq 作为后端目标。没有 OCaml 前端，Proof 方言缺少从实际证明工具导入证明脚本的通道。

**测试重点：**

- Coq `Gallina` 语言的 term 与 Nyar Proof 方言 `Theorem/Proof/Tactic` 节点的转换
- `Ltac` / `Ltac2` 策略语言与 Nyar 重写规则 DSL 的对应关系
- 从 Coq 证明中提取可执行代码（`Extraction`）与 Nyar `Extract` 节点的协同

## 对标 Nyar 需求

| OCaml 特性         | Nyar 对应缺口             | 优先级 |
|:-------------------|:--------------------------|:-------|
| 模块系统 / Functor | 方言模块化扩展            | P0     |
| Effect (OCaml 5.0) | 代数效应运行时验证        | P0     |
| Coq 集成           | Proof 方言实际输入源      | P1     |
| GADT               | 依赖类型与类型等价证明    | P1     |
| Objects            | Witness Table 与 OOP 语义 | P2     |
| PPX 元编程         | 元编译器宏系统对比        | P2     |

## 参考资源

- [OCaml 5.0 Effect Handlers](https://v2.ocaml.org/manual/effects.html)
- [Coq Reference Manual](https://coq.inria.fr/doc/)
- [Modular Implicits (OCaml)](https://arxiv.org/abs/1512.01895)
