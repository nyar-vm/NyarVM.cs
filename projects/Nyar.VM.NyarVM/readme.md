# Nyar.VM.NyarVM

## 概述

`Nyar.VM.NyarVM` 当前是 Nyar 体系中 **Valkyrie 官方标准语言** 的运行时。它也是未来可能承接更多统一 lowering 产物的**主性能正式平台**，但那是规划，不是当前事实。

这意味着：

- 当前 `NyarVM` 服务的是 Valkyrie
- 当前除 Valkyrie 外的语言主要由 `LegacyVM` 承载
- 当前不应把“所有语言统一进入 `NyarVM`”写成项目组的既定任务

## 当前状态

### 当前事实

- `NyarVM` 当前只承载 Valkyrie
- `NyarVM` 当前不是其他语言的通用执行入口
- `NyarVM` 当前的首要职责，是把 Valkyrie 这门官方标准语言的运行时能力做扎实
- `NyarVM` 当前即便服务的是 Valkyrie，也不直接感知“Valkyrie 语言”这一前端身份

### 语言无感知原则

`NyarVM` 只感知：

- lowering 后的模块
- bytecode 或等价统一产物
- 运行时对象、调用约定、effect、ABI

`NyarVM` 不感知：

- 前端语言名称
- 前端语法形态
- 作用域、闭包、对象模型在源语言层面的表述方式

这条原则对当前标准语言 Valkyrie 同样成立。也就是说，`NyarVM` 跑的是“Valkyrie lowering 后的统一产物”，而不是“直接理解 Valkyrie 语法的专用 VM”。

### 当前投资重点

当前最值得投入在 `NyarVM` 的内容是：

- bytecode 与执行器
- GC
- JIT
- OSR / deopt
- profiling / observability
- debugger
- FFI
- effect runtime
- hot reload

这些都是 Valkyrie 标准路线和未来主性能路线共用的底座。

## 未来规划

### 未来角色

未来如果项目组决定让更多高价值语言进入统一主性能路线，那么 `NyarVM` 的目标应是：

- 执行统一 lowering 后的 `.nyar` / bytecode / Standard 产物
- 成为统一 runtime、GC、JIT、OSR、FFI、debug 的承载层
- 成为多语言系统里唯一值得长期做主性能投入的执行器
- 继续保持“只认统一产物，不认前端语言身份”的执行模型

### 未来前提

但这些前提必须先满足：

- 对应语言有明确收益
- 前端和 lowering 已经足够稳定
- `Nyar` 层的 IR、ABI、优化桥接已经准备好
- 项目组确实愿意为该语言投入主性能路线成本

如果这些条件不满足，语言继续停留在 `LegacyVM` 是合理且正确的。

## 与 LegacyVM 的关系

### 当前关系

当前真实分工是：

- `LegacyVM`：除 Valkyrie 外的主要多语言执行与编译平台
- `NyarVM`：Valkyrie 官方标准语言 VM

### 长期关系

长期看，两者是互补关系：

- `LegacyVM` 承担参考解释、万能编译器、legacy 生产执行
- `NyarVM` 承担标准语言和未来高价值语言的主性能路线

一句话：

- `LegacyVM` 负责多语言现实落地
- `NyarVM` 负责标准语言和未来主性能路线

## 核心原则

### 当前不做无谓工作

当前不应要求项目组：

- 为所有语言立即编写 `NyarVM` lowering
- 为所有语言立即设计统一 runtime 适配
- 为尚无收益的语言提前投入 `NyarVM` 迁移
- 在 `NyarVM` 中为具体前端语言增加专用执行分支

### 当前该做的事

当前应优先做：

- 把 Valkyrie 在 `NyarVM` 上跑稳
- 把 `NyarVM` 的核心 runtime 能力做深
- 让 `LegacyVM` 继续稳住多语言现实需求
- 只为少数高价值语言保留未来迁移选项
- 保持 `NyarVM` 对前端语言无感知，只围绕统一产物与 ABI 演进

## 对 Nyar.Language 和 Nyar 的依赖

`NyarVM` 本身不负责解决所有语言差异，它依赖：

### `Nyar.Language`

负责：

- parser
- 名字解析
- scope / closure 分析
- object model lowering
- 语言特定 desugar

### `Nyar`

负责：

- dialect
- Object Algebra
- HIR / MIR / LIR
- rewrite / egraph / cost model
- ABI 与 bridge
- 通用优化

因此，`NyarVM` 越要成为统一主性能平台，就越要求 `Nyar.Language` 和 `Nyar` 先把语义问题处理清楚。但这属于未来扩展条件，不是当前所有语言的既定任务。

换句话说，哪怕当前标准语言是 Valkyrie，Valkyrie 的语言特性也应在 `Nyar.Language` / `Nyar` 层被分析和 lowering；`NyarVM` 不应直接理解这些前端特性。

## 当前工作流

推荐通过 `legend` 理解当前分流：

- `Valkyrie -> NyarVM`
- `Other Languages -> LegacyVM`

这里的含义是“Valkyrie 当前通过统一 lowering 产物进入 `NyarVM`”，而不是“`NyarVM` 天然理解 Valkyrie 语法”。这比“默认所有语言都应进入 `NyarVM`”或“`NyarVM` 是 Valkyrie 专用前端 VM”都更符合当前现实。

## 未来迁移准则

只有当一种非 Valkyrie 语言满足以下条件时，才值得考虑迁入 `NyarVM`：

- 用户价值高
- 性能收益明显
- 语义稳定
- lowering 成本可控
- 长期维护收益大于继续停留在 `LegacyVM`

否则，更合理的方案是继续让它留在 `LegacyVM`。

## 一句话总结

`Nyar.VM.NyarVM` 的正确定位是：

- 当前的 Valkyrie 官方标准语言 VM
- 当前对前端语言无感知，只执行统一 lowering 后产物
- 未来可能扩展的统一主性能运行时
- 但不是当前所有语言的统一执行现实

## 相关文档

- [Nyar.VM.LegacyVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.LegacyVM/readme.md)
- [ObjectAlgebra](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar/ObjectAlgebra/readme.md)
- [legacy-full-pipeline spec](file:///e:/RiderProjects/.trae/specs/legacy-full-pipeline/spec.md)
