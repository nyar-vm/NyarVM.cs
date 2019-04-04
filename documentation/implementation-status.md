# 实现状态

## 概述

本文档追踪 Nyar OA 架构各模块的实现进度。

## 方言接口定义

| 方言 | 接口定义 | EGraph Builder | PE Builder | PSI Builder | 状态 |
|:---|:---|:---|:---|:---|:---|
| Core | `ICoreAlg<E>` | ✅ 手写 | ✅ 手写 | ⬜ 待实现 | v0.1.0 |
| Standard | `IStandardAlg<E>` | ⚠️ 部分 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Game | `IGameAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Tensor | `ITensorAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Data | `IDataAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Web | `IWebAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Shader | `IShaderAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Regex | `IRegexAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |
| Hardware | `IHardwareAlg<E>` | ⬜ 待实现 | ⬜ 待实现 | ⬜ 待实现 | 设计中 |

## 核心系统

### EGraph 引擎

| 组件 | 文件 | 状态 | 说明 |
|:---|:---|:---|:---|
| EClass / ENode | `Nyar.Core/IR/EGraph/` | ✅ 已完成 | 等价类和节点数据结构 |
| EGraph Builder | `Nyar.Core/IR/EGraph/` | ✅ 已完成 | `CoreEgraphBuilder : ICoreAlg<EClassId>` |
| UnionFind | `Nyar.Core/IR/UnionFind/` | ✅ 已完成 | 路径压缩 + 按秩合并 |
| 饱和引擎 | `Nyar.Core/IR/Rewrite/` | ✅ 已完成 | 迭代饱和，规则匹配 |
| 提取器 | `Nyar.Core/IR/Extractor/` | ✅ 已完成 | 成本模型驱动的最优提取 |
| 成本模型 | `Nyar.Core/IR/Extractor/` | ✅ 已完成 | `CostVector` + `ICostModel<E>` |

### 部分求值

| 组件 | 文件 | 状态 | 说明 |
|:---|:---|:---|:---|
| PE Builder | `Nyar.PartialEvaluate/` | ⚠️ 设计中 | `PEBuilder : ICoreAlg<(bool, object, object?)>` |
| 绑定时间分析 | — | ⬜ 待实现 | 静态 / 动态标注 |
| Futamura 投影 | — | ⬜ 远期 | 从解释器工厂自动生成编译器 |

### 代码生成

| 后端 | 文件 | 状态 | 说明 |
|:---|:---|:---|:---|
| NyarVM 后端 | `Nyar.Core/Assembler/NyarVM/` | ✅ 已完成 | 使用 Acorn.Nyar 编码 |
| JVM 后端 | `Nyar.Core/Assembler/JVM/` | ✅ 已完成 | 使用 Acorn.Jvm 编码 |
| WASM 后端 | `Nyar.Core/Assembler/WASM/` | ✅ 已完成 | 使用 Acorn.Wasm 编码 |
| SPIR-V 后端 | `Nyar.Dialect.Shader.Output/` | ✅ 已完成 | 使用 Acorn.SpirV 编码 |
| CLR 后端 | — | ⬜ 远期 | .NET 程序集输出 |
| Native 后端 | — | ⬜ 远期 | x86/ARM/RISC-V |

### NyarVM 运行时

| 组件 | 文件 | 状态 | 说明 |
|:---|:---|:---|:---|
| 解释器核心 | `Nyar.Core/VM/Executor.cs` | ✅ 已完成 | 基于操作码的解释执行 |
| 调用帧 | `Nyar.Core/VM/Frame.cs` | ✅ 已完成 | 值栈 + 调用栈 |
| GC | `Nyar.Core/VM/GC/` | ✅ 已完成 | 标记-清除 |
| 代数效应 | — | ⬜ 设计中 | 统一控制流抽象 |
| Witness Table | — | ⬜ 设计中 | 动态派发 + 热更新 |
| JIT 编译器 | — | ⬜ 远期 | 分层编译 |

## Source Generator

| 产物 | 状态 | 说明 |
|:---|:---|:---|
| `[Dialect]` → 泛型工厂接口 | ⬜ 待实现 | 分析接口定义，生成 `IArithAlg<E>` 等 |
| `[Op]` → AST 节点 | ⬜ 待实现 | 为每个 Op 方法生成 AST 节点类 |
| `[Language]` → 组合工厂 | ⬜ 待实现 | 多方言接口联合实现 |
| EGraph Builder 生成 | ⬜ 待实现 | 从方言接口自动生成 Builder |
| PE Builder 生成 | ⬜ 待实现 | 从方言接口自动生成 Builder |
| PSI Builder 生成 | ⬜ 待实现 | 从方言接口自动生成 IDE PSI |
| 重写规则宏 | ⬜ 待实现 | `[Rule]` 特性的模式匹配代码生成 |
| 封闭世界优化 | ⬜ 待实现 | 去虚拟化、工厂内联、规则预编译 |

## 已知问题

### 架构层面

| 问题 | 优先级 | 说明 |
|:---|:---|:---|
| OA 接口方法过多 | P1 | Core 方言 15 个原子 + Standard 扩展可能导致单个接口 > 50 方法 |
| 工厂实现的样板代码 | P1 | 当前手写 Builder 有大量重复结构，需 Source Generator 解决 |
| EGraph Builder 内存占用 | P2 | 大量等价类节点可能导致 GC 压力 |
| HOAS 的序列化 | P2 | `Func<E, E>` 闭包无法直接序列化为二进制 |

### 性能层面

| 问题 | 优先级 | 说明 |
|:---|:---|:---|
| 虚方法派发开销 | P1 | OA 工厂的接口调用链在热路径上开销显著——等封闭世界优化解决 |
| EGraph 饱和收敛速度 | P2 | 大规模等价规则可能导致收敛缓慢 |
| PE 绑定时间分析精度 | P3 | 不够精确导致残余代码膨胀 |

## 近期计划

1. **完成 Standard 方言定义**：在 `ICoreAlg<E>` 基础上扩展 I/O 和字符串运算
2. **验证 EGraph + PE 协同管线**：同一程序函数依次传入两种工厂
3. **启动 Source Generator 设计**：确定 `[Dialect]` / `[Op]` / `[Language]` 的完整特性定义
4. **补充跨方言优化规则**：Regex → Data 联合优化的原型验证