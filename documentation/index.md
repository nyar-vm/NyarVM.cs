# Nyar 文档

欢迎来到 **Nyar** 官方文档。

## 简介

Nyar 是一个基于**对象代数**的元编译器框架。它以单一方言定义为事实来源，统一驱动编译器、IDE、解释器和静态分析器。

> 一次定义，处处优化，永久扩展。四个应用，一套接口。

## 快速导航

| 文档 | 描述 |
|:---|:---|
| [项目介绍](./introduction.md) | 了解 Nyar 的核心哲学：从 Expression Problem 到对象代数 |
| [快速开始](./quick-start.md) | 环境配置与第一个 OA 风格的 Nyar 程序 |
| [入门指南](./getting-started.md) | 构建项目、编写程序、定义方言的完整教程 |
| [API 参考](./api-reference.md) | Nyar OA 架构完整 API（方言、EGraph、后端、NyarVM） |
| [语言前端抽象](./language-frontend-abstractions.md) | 语言前端到 OA 程序函数的转换抽象 |
| [版本路线图](./roadmap.md) | v0.1.0 – v1.0.0 渐进式开发计划 |
| [实现状态](./implementation-status.md) | 各模块实现进度追踪 |

## 核心概念

### 基石

| 概念 | 说明 |
|:---|:---|
| [**对象代数**](./core-systems/object-algebras.md) | Nyar 的核心组织原则：工厂接口统一表示与操作 |
| [**等价意图**](./core-systems/equivalence-intent.md) | 意图 = 多态程序函数，等价 = 可替换的工厂实现 |
| [**PSI 集成**](./core-systems/psi-integration.md) | 从方言 OA 自动生成 IDE 语法树与分析能力 |

### 编译器与优化

| 概念 | 说明 |
|:---|:---|
| [**元编译器**](./core-systems/meta-compiler.md) | OA 驱动的编译管线：EGraph + PE + CostModel |
| [**EGraph 引擎**](./core-systems/egraph.md) | 饱和优化引擎 —— OA 的一种工厂实现 |
| [**部分求值**](./core-systems/partial-eval.md) | 编译期特化 + Futamura 投影 —— OA 的另一种工厂实现 |

### 运行时

| 概念 | 说明 |
|:---|:---|
| [**元虚拟机**](./core-systems/meta-vm.md) | NyarVM：OA 的一个运行时后端（非唯一后端） |
| [**代数效应**](./core-systems/algebraic-effects.md) | NyarVM 的统一控制流抽象 |
| [**Witness Table**](./core-systems/witness-table.md) | NyarVM 的动态派发与热更新机制 |

### 统一优化范式

对象代数使 Nyar 能够将不同领域的优化统一在同一框架下：

| 应用场景 | OA 工厂实现 | 消费方 |
|:---|:---|:---|
| **编译器/优化器** | `EgraphBuilder`, `PEBuilder`, `CostModelBuilder` | 多目标代码生成后端 |
| **IDE/编辑器** | `PsiBuilder`, `SyntaxHighlightBuilder` | IDE 插件（Rider / VS Code） |
| **解释器/VM** | `InterpBuilder`, `JitCompiler` | NyarVM / JVM / WASM / Native |
| **静态分析** | `TypeInferBuilder`, `EffectAnalyzer` | Linter、验证器、安全扫描 |
| **扫描/搜索** | `PatternMatchBuilder`, `EquivSearchBuilder` | 重构工具、代码搜索引擎 |

## 文档结构

```
documentation/
├── index.md                                 # 文档首页
├── introduction.md                          # 项目介绍：从 EP 到 OA
├── quick-start.md                           # 快速开始
├── getting-started.md                       # 入门指南
├── api-reference.md                         # API 参考（OA 风格）
├── implementation-status.md                 # 实现状态追踪
├── language-frontend-abstractions.md        # 语言前端抽象层设计
├── roadmap.md                               # 版本路线图
│
├── core-systems/                            # 核心系统
│   ├── object-algebras.md                   # 对象代数（基石）
│   ├── equivalence-intent.md                # 等价意图：OA 下的意图表达
│   ├── meta-compiler.md                     # 元编译器：OA 编译管线
│   ├── egraph.md                            # EGraph：OA 优化工厂
│   ├── partial-eval.md                      # 部分求值：OA 特化工 + Futamura
│   ├── psi-integration.md                   # PSI 集成：IDE 统一方案
│   ├── meta-vm.md                           # NyarVM：一个 OA 运行时后端
│   ├── algebraic-effects.md                 # 代数效应（NyarVM 运行时特性）
│   ├── witness-table.md                     # Witness Table（NyarVM 运行时特性）
│   ├── binary-architecture.md               # Nyar.Binary 核心接口与基础设施
│   ├── binary-attributes.md                 # Nyar.Binary 声明式特性系统
│   ├── data-model.md                        # ISource、GreenNode、RedNode 数据模型
│   ├── parsing.md                           # CstBuilder 与 ParseContext
│   ├── incremental-reparse.md               # 增量重解析机制
│   ├── typed-ast.md                         # 强类型 AST 手写基座
│   ├── language-abstraction.md              # Language 抽象配置
│   ├── language-injection.md                # 语言注入：多语言交织
│   ├── language-registry.md                 # LanguageRegistry 全局注册
│   ├── synthetic-construction.md            # 程序化合成节点
│   └── utilities.md                         # LineIndex、错误恢复等可选工具
│
├── dialects/                                # 方言体系
│   ├── core-dialect.md                      # Core 方言（OA 接口定义）
│   ├── standard-dialect.md                  # Standard 方言
│   ├── tensor-dialect.md                    # Tensor 方言
│   ├── data-dialect.md                      # Data 方言
│   ├── web-dialect.md                       # Web 方言
│   ├── hardware-dialect.md                  # Hardware 方言
│   ├── regex-dialect.md                     # Regex 方言
│   ├── proof-dialect.md                     # Proof 方言
│   ├── shader-dialect.md                    # Shader 方言
│   ├── game-dialect.md                      # Game 方言
│   ├── schedule-dialect.md                  # Schedule 方言
│   ├── agent-dialect.md                     # Agent 方言
│   └── quantum-dialect.md                   # Quantum 方言
│
├── binary/                                  # Nyar.Binary 二进制框架
│   ├── overview.md                          # 框架概述：设计哲学与快速开始
│   ├── faq.md                               # 常见问题与最佳实践
│   ├── examples.md                          # 典型用例：GLB、MySQL、Live2D、WASM
│   ├── extensibility.md                     # 扩展点：ICodec、IFrameProtocol
│   └── source-generator.md                  # Roslyn 源生成器契约
│
├── intelligence/                            # Nyar.Intelligence 解析基础设施
│   ├── design-philosophy.md                 # 设计哲学（原 Oak 2.0）
│   ├── faq.md                               # 常见问题
│   └── layer.md                             # 分层架构：Oak → Nyar.Syntax → 上层
│
└── technical/                               # 技术细节
    ├── nyar-bytecode-format.md              # Nyar 字节码格式
    ├── aot-workflow.md                      # AOT 编译工作流
    ├── gnosis-vm-backend.md                 # GnosisVM 后端
    └── ai-self-evolution.md                 # AI 与自我进化
```

## 解析基础设施 (Nyar.Intelligence)

Nyar.Intelligence 源自 Oak 2.0，是 Nyar 体系中的纯语法解析基础设施，提供以下核心能力：

| 概念 | 说明 |
|:---|:---|
| [**数据模型**](./core-systems/data-model.md) | ISource、TextSpan、GreenNode、RedNode、SyntaxTree 核心抽象 |
| [**解析器辅助**](./core-systems/parsing.md) | CstBuilder 栈上构建器 + ParseContext 解析上下文 |
| [**增量重解析**](./core-systems/incremental-reparse.md) | 编辑后自动定位受影响子树并增量更新 |
| [**强类型 AST**](./core-systems/typed-ast.md) | SyntaxNode 抽象基类，用户手写继承体系 |
| [**语言注入**](./core-systems/language-injection.md) | 嵌套子树机制，支持多语言交织 |
| [**程序化构造**](./core-systems/synthetic-construction.md) | 脱离解析环境使用 CstBuilder 合成新节点 |
| [**Language 抽象**](./core-systems/language-abstraction.md) | 方言配置对象，语法特性开关控制 |
| [**LanguageRegistry**](./core-systems/language-registry.md) | 语言 ID → 解析器的全局注册机制 |
| [**可选工具**](./core-systems/utilities.md) | LineIndex、DiagnosticCollector、错误恢复组合子 |
| [**设计哲学**](./intelligence/design-philosophy.md) | 纯偏移、不可变绿树、手写而非生成 |
| [**常见问题**](./intelligence/faq.md) | Nyar.Intelligence 使用 FAQ |
| [**分层架构**](./intelligence/layer.md) | Nyar 体系中解析层与语义层、工作区层的分层 |

## 二进制基础设施 (Nyar.Binary)

Nyar.Binary 源自 Acorn，是 Nyar 体系中的高性能二进制编解码基础设施，提供以下核心能力：

| 概念 | 说明 |
|:---|:---|
| [**架构设计**](./core-systems/binary-architecture.md) | ByteBuffer、ICodec、IFrameProtocol、FrameScanner 等核心抽象 |
| [**特性系统**](./core-systems/binary-attributes.md) | BinarySerializable、Field、BitField、OffsetTable、AlgebraicUnion |
| [**框架概述**](./binary/overview.md) | 设计哲学、快速开始、项目结构 |
| [**常见问题**](./binary/faq.md) | Nyar.Binary 使用 FAQ 与最佳实践 |
| [**源生成器**](./binary/source-generator.md) | Roslyn 增量生成器契约与行为 |
| [**典型用例**](./binary/examples.md) | GLB、MySQL、Live2D、WASM 等解析示例 |
| [**扩展点**](./binary/extensibility.md) | 自定义编解码器、帧协议、协议工厂 |

## 架构全景

```
┌────────────────────────────────────────────────────────────────────┐
│                    方言 OA 接口（唯一定义）                           │
│            ICoreAlg<E> : IStandardAlg<E> : IGameAlg<E> : ...       │
└────────────────────────────┬───────────────────────────────────────┘
                             │
      ┌──────────────────────┼──────────────────────┐
      │                      │                      │
      ▼                      ▼                      ▼
┌───────────┐        ┌───────────┐        ┌───────────┐
│  编译器    │        │   IDE     │        │  运行时    │
│           │        │           │        │           │
│ EGraph    │        │ PSI 工厂   │        │ NyarVM    │
│ PE 工厂   │        │ 语法高亮   │        │ JVM 后端  │
│ 成本模型  │        │ 代码补全   │        │ WASM 后端 │
│ 规则引擎  │        │ 重构引擎   │        │ 原生后端  │
└─────┬─────┘        └─────┬─────┘        └─────┬─────┘
      │                    │                    │
      └────────────────────┼────────────────────┘
                           │
                    ┌──────┴──────┐
                    │ 多目标代码生成│
                    │ (OA 工厂实现)│
                    └─────────────┘
```

## 版本信息

- **版本**：1.0
- **发布日期**：2026 年 Q2
- **状态**：设计阶段（OA 架构迁移中）