# Nyar — 全领域优化器

**Nyar** 是一个基于 E-Graph 饱和优化的全领域编译器框架。它不是某个语言的编译器，而是**所有语言**
的编译器基础设施——类似 LLVM 之于底层优化、GraalVM 之于多语言运行时，但 Nyar 的野心更大：
**一切可表示为代数重写的优化问题，都应在 Nyar 中求解。**

---

## 核心思想

```
                     ┌──────────────────────────────┐
                     │          Nyar                 │
                     │    全领域优化器（中心）          │
                     │                              │
                     │  EGraph · Rewrite · CostModel │
                     │  Extractor · PartialEval      │
                     │  Dialects · TypeInfer         │
                     └──────────────┬───────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
          ↓                         ↓                         ↓
    ┌──────────┐            ┌──────────────┐          ┌──────────────┐
    │  Oak.cs  │            │   Nyar 自身    │          │  Acorn.cs    │
    │ 文本编解码 │            │  代码生成+运行时 │          │ 二进制编解码   │
    └──────────┘            └──────────────┘          └──────────────┘
```

Nyar 不直接处理文本（那是 Oak 的职责），也不直接处理二进制（那是 Acorn 的职责）。
Nyar 独占**分析、优化、代码生成**——这是编译链条中最有价值的部分。

---

## 上游：两大基础设施

| 项目 | 一句话定义 | 职责 |
|:---|:---|:---|
| **Oak** | 一切文本编解码 | 文本 ↔ 结构：Lexer、Parser、Formatter |
| **Acorn** | 一切二进制编解码 | 字节流 ↔ 数据结构：Encoder、Decoder、Scanner |

Nyar 消费 Oak 产出的 AST 和 Acorn 提供的数据模型，专注于中间的优化和代码生成。

---

## 下游：全领域覆盖

Nyar 的优化能力不限于传统编译器——任何能用代数规则描述的问题，都可以交给 Nyar。

### 数据库

```
SQL → Oak 解析 → EGraph IR → 查询重写/计划优化 → NyarVM/Native 执行
```

- 查询计划等价重写（谓词下推、Join 重排、视图展开）
- 基于成本模型的物理计划选择
- 内嵌 NyarVM 的存储过程引擎
- 协议二进制编解码（MySQL / PostgreSQL / Redis）由 Acorn 提供

### 深度学习

```
ONNX / PyTorch Graph → EGraph IR → 算子融合/布局优化 → NyarVM/WASM/GPU 执行
```

- 算子等价重写（`Conv + BN + ReLU` 融合为单一算子）
- 张量布局优化（NHWC ↔ NCHW 自动转换）
- 计算图部分求值（符号维度推导）
- 多后端代码生成（GPU Compute Shader / WASM SIMD / Native）
- ONNX 模型编解码由 Acorn 提供

### 符号计算

```
数学表达式 → EGraph IR → 代数化简/求导/积分 → 数值代码生成
```

- 多项式因式分解与展开
- 自动微分（前向/反向模式）
- 符号积分规则库
- 数值稳定性重写（避免灾难性抵消）
- 生成高性能数值代码（C / WASM / Native）

### 前端开发

```
AWSL / JSX / 模板 → EGraph IR → 死代码消除/常量折叠 → WASM / JS 输出
```

- 响应式依赖图优化（消除冗余订阅）
- CSS-in-JS 样式去重与合并
- 组件树静态提升（编译器时代替运行时 Virtual DOM diff）
- 路由预计算
- Service Worker 生成

### 游戏引擎

```
GGScript / GGShader → EGraph IR → 逻辑优化/Shader 优化 → GnosisVM / SPIR-V
```

- Game 方言特化（实体查询、物理碰撞）
- Shader 死代码消除与指令合并
- 跨平台 Shader 编译（SPIR-V / HLSL / MSL / GLSL）
- 资源管线（纹理压缩、模型格式转换）由 Acorn 提供 40+ 格式支持

### 编程语言

```
任意语言 → Oak 解析 → EGraph IR → 优化 → 多后端 AOT/JIT
```

| 后端 | 输出格式 | 编解码 |
|:---|:---|:---|
| NyarVM | `.nyar` | Acorn.Nyar |
| GnosisVM | `.gnosis` | Acorn.Gnosis |
| JVM | `.class` | Acorn.Jvm |
| CLR | `.dll` / `.exe` | Acorn.Clr |
| WASM | `.wasm` | Acorn.Wasm |
| Native | `.elf` / `.exe` / `.dylib` | Acorn.Elf / Pe / MachO |

### 更多领域（规划中）

| 领域 | 应用 | 优化类型 |
|:---|:---|:---|
| **加密** | 零知识证明电路优化 | 约束重写、门消除、R1CS 降级 |
| **金融** | 量化交易策略编译 | 代数化简、向量化、风险计算融合 |
| **物理仿真** | 刚体/流体求解器 | 数值稳定性重写、SIMD 自动向量化 |
| **网络协议** | 协议解析器生成 | 状态机合并、分支优化 |
| **正则引擎** | 模式匹配编译 | NFA→DFA 转换、字符类优化 |

---

## 两大运行时

基于 Nyar 生成的后端，建立两种互补的运行时：

| 运行时 | 基础方言 | 定位 | JIT |
|:---|:---|:---|:---|
| **NyarVM** | Standard 方言 | 通用计算运行时 | ✅ |
| **GnosisVM** | Game 方言 | 游戏运行时 | ✅ |

---

## 项目地图

```
RiderProjects/
├── Oak.cs/              ← 一切文本编解码（Lexer / Parser / Formatter）
│   ├── Oak.Valkyrie/    GGScript/GGShader 文本
│   ├── Oak.Verse/       Verse 叙事语言
│   ├── Oak.WasmText/    WAT 文本
│   ├── Oak.Jasmin/      Jasmin 汇编文本
│   ├── Oak.Csv/Json/    结构化文本
│   └── ...
│
├── Acorn.cs/            ← 一切二进制编解码（Encoder / Decoder / Scanner）
│   ├── Acorn.Nyar/      .nyar 格式
│   ├── Acorn.Wasm/      .wasm 格式
│   ├── Acorn.Jvm/       .class 格式
│   ├── Acorn.SpirV/     .spv 格式
│   ├── Acorn.Llvm/      .bc 格式
│   └── ... (40+ 格式)
│
├── NyarVM.cs/           ← 一切分析与优化 + NyarVM 运行时
│   ├── Nyar.Core/       EGraph · Rewrite · Extractor · UnionFind
│   ├── Nyar.Assembler/  代码生成后端（Wasm / JVM / CLR / Native）
│   ├── Nyar.VM/         NyarVM 运行时 + JIT
│   ├── Nyar.Optimizer/  优化 Pass
│   ├── Nyar.Dialect.*/  方言降级规则
│   ├── Nyar.Database/   数据库引擎
│   ├── Nyar.ObjectAlgebra/  代数规则引擎
│   └── tools/legion/    包管理兼构建工具
│
├── Valkyrie.cs/         ← GGScript/GGShader 语言前端 + 管线编排
│
├── Gnosis.cs/           ← 元游戏引擎（ECS / Graphic / Audio / Physics）
│   └── GnosisVM         Game 方言运行时
│
└── Sonic.cs/            ← 标准库（Option / Result / Collections / Math）
```

---

## 快速开始

```bash
# 编译 Valkyrie 项目
legion build

# 运行测试（默认 NyarVM）
legion test

# 多 target 测试
legion test --target nyar,clr,jvm

# 基准测试
legion bench --target all

# 覆盖率检查
legion coverage
```

---

## 设计原则

1. **每种格式只有一个 Source**：文本格式全在 Oak，二进制格式全在 Acorn，绝不重复实现。
2. **Nyar 不做编解码**：Oak 处理文本，Acorn 处理二进制，Nyar 专注于中间的优化和生成。
3. **方言即规则**：每个方言是一组降级规则，通过 E-Graph 饱和重写自动降级。
4. **成本模型驱动**：最优程序由成本模型自动选择，而非手写 heuristics。
5. **一切可重写**：任何能用代数等式表示的问题，都可以建模为 Nyar 的重写规则。

---

## 为什么叫 Nyar？

Nyar 来自匈牙利语 *nyár*（夏天），与现有的 LLVM / GraalVM / Cranelift 等"冬季系"命名形成对比。我们相信编译器的未来应该像夏天一样充满生命力——跨语言、跨领域、跨平台。