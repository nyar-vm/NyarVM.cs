# Valkyrie 生态预演

`projects/` 目录是 `valkyrie.v` 生态系统的 .NET 实现基础。两者是**源码分发层**与**编译运行时层**的关系——就像 Python 发行包依赖 CPython 解释器，`valkyrie.v` 生态中的 `.v` 源码依赖此处的 .NET 实现来编译、优化和运行。

---

## 关系：`projects/` ↔ `valkyrie.v`

```
valkyrie.v/ (源码分发层)
├── bootstrap.v/   ──→  Valkyrie 语言 + 工具链 (VCC/Legion)  ──→  依赖 projects/Valkyrie.* 编译
├── std.v/         ──→  标准库 (core + std + adaptors)        ──→  依赖 projects/Nyar.VM 运行
├── asgard.v/      ──→  前端框架 (AWSL 组件)                  ──→  依赖 projects/Nyar.Assembler 生成 WASM
├── atlas.v/       ──→  云平台适配器                          ──→  依赖 projects/Nyar.Assembler 多后端
├── nyar.v/        ──→  Nyar 核心                            ──→  依赖 projects/Nyar.Core (IR/Optimizer)
└── yuanshen.v/    ──→  自举应用 (原神 Git GUI)               ──→  依赖 projects/ 全栈编译

                            ↑ 所有 .v 源码最终编译至此处的 .NET 实现上运行 ↑

NyarVM.cs/projects/ (编译运行时层)
├── Sonic/            标准库 (.NET 实现)
├── Nyar.Core/        优化器核心 (EGraph / Rewrite / Extractor)
├── Nyar.Assembler/   代码生成后端 (Wasm / JVM / CLR / SPIR-V)
├── Nyar.VM/          NyarVM 运行时 + JIT
├── Nyar.Dialect.*/   方言降级规则
├── Nyar.Database/    数据库引擎
├── Nyar.Language.*/  语言服务 (Valkyrie / Von / AWSL)
├── Nyar.Protocol.*/  网络协议 (MySQL / PostgreSQL / Redis / ZeroMQ)
├── Nyar.Workspace/   增量分析 + 项目模型
└── tools/legion/     Legion 包管理兼构建工具
```

> **关键认知**：`valkyrie.v` 是用户看到和使用的"发行包"，`projects/` 是支撑它的"引擎"。两者最终都会独立发布为 NuGet 包，但现在聚合在同一个仓库中以极低成本验证架构。

---

## `valkyrie.v` 发行包结构

每个 `valkyrie.v` 子目录是一个独立的 workspace，包含 `projects/`（库）和 `examples/`（示例/测试），可以独立版本化、独立发布：

```
valkyrie.v/
├── bootstrap.v/            # Valkyrie 语言核心分发
│   ├── projects/
│   │   └── valkyrie/        # Valkyrie VM (Valkyrie 语言自举运行时)
│   ├── examples/            # 28 个语法特性测试包
│   │   ├── test.hello_world/  source/ + test/ + legion.von
│   │   ├── test.pattern_match/source/ + test/ + legion.von
│   │   └── ...
│   ├── documentation/       # 完整文档体系 (language/developer/maintainer/toolchain)
│   └── legions.von
│
├── std.v/                   # 标准库分发
│   ├── projects/
│   │   ├── core/            # 基础类型 (primitive + text + traits)
│   │   ├── std/             # 标准库 (collection/command/crypto/io/math/net/text/types)
│   │   ├── std.adaptor.dotnet/
│   │   ├── std.adaptor.jvm/
│   │   ├── std.adaptor.linux/
│   │   ├── std.adaptor.macos/
│   │   ├── std.adaptor.nyar/
│   │   ├── std.adaptor.wasip1/
│   │   ├── std.adaptor.wasip2/
│   │   ├── std.adaptor.wasm/
│   │   └── std.adaptor.windows/
│   └── legions.von
│
├── asgard.v/                # 前端框架分发
│   ├── projects/
│   │   ├── asgard/          # 核心组件库 (button, card, input, modal, tabs, toast)
│   │   ├── asgard.router/   # 路由
│   │   └── voa.runtime/     # VOA 运行时 (JS/WASM)
│   ├── examples/            # 7 个前端示例 (blog, dashboard, admin, fullstack...)
│   └── legions.von
│
├── atlas.v/                 # 云适配器分发
│   ├── projects/
│   │   ├── atlas/
│   │   ├── atlas.adaptor.aliyun/
│   │   ├── atlas.adaptor.azure/
│   │   └── atlas.adaptor.tencent/
│   └── legions.von
│
├── nyar.v/                  # Nyar 核心分发
│   ├── projects/
│   │   └── nyar.core/
│   └── legions.von
│
├── yuanshen.v/              # 自举应用分发 (原神 - Git GUI)
│   ├── projects/
│   │   └── yuanshen/
│   └── legions.von
│
└── legions.von              # 根 workspace，串联所有子系统
```

### 约定

| 约定 | 说明 |
|:---|:---|
| `source/` | 库源码目录 |
| `test/` | 特殊测试目录，仅 `legion test` 时编译 |
| `script/` | 可执行脚本入口 |
| `examples/` | 示例项目（自身也是包，含 `source/` + `test/` + `legion.von`） |
| `legion.von` | 单包清单 |
| `legions.von` | 多包 workspace 清单 |
| `voa.config.v` | VOA 前端构建配置 |
| `dist/` | 最终部署产物（仅 `legion build` 产出） |
| `.cache/` | 中间/缓存产物（test/bench 编译输出、增量编译缓存） |

---

## 五层架构总览

Nyar 体系的职责划分遵循以下五层架构：

```text
Nyar.Language        → 多语言前端解析与语义 lowering
Nyar                 → 统一方言 / IR / 规则 / 优化 / ABI
Nyar.VM.LegacyVM     → 参考解释器 / 万能编译器平台 / legacy 生产执行层
Nyar.VM.NyarVM       → 正式统一执行器 / bytecode / GC / JIT / OSR / FFI
tools/legend         → 唯一编排入口
```

### 当前执行路径

当前真实执行格局为两条路径并存：

```
Valkyrie → Nyar.Language.Valkyrie → Nyar IR → Nyar.Assembler → NyarVM
Other Languages → Nyar.Language (发现) → LegacyVM (解释执行)
```

- **Valkyrie** 走完整 lowering 管线，经 `Nyar` 优化后进入 `NyarVM` 正式执行
- **其他语言** 目前通过 `LegacyVM` 的 evaluator 直接解释执行
- `NyarVM` **只感知** lowering 后的统一产物和 ABI，**不感知**前端语言名称与表层特性
- `LegacyVM` 不是"仅供调试"的边缘工具，而是当前除 Valkyrie 外的**主要多语言承载平台**

### 未来主性能路线

未来主性能路线为：`source → Nyar.Language → Nyar IR → optimize → NyarVM`。但这仅是规划，不是当前所有语言的既定任务。长尾 legacy 语言允许长期通过 `LegacyVM` 以较低投入获得生产可用执行能力。

详细设计见 [legacy-full-pipeline spec](file:///e:/RiderProjects/.trae/specs/legacy-full-pipeline/spec.md)。

## 编译管线：从 `.v` 到运行

```
.v 源码
  │
  ├─→ Oak (文本解码): Lexer → Parser → AST
  │    由 projects/Nyar.Language.Valkyrie 调用 Oak.Valkyrie
  │
  ├─→ Nyar (分析+优化): AST → EGraph<IKun> → Rewrite → Extract → IKunTree
  │    由 projects/Nyar.Core + Nyar.Optimizer + Nyar.Dialect.* 完成
  │
  ├─→ Nyar (代码生成): IKunTree → 目标数据结构
  │    由 projects/Nyar.Assembler 完成，输出到 .cache/
  │    支持: NyarVM / JVM / CLR / WASM / SPIR-V
  │
  ├─→ Acorn (二进制编码): 目标数据结构 → 二进制格式
  │    由 projects/Acorn.* 完成，最终产物进入 dist/
  │
  └─→ 运行时执行
       进程内: NyarVM (projects/Nyar.VM.NyarVM)
       外部:   CLR (dotnet), JVM (java), WASM (node)
       管理:   tools/legion IRunner 抽象
```

---

## `projects/` 内项目全景

### 基础设施层

| 项目 | 对应 valkyrie.v | 职责 |
|:---|:---|:---|
| `Sonic` | `std.v` | 标准库 .NET 实现 (Option/Result/集合/数学/文本/并发/CLI/HTTP/REPL) |
| `Nyar.Core` | `nyar.v` | IR (IKun) + EGraph + Rewrite + Extractor + UnionFind |
| `Nyar.ObjectAlgebra` | `nyar.v` | 代数规则引擎 |

### 编译器与运行时层

| 项目 | 对应 valkyrie.v | 职责 |
|:---|:---|:---|
| `Nyar.Assembler` | `bootstrap.v` | 代码生成后端 (IKun → NyarVM/JVM/CLR/WASM/SPIR-V) |
| `Nyar.Optimizer` | `bootstrap.v` | LoweringPass + DefaultCostModel |
| `Nyar.VM.NyarVM` | `bootstrap.v` | NyarVM 运行时：Valkyrie 官方标准语言 VM (Executor/Frame/GC/JIT/OSR/Profiling/Debug/HotReload) |
| `Nyar.VM.LegacyVM` | — | 参考解释器、万能编译器平台、legacy 语言生产可用执行层 |
| `Nyar.Workspace` | `bootstrap.v` | 增量分析 + 项目模型 |

### 方言层

| 项目 | 方言 | 最终归宿 |
|:---|:---|:---|
| `Nyar.Dialect.Core` | 基础方言 | `bootstrap.v` |
| `Nyar.Dialect.Standard` | Standard 方言 | `bootstrap.v` |
| `Nyar.Dialect.Game` | Game 方言 | `gnosis.v` (远期) |
| `Nyar.Dialect.Shader` | Shader 方言 | `bootstrap.v` |
| `Nyar.Dialect.Data` | Data 方言 | `bootstrap.v` |
| `Nyar.Dialect.Web` | Web 方言 | `asgard.v` |
| `Nyar.Dialect.Tensor` | Tensor 方言 | 📋 规划中 |

### 语言服务层

| 项目 | 职责 |
|:---|:---|
| `Nyar.Language` | 多语言前端与语义 lowering：`ILanguage`/`ILanguageService` 抽象，语言注册与发现 |
| `Nyar.Language.Valkyrie` | Valkyrie 语言完整前端 (Lexer/Parser/HIR/MIR/LIR/TypeChecker/Compiler) |
| `Nyar.Language.Von` | Von 配置格式语言服务 |
| `Nyar.Language.Awsl` | AWSL 组件语言服务 |

### 网络协议层

| 项目 | 协议 | 最终归宿 |
|:---|:---|:---|
| `Nyar.Protocol.MySql` | MySQL | Acorn (二进制协议) |
| `Nyar.Protocol.PostgreSql` | PostgreSQL | Acorn |
| `Nyar.Protocol.Redis` | Redis RESP | Acorn |
| `Nyar.Protocol.ZeroMQ` | ZeroMQ | Acorn |

### 工具层

| 项目 | 对应 CLI | 职责 |
|:---|:---|:---|
| `tools/legion` | `legion` | 包管理 (install/update/remove) + 构建 (build/test/bench/coverage) + 发布 (pack/publish) |

---

## 演进路线

| 阶段 | 内容 |
|:---|:---|
| **当前** | 所有 .NET 实现聚合在 `NyarVM.cs/projects/`，与 `valkyrie.v` 并行开发，快速迭代验证 |
| **收敛** | `Nyar.Intelligence` 中的 Lexer/Parser 基础设施迁入 `Oak.Core`；`Sonic.DataProcess` 迁入 `Acorn`；协议模块迁入 `Acorn` |
| **独立** | 每个项目独立为 NuGet 包，Sonic / Oak / Acorn / Nyar 各有自己的仓库和版本号 |
| **自举** | `valkyrie.v/bootstrap.v/projects/valkyrie/` 中的 Valkyrie VM 可以运行自身，Valkyrie 语言用 Valkyrie 语言编写 |
| **生态** | 第三方语言前端 (C/Rust/Python/Haskell...) 接入 Nyar 优化管线；`valkyrie.v` 成为真正的语言发行平台 |