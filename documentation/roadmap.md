# Nyar 版本路线图

## 概述

Nyar 采用渐进式开发策略，从 OA 核心基础设施开始，逐步扩展方言生态和后端支持。每个版本都有明确的可交付成果和验收标准。

---

## 版本总览

```
v0.1.0 ████████░░░░░░░░░░░░ OA 基础设施（方言 OA 接口 + EGraph + PE Builder）
v0.2.0 ██████████████░░░░░░ 核心方言 + 优化管线（Core/Standard + 饱和优化引擎）
v0.3.0 ██████████████████░░ Source Generator 自动化（PSI + Builder 自动生成）
v0.4.0 ████████████████████ 多后端（NyarVM / JVM / WASM / Native 后端工厂）
v0.5.0 ████████████████████ 扩展方言（Game / Tensor / Data / Web / Shader）
v1.0.0 ████████████████████ 生产就绪（稳定 API + 封闭世界优化 + 性能达标）
```

---

## v0.1.0 —— OA 基础设施

**目标**: 建立 OA 核心框架，验证对象代数范式的可行性

### Nyar.Core.OA

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| ICoreAlg\<E\> | Core 方言 OA 接口定义（15 个语义原子） | 接口编译通过，完整覆盖基础运算 |
| 方言组合机制 | 接口继承 + 工厂组合的基础设施 | `IStandardAlg<E>` 可正确继承 `ICoreAlg<E>` |
| IKunBuilder | IKun 判别联合构造器 | 可构造完整的 IKun 表达式树 |
| 等价意图表达式 | OA 程序函数模式的建立 | `static E P<Alg>(Alg alg) => ...` 模式可用 |

### Nyar.Core.EGraph

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| EGraph 数据结构 | EClass / ENode / UnionFind | 添加和合并等价类正常 |
| EGraph Builder | `CoreEgraphBuilder : ICoreAlg<EClassId>` | 程序函数可传入 EGraph Builder |
| 饱和引擎 | 重写规则匹配 + 迭代饱和 | 对常量折叠规则可饱和 |
| 提取器 | 从等价类中选择最优表示 | 可提取单一最优表达式 |

### Nyar.Core.PE

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| PE Builder | `CorePEBuilder : ICoreAlg<(bool, object, object?)>` | 静态部分正确识别和求值 |
| 绑定时间分析 | 静态 / 动态标注 | 可区分编译期常量和运行时变量 |
| 残余代码生成 | 动态部分生成残余 OA 程序 | 残余代码仍可传给其他工厂 |

### 里程碑验收

- [ ] `ICoreAlg<E>` 接口定义完成并编译通过
- [ ] EGraph Builder 可接收简单算术程序，执行常量折叠
- [ ] PE Builder 可特化静态参数，生成残余程序
- [ ] 同一段程序函数可依次传入 EGraph Builder 和 PE Builder

---

## v0.2.0 —— 核心方言与优化管线

**目标**: 完成 Core / Standard 方言定义，实现完整的优化管线

### 方言体系

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| Core 方言完善 | 15 个语义原子全部定义到接口 | 类型安全，无运行时 fail |
| Standard 方言 | `IStandardAlg<E> : ICoreAlg<E>` 扩展 I/O、字符串 | 可表达完整的通用计算程序 |
| 方言降级规则 | Standard → Core 的降级路径定义 | 所有 Standard 构造可表达为 Core 组合 |
| 方言文档 | 每个方言的接口规范 + 语义说明 | 开发文档完整 |

### 优化规则引擎

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| 代数规则 | 交换律、结合律、分配律的 OA 形式 | 规则库可自动应用 |
| 融合规则 | Map-Map、Filter-Map、Map-Reduce 融合 | 端到端融合测试通过 |
| 化简规则 | 常量折叠、零元素消除、恒等元素消除 | 所有基础化简正确 |
| 规则组合 | 不同来源的规则合并执行 | 用户规则和系统规则可共存 |

### 成本模型

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| CostVector | 多维成本向量（时间、空间、能量、带宽） | 四维向量运算正确 |
| ICostModel\<E\> | OA 成本模型接口 | 接口定义完整，可扩展 |
| 延迟优先模型 | 默认成本模型 | 常见模式评估合理 |

### 里程碑验收

- [ ] Core + Standard 方言定义完成
- [ ] 饱和优化引擎端到端测试通过
- [ ] Map-Map 融合优化验证
- [ ] 成本模型可从等价类中正确选择最优表示

---

## v0.3.0 —— Source Generator 自动化

**目标**: 通过 Source Generator 自动生成 OA 工厂骨架，实现 "一次定义，处处生成"

### Source Generator

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| [Dialect] 特性 | 分析方言接口生成泛型工厂接口 | `IArithAlg<E>` 自动生成正确 |
| [Op] 特性 | 分析 Op 方法生成 AST 节点 | 节点类型完整，类型约束正确 |
| [Language] 特性 | 多方言组合接口自动合并 | 组合工厂实现编译通过 |
| PSI Builder 生成 | 从方言接口自动生成 IDE PSI 构造器 | Rider 中可用 PSI 功能 |
| EGraph Builder 生成 | 自动生成 `DialectEgraphBuilder` | 替代手写 Builder |
| PE Builder 生成 | 自动生成 `DialectPEBuilder` | 替代手写 Builder |
| 重写规则宏 | 从 `[Rule]` 特性生成匹配代码 | 减少手写模式匹配 |

### 方言组合器

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| 组合工厂生成 | 多方言接口联合实现自动生成 | 组合语言应用测试通过 |
| 命名冲突检测 | 不同方言的同名 Op 检测 | 编译期报错，可手动消歧 |

### 里程碑验收

- [ ] Source Generator 可从单一 `[Dialect]` 接口自动生成 EGraph Builder
- [ ] Source Generator 可从单一 `[Dialect]` 接口自动生成 PE Builder
- [ ] Source Generator 可从单一 `[Dialect]` 接口自动生成 PSI Builder
- [ ] `[Language]` 组合方言的联合工厂自动生成正确

---

## v0.4.0 —— 多后端支持

**目标**: 实现多目标代码生成后端，验证 OA 后端可扩展性

### 代码生成后端

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| NyarVM 后端 | `NyarVMBuilder : IStandardAlg<CompilationUnit>` | 可生成 .nyar 模块并执行 |
| JVM 后端 | `JvmBuilder : ICoreAlg<JvmClassFileData>` | 可生成 .class 文件 |
| WASM 后端 | `WasmBuilder : ICoreAlg<WasmModuleData>` | 可生成 .wasm 文件 |
| Native 后端 | `NativeBuilder : ICoreAlg<NativeModuleData>` | 可生成本地可执行文件 |

### AOT 编译管线

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| 后端选择器 | 根据目标平台自动选择后端工厂 | 可命令行指定目标 |
| AOT 工作流 | 完整的源语言 → 目标平台编译流程 | 端到端编译 + 运行 |
| 调试信息 | 生成 DWARF / PDB 调试符号 | 可断点调试 |

### 里程碑验收

- [ ] NyarVM 可执行完整的 Standard 方言程序
- [ ] JVM 后端可生成可执行 .class 文件
- [ ] WASM 后端可生成可在浏览器中运行的 .wasm
- [ ] 同一段 OA 程序可编译到至少 3 个不同目标

---

## v0.5.0 —— 扩展方言

**目标**: 完成核心方言之外的领域方言定义

### 方言实现

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| Game 方言 | `IGameAlg<E> : IStandardAlg<E>` + 物理、渲染 | 可表达游戏逻辑 |
| Tensor 方言 | 张量运算、矩阵操作 OA 接口 | 可表达 ML 模型 |
| Data 方言 | 关系代数、集合操作 OA 接口 | 可表达 SQL 查询 |
| Web 方言 | HTTP、HTML、CSS OA 接口 | 可表达 Web 服务 |
| Shader 方言 | 着色器运算 OA 接口 | 可生成 SPIR-V |
| Hardware 方言 | 硬件描述 OA 接口 | 可生成 Verilog / 高级综合 |
| Regex 方言 | 正则表达式 OA 接口 | 可编译为 NFA / DFA |

### 跨方言优化

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| Regex → Data | 正则转为 SQL 索引扫描 | 端到端性能提升验证 |
| Tensor → Shader | 计算推入图形管线 | 融合测试通过 |
| Data → Tensor | ML 模型下推到数据库 | UDF 推理测试通过 |

### 里程碑验收

- [ ] 至少 5 个方言定义完整并通过 Source Generator 生成 Builder
- [ ] 至少 2 个跨方言联合优化案例可运行
- [ ] Game 方言可在 GnosisVM 上运行

---

## v1.0.0 —— 生产就绪

**目标**: 稳定 API，封闭世界优化，性能对标生产级编译器

### API 稳定化

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| 方言接口冻结 | ICoreAlg / IStandardAlg 等核心接口 | 文档完成，兼容性承诺 |
| Source Generator API | [Dialect] / [Op] / [Language] 特性 | 行为规范文档完整 |
| 迁移指南 | 从旧 OpCode 架构迁移到 OA 架构 | 指南可用，示例覆盖主要场景 |

### 封闭世界优化

| 任务 | 描述 | 验收标准 |
|:---|:---|:---|
| 去虚拟化 | 热路径 switch / visitor 替代虚方法调用 | 性能提升 30%+ |
| 工厂内联 | 已知工厂实现在编译期展开 | 无接口调用开销 |
| 规则预编译 | 规则引擎编译期生成状态机 | 规则匹配零开销 |

### 性能基准

| 指标 | 目标 | 验收标准 |
|:---|:---|:---|
| EGraph 饱和速度 | 500K 次 / 秒 | 基准测试通过 |
| 编译速度 | 中型项目 < 30s | 真实项目编译通过 |
| OA 接口调用开销 | 与 hand-written visitor 持平 | 基准对比测试 |
| 生成代码质量 | 与手写后端可比 | 输出代码性能测试 |

### 里程碑验收

- [ ] 所有公共 API 文档完整
- [ ] 封闭世界优化在基准测试中性能提升 > 30%
- [ ] 至少 3 个真实项目可编译运行
- [ ] NuGet 包可发布

---

## 版本依赖关系

```
v0.1.0 OA 基础设施
    │
    ├──▶ v0.2.0 核心方言 + 优化管线
    │       │
    │       ├──▶ v0.3.0 Source Generator
    │       │       │
    │       │       └──▶ v0.4.0 多后端
    │       │               │
    │       │               └──▶ v0.5.0 扩展方言
    │       │                       │
    │       │                       └──▶ v1.0.0 生产就绪
    │       │
    │       └──▶ (并行) 规则库持续扩充
    │
    └──▶ (并行) 方言设计文档持续完善
```

## 各阶段重点

| 阶段 | 核心挑战 | 验证方式 |
|:---|:---|:---|
| v0.1.0 | OA 范式是否真的能解决 Expression Problem | 用同一段程序函数跑 EGraph / PE / 求值三个工厂 |
| v0.2.0 | 规则引擎是否足够表达常见优化 | 用 Map-Map 融合和常量折叠验证 |
| v0.3.0 | Source Generator 生成量是否覆盖手写量 | 统计手写 vs 自动生成的代码行数比例 |
| v0.4.0 | OA 后端工厂能否生成生产可用的代码 | 用真实 benchmark 验证输出质量 |
| v0.5.0 | 跨方言优化是否有实际价值 | 用正则→SQL 联合优化验证性能提升 |
| v1.0.0 | OA 架构能否达到生产级性能 | 与 hand-written 编译器做性能对比