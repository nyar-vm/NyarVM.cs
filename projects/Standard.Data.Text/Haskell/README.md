# Nyar.Haskell — Haskell 语言前端占位包

## 状态

占位阶段（Placeholder）。等待 Haskell 编译器相关实现填充。

## 对 Nyar 体系的核心价值

### 1. 类型类 → Witness Table 理论统一

Haskell 的 `TypeClass` 是工业级 ad-hoc 多态的鼻祖。Nyar 的 Witness Table 机制在语义上是对 TypeClass 的运行时实现，但缺少理论层面的验证。

**测试重点：**

- Haskell `TypeClass` 的字典传递（dictionary passing）能否直接映射为 Nyar 的 `Witness*` 指针？
- 多参数类型类（`MultiParamTypeClasses`）与 Nyar 接口混合（Interface Mixing）的兼容性
- `GHC` 的 `Core` IR（System FC）与 Nyar Core 方言的表达能力对比

### 2. 代数效应的理论源头

Haskell 的 `mtl`（monad transformer library）和 OCaml 5.0 的 `effect` 是 Nyar 代数效应系统的理论近亲。Nyar
当前效应实现是文档级概念，缺少从成熟函数式语言直接导入效应处理器的路径。

**测试重点：**

- Haskell `MonadError` / `MonadState` / `MonadReader` 的 transformer 堆叠与 Nyar `Handle` 节点的嵌套语义是否等价？
- `freer-simple` / `fused-effects` 的代数效应实现与 Nyar `Perform/Handle` 的性能对比
- 高阶效应（higher-order effects）在 Nyar IR 中的表达能力

### 3. GADT / 依赖类型 → Proof 方言

Haskell 的 `GADT`（广义代数数据类型）和 `TypeFamilies` 是轻量级依赖类型的工业实践。Nyar 的 Proof 方言和 `TypeAnnotation`
携带依赖证明的机制需要与 Haskell 的类型级编程能力对比。

**测试重点：**

- Haskell `GADT` 的 term-level / type-level 统一与 Nyar `DepType` 方言的映射
- `DataKinds` + `TypeFamilies` 与 Nyar EGraph 中类型等价推导的协同
- 从 Haskell 证明脚本到 Nyar Proof 方言的自动转换

## 对标 Nyar 需求

| Haskell 特性                        | Nyar 对应缺口          | 优先级 |
|:------------------------------------|:-----------------------|:-------|
| TypeClass                           | Witness Table 理论验证 | P0     |
| Monad / Algebraic Effects           | 效应系统形式化         | P0     |
| GADT                                | Proof 方言输入源       | P1     |
| Lazy Evaluation                     | EGraph 惰性求值兼容性  | P1     |
| TemplateHaskell                     | 元编译器宏系统对比     | P2     |
| STM (Software Transactional Memory) | 并发效应扩展           | P2     |

## 参考资源

- [GHC Commentary](https://gitlab.haskell.org/ghc/ghc/-/wikis/commentary)
- [System FC](https://www.microsoft.com/en-us/research/publication/system-f-with-type-equality-coercions/)
- [Algebraic Effects and Handlers (Plotkin & Pretnar)](https://www.eff-lang.org/handlers-tutorial.pdf)
