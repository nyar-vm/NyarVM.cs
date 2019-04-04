# Nyar.VM.LegacyVM

## 概述

`Nyar.VM.LegacyVM` 是 Nyar 体系中的**参考解释器、万能编译器平台与 legacy 语言生产可用执行层**。它的目标不是成为主流语言的最终高性能 VM，而是解决以下几类问题：

- 直接解释源码，快速验证语言行为
- 为语言前端提供对拍基线
- 在 lowering 尚未完成时提供 fallback
- 通过 OA / PE / Futamura 路线孵化编译器
- 在优化预算有限时，为 legacy / 小众语言提供生产可用执行

它适合承担“先跑起来、先验证语义、先生成编译器、先稳定交付尾部语言”的职责，不适合承载主流语言的长期统一对象模型、JIT、GC、OSR 或主性能优化主线。

## 当前状态

### 当前事实

- 当前除 Valkyrie 外，其他语言主要都由 `LegacyVM` 承载
- 当前 `LegacyVM` 不是边缘工具，而是多语言现实落地的主平台
- 当前项目组不应把“尽快让所有语言迁出 `LegacyVM`”当作默认任务

### 当前价值

`LegacyVM` 当前的价值有三层：

- 参考解释器：帮助快速验证语言行为
- 万能编译器平台：通过 OA / PE / Futamura 低成本长出编译路径
- 生产可用执行层：为尾部语言提供稳定部署方案

## 未来规划

### 未来长期角色

即使未来 `NyarVM` 吸纳更多高价值语言，`LegacyVM` 也不会消失。它长期应保留为：

- 长尾语言的执行平台
- 编译器工坊
- 参考语义基线
- fallback 与对拍平台

### 未来不应承担的工作

未来也不应要求 `LegacyVM` 成为：

- 所有语言统一的高性能 VM
- 主性能路线的 GC / JIT / OSR 中心
- 所有新语言都必须优先接入的唯一入口

## 在整体架构中的位置

Nyar 当前应分为五层：

```text
Nyar.Language        -> 多语言 parser / scope / closure / object lowering
Nyar                 -> 统一 dialect / OA / HIR-MIR-LIR / rewrite / optimize
Nyar.VM.LegacyVM     -> 参考解释器 / 万能编译器平台 / legacy 生产执行层
Nyar.VM.NyarVM       -> 正式统一执行器 / bytecode / GC / JIT / OSR / FFI
tools/legend         -> 唯一编排入口
```

其中：

- `LegacyVM` 解释源码并孵化编译器
- `NyarVM` 当前承载 Valkyrie，未来可能承载更多统一产物
- `Nyar` 负责统一语义和优化

## 设计定位

### 1. 参考执行

`LegacyVM` 直接消费源码或轻量转换结果，主要用于：

- 语言 bring-up
- 行为验证
- lowering 前后的结果对拍
- 小众语言的低成本接入

### 2. 过渡执行

如果某个语言尚未完成：

- 完整 parser
- scope / closure 语义分析
- object model lowering
- 到 `Nyar` IR 的 lowering

则可以临时通过 `LegacyVM` 跑通最小可用路径。

### 3. 万能编译器平台

`LegacyVM` 仍然是最适合验证以下思想的地方：

- Object Algebra 的多解释器能力
- Partial Evaluation 的静态/动态分离
- Futamura 投影对解释器生成编译器的可行性

它的意义不只是实验，而是为这些语言提供一条现实路径：

- 先有解释器
- 再从解释器导出编译器、lowering 或专用执行路径
- 最后按语言价值决定是否迁入 `NyarVM` 主线

### 4. legacy 语言生产可用执行

对很多尾部语言来说，团队并不会投入完整 lowering、专用优化和高性能 runtime 集成。在这种情况下，`LegacyVM` 可以直接承担生产职责，只要满足：

- 语义稳定
- 工具链可控
- 性能目标可接受
- 运维成本低于重写一整套主线路径

## 不再承担的职责

从现在开始，以下职责不应继续以 `LegacyVM` 为中心扩张：

- 统一对象模型的最终定义
- 多语言正式运行时 ABI
- 正式 JIT / deopt / OSR / GC 能力
- 生产级 debug / profile / hot reload 基础设施
- 面向主流语言的长期优化中心

换句话说，`LegacyVM` 可以是**参考实现**、**生产可用执行层**、**编译器生成平台**，也是当前多语言现实的主要承载者，但不再是主流语言未来主性能路线的**唯一真相源**。

## 当前结构

### Algebra

`LegacyVM` 保留 OA 风格的基础代数接口和解释器实现，例如：

- `Algebra/Core`
- `Algebra/Standard`
- `Algebra/Shell`

这些模块的意义是：

- 作为参考语义的快速宿主
- 作为 PE / 实验型 codegen 的可操作样例
- 作为编译器生成的母体
- 作为某些简单语言或 DSL 的最小执行面

### Evaluator

`Evaluator` 目录中的各语言 `*TreeEvaluator` 或脚本 evaluator，代表的是**源码级参考执行路径**。它们的价值在于：

- 帮助理解语言现有行为
- 作为 lowering 前的可执行样例
- 支持对拍和调试
- 为长尾语言提供生产可用解释路径
- 为 PE / 编译器生成提供解释器基底

它们不应再被视为“未来所有主流语言都要长期补齐的正式高性能 runtime”。

### Runner

`Runner/LegacyVmRunner.cs` 的职责应聚焦为：

- 注册语言入口
- 选择源码解释路径
- 选择编译器生成或专用执行路径
- 管理 fallback 执行
- 提供面向调试与对拍的统一入口

未来更推荐的用户工作流应由 `legend` 调用它，而不是要求用户直接理解内部所有分支。

## 推荐主路径

未来对高价值语言，推荐主路径如下：

```text
source
  -> Nyar.Language.<Lang>
  -> Nyar HIR / MIR / LIR
  -> optimize
  -> .nyar / bytecode
  -> Nyar.VM.NyarVM
```

但当前现实更常见的路径是：

```text
source -> LegacyVM
```

适用场景：

- 当前除 Valkyrie 外的大多数语言
- `legend run --legacy`
- `legend test-conformance`
- legacy / 小众语言的生产部署
- lowering 调试
- 尚未迁移完成的语言 fallback

## 支持语言的理解方式

这里的“支持语言”不应再理解为“为每种语言补一个长期 evaluator”，而应区分成四类：

### 1. 参考解释支持

通过 `LegacyVM` evaluator 或轻量转换器直接运行源码，例如脚本类语言、实验语言、小众语言。

### 2. 迁移中支持

语言当前可能先通过 `LegacyVM` 跑起来，但长期目标是进入：

- `Nyar.Language.<Lang>`
- `Nyar` 统一 IR
- `NyarVM` 正式执行

### 3. legacy 生产支持

语言可能长期停留在 `LegacyVM`，但仍然用于生产。典型条件是：

- 用户规模有限
- 性能要求温和
- 团队没有资源投入主线 lowering
- 更需要稳定与低维护成本

### 4. 主线正式支持

当一种语言已经具备完整 lowering 路线且迁移收益明确时，其正式执行平台才值得切换到 `NyarVM`。

## OA 与 PE 的保留价值

`LegacyVM` 仍然保留 OA + PE 的独特价值。

### OA

同一程序可以用不同 algebra 实现获得不同结果：

- 直接解释执行
- 格式化输出
- 静态分析
- 部分求值
- reify 到统一 IR

### PE

PE 的价值不是让 `LegacyVM` 自己变成统一最终 VM，而是：

- 证明某类解释器可以自动特化
- 为简单语言或 DSL 生成低成本编译路径
- 为 legacy 语言生成足够实用的编译器
- 为 `Nyar` 主线提供实验性 lowering / specialization 样本

## 扩展建议

### 新语言接入时优先级

对未来要进入主线的高价值语言，推荐顺序：

1. 在 `Nyar.Language.<Lang>` 写 parser 与语义分析
2. 建立到 `Nyar` IR 的 lowering
3. 让产物进入 `NyarVM`
4. 只在必要时补一个 `LegacyVM` 参考 evaluator

对 legacy / 小众语言，也允许另一条路线：

1. 在 `LegacyVM` 先建立稳定解释器
2. 通过 OA / PE 生成编译器或专用执行路径
3. 直接作为生产可用方案交付
4. 只有在收益足够高时再迁入主线

当前不推荐顺序：

1. 把所有语言都先写成越来越复杂的 `LegacyVM` 长期 evaluator
2. 再把所有语义逐步搬去正式主线

### 适合留在 LegacyVM 的内容

- 小众脚本语言的快速解释路径
- 壳语言、命令语言、原型 DSL
- 编译器生成实验与低成本编译路径
- 对拍工具
- 教学和实验型解释器
- 长尾语言的生产可用执行面

### 应迁出的内容

- 长期对象模型
- 统一异常和 effect 语义
- 统一调用约定
- 统一优化基础设施
- 主性能关键路径

## 使用方式建议

推荐通过 `legend` 使用：

- `legend run --legacy foo.py`
- `legend test-conformance foo.ts`
- `legend dump --legacy-trace foo.lua`

不推荐把 `LegacyVM` 写成“所有语言未来都应继续停留的唯一平台”；但在当前现实里，对除 Valkyrie 外的大多数语言，它就是正式生产入口。

## 一句话总结

`Nyar.VM.LegacyVM` 的正确定位是：

- 源码级参考解释器
- 万能编译器平台
- legacy 语言生产可用执行层
- 行为对拍器
- 迁移期 fallback
- OA / PE 实验场

而不是主流语言未来的长期高性能统一 VM。

## 相关文档

- [Nyar.VM.NyarVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.NyarVM/readme.md)
- [ObjectAlgebra](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar/ObjectAlgebra/readme.md)
- [legacy-full-pipeline spec](file:///e:/RiderProjects/.trae/specs/legacy-full-pipeline/spec.md)
