# legend

## 概述

`legend` 是 Nyar 工具链的**统一编排入口**。它的职责不是重新实现语言语义、优化器或 VM，而是把以下几层串成一条可用的工作流：

- `Nyar.Language` 的多语言前端
- `Nyar` 的统一 IR、规则与优化
- `Nyar.VM.LegacyVM` 的源码参考执行、万能编译器与 legacy 生产路径
- `Nyar.VM.NyarVM` 的正式统一执行路径

从用户视角看，`legend` 应该是日常使用 Nyar 体系的默认入口。

## 设计目标

### 1. 统一入口

用户不应该手工理解内部到底要调用：

- 哪个语言前端
- 哪条 lowering 链
- 哪个优化阶段
- `LegacyVM` 还是 `NyarVM`

这些都应由 `legend` 根据文件类型、配置和命令参数统一编排。

### 2. 当前按现实分流，未来保留主性能主线

`legend` 当前的默认行为应先尊重仓库现实：

```text
Valkyrie        -> NyarVM
Other Languages -> LegacyVM
```

未来如果有更多高价值语言完成统一 lowering，再逐步扩展 `NyarVM` 主性能主线。

以下场景会显式选择 `LegacyVM`：

- `--legacy`
- `test-conformance`
- lowering 调试
- 某语言尚未完成正式 lowering
- 某语言明确选择 legacy 生产部署

### 3. 统一可观测性

无论最终走哪条执行分支，`legend` 都应输出统一风格的：

- diagnostics
- artifact 路径
- lowering trace
- profile 结果
- runtime 选择信息

## 在整体架构中的位置

```text
legend
  -> choose frontend
  -> choose lowering pipeline
  -> choose optimizer stages
  -> choose runtime
```

具体分工：

- `Nyar.Language` 负责“看懂源语言”
- `Nyar` 负责“统一语义和优化”
- `LegacyVM` 负责“源码参考执行、编译器工坊与 legacy 生产”
- `NyarVM` 负责“正式统一执行与主性能路线”
- `legend` 负责“把这些环节组织成稳定工作流”

## 推荐命令模型

### `run`

运行源码或模块。

默认语义：

- 当前若输入是 Valkyrie，则默认走 `NyarVM`
- 当前若输入不是 Valkyrie，则默认走 `LegacyVM`
- 如果用户显式指定 `--legacy`，则强制走 `LegacyVM`
- 如果未来某语言完成统一 lowering，可再扩展其默认去向

这里的 `--legacy` 不只意味着“调试模式”，也可以意味着：

- 选择 `LegacyVM` 参考执行
- 选择 `LegacyVM` 的 legacy 生产执行面
- 选择 `LegacyVM` 驱动的编译器 / 专用执行路径

示例：

```bash
legend run app.py
legend run main.ts
legend run script.lua --legacy
```

### `build`

执行编译、lowering 和产物输出。

示例：

```bash
legend build main.rs
legend build app.v --emit bytecode
```

### `dump`

查看中间表示或执行产物，帮助调试 lowering 和优化。

建议支持：

- `hir`
- `mir`
- `lir`
- `bytecode`
- `legacy-trace`

示例：

```bash
legend dump hir app.py
legend dump mir main.ts
legend dump bytecode module.nyar
```

### `profile`

当前主要触发 `NyarVM` 路径下的 profiling / trace / hotspot 信息。

示例：

```bash
legend profile app.v
legend profile service.py
```

### `test-conformance`

同时运行：

- `LegacyVM` 的源码参考执行
- lowering 后 `NyarVM` 的正式执行

并对比结果、异常、输出和关键 trace，用于迁移验证。

## 与 LegacyVM 的关系

`legend` 不会隐藏 `LegacyVM` 的存在，但会把它明确为**补充路径**：

- 用于源码参考执行
- 用于行为对拍
- 用于语言 bring-up
- 用于尚未完成 lowering 的临时 fallback
- 用于 tail / legacy 语言的正式部署
- 用于编译器生成与实验性 codegen

这能避免用户把 `LegacyVM` 误解成所有语言统一的主性能平台，同时也避免把它误解成“只能调试”的辅助玩具。

## 与 NyarVM 的关系

`legend` 不应把 `NyarVM` 表达成“理解前端语言的 VM”。它调度到 `NyarVM` 时，针对的是统一 lowering 后产物的正式执行路径。即便当前标准语言是 Valkyrie，这一点也成立。

从长期能力建设看，`NyarVM` 仍然是：

- 正式 bytecode 执行器
- 正式 GC / JIT / OSR 宿主
- 正式 profiling / debug / FFI 平台

但从当前现实看，`legend` 不应误导项目组认为“所有语言都该默认进 `NyarVM`”。更准确的说法是：

- 当前 `Valkyrie -> NyarVM`
- 当前 `Others -> LegacyVM`
- 未来高价值语言再按收益决定是否进入 `NyarVM`

## 当前不做的事

当前不应要求 `legend`：

- 把所有语言默认送入 `NyarVM`
- 假设 `NyarVM` 直接理解前端语言及其特性
- 为尚无收益的语言编排完整 `NyarVM` 迁移链
- 把未来主性能路线写成当前现实

## 不应承担的职责

`legend` 不应：

- 重写语言语义
- 复制一套优化器
- 维护独立 runtime 语义
- 替代 `Nyar.Language`、`Nyar`、`LegacyVM` 或 `NyarVM`

它的核心价值是编排，而不是重新成为一个新的中心系统。

## 一句话总结

`legend` 的正确定位是：

- Nyar 体系唯一的用户入口
- 当前现实路径和未来主性能路径的编排器
- 编译、运行、dump、profile、conformance 的统一门面

## 相关文档

- [Nyar.VM.LegacyVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.LegacyVM/readme.md)
- [Nyar.VM.NyarVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.NyarVM/readme.md)
- [legacy-full-pipeline spec](file:///e:/RiderProjects/.trae/specs/legacy-full-pipeline/spec.md)
