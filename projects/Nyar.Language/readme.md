# Nyar.Language

## 概述

`Nyar.Language` 是 Nyar 体系中的**多语言前端与语义分析层**。它的职责是"看懂源语言"——负责解析、语义分析和 lowering，为后续的 `Nyar` 优化和 `NyarVM` / `LegacyVM` 执行做好准备。

`Nyar.Language` 不负责优化、代码生成、二进制编解码或 VM 执行，这些分别属于 `Nyar`、`Acorn` 和 `Nyar.VM.*`。

## 当前状态

### 当前事实

- `Nyar.Language.Valkyrie` 是 Valkyrie 官方标准语言的完整前端，包括 Lexer、Parser、TypeChecker、HIR/MIR/LIR lowering、编译器管线
- 其他语言（Python、TypeScript、JavaScript、Rust、Julia、C、Lua、Bash、PowerShell、Batch、SQL、Wasm 等）的解析器当前分散在 `Sonic.Standard.Data.Text` 中，由 `LegacyVM` 引用编译
- `Nyar.Language` 项目当前主要承载 Valkyrie 编译器管线和各类语言的 `ILanguage` 注册

### 当前价值

`Nyar.Language` 当前的价值有两层：

- 为 Valkyrie 提供完整的前端到 lowering 管线（Lexer → Parser → HIR → MIR → LIR → CodeGen）
- 为 `LegacyVM` 和工具链提供统一的 `ILanguage` / `ILanguageService` 抽象，让 `legend` 等工具能发现和调度不同语言

## 未来规划

### 未来角色

未来当更多高价值语言需要进入 `NyarVM` 主性能路线时，`Nyar.Language` 应扩展为：

- 为更多语言提供 parser 与语义分析
- 为更多语言建立到 `Nyar` IR 的 lowering
- 让产物进入 `NyarVM` 正式执行

### 未来不应承担的工作

`Nyar.Language` 不应：

- 实现优化器（那是 `Nyar` 的职责）
- 实现代码生成后端（那是 `Nyar.Assembler` 的职责）
- 实现 VM 运行时（那是 `Nyar.VM.NyarVM` 的职责）
- 处理二进制编解码（那是 `Acorn` 的职责）

## 在整体架构中的位置

Nyar 体系的五层架构：

```text
Nyar.Language        → 多语言前端与语义 lowering（本层）
Nyar                 → 统一 dialect / IR / 规则 / 优化
Nyar.VM.LegacyVM     → 参考解释器 / 万能编译器平台 / legacy 生产执行层
Nyar.VM.NyarVM       → 正式统一执行器 / bytecode / GC / JIT / OSR / FFI
tools/legend         → 唯一编排入口
```

其中 `Nyar.Language` 的职责是：

- 把源码解析为 AST（消费 Oak 的 Lexer/Parser）
- 进行名字解析、类型检查、作用域/闭包分析
- 进行 object model lowering
- 语言特定 desugar
- 将结果送入 `Nyar` 统一 IR 进行优化

## 设计定位

### 1. 只负责前端与 lowering

`Nyar.Language` 的边界是清晰的：

**包含**：
- Tokenizer / Lexer（消费 Oak 基础设施）
- Parser（消费 Oak 基础设施）
- 类型检查与类型推断
- 语义分析（作用域、闭包、对象模型）
- HIR / MIR / LIR lowering
- 语言到 `Nyar` IR 的桥接

**不包含**：
- IR 优化（`Nyar` 的职责）
- 代码生成（`Nyar.Assembler` 的职责）
- 二进制编码（`Acorn` 的职责）
- VM 执行（`Nyar.VM.*` 的职责）

### 2. 当前仅 Valkyrie 有完整 lowering 管线

当前只有 Valkyrie 拥有从前端到 lowering 的完整路径：

```
Valkyrie 源码
  → Oak.Valkyrie (Lexer + Parser)
  → Nyar.Language.Valkyrie (TypeChecker + Semantic)
  → Nyar.Language.Valkyrie.Compiler (HIR → MIR → LIR)
  → Nyar.Assembler (CodeGen)
  → Nyar.VM.NyarVM (执行)
```

其他语言当前通过 `LegacyVM` 的 evaluator 直接解释执行，不经过完整的 lowering 管线。

### 3. 统一的 ILanguage 抽象

`Nyar.Language` 提供 `ILanguage` 和 `ILanguageService` 接口，让工具链能：

- 发现已注册的语言
- 根据文件扩展名或内容检测语言类型
- 调度到正确的编译或执行路径

```text
legend
  → ILanguageService.GetLanguages()
  → 根据文件/源码检测语言
  → 选择执行路径:
      Valkyrie → NyarVM
      Others   → LegacyVM
```

## 当前结构

### Valkyrie 前端

`Nyar.Language.Valkyrie/` 包含完整的 Valkyrie 编译器前端：

| 层级 | 目录 | 职责 |
|:---|:---|:---|
| HIR | `Valkyrie/Compiler/Hir/` | 高层中间表示，保留完整类型信息和语义结构 |
| MIR | `Valkyrie/Compiler/Mir/` | 中层中间表示，HIR → MIR lowering，基本优化 |
| LIR | `Valkyrie/Compiler/Lir/` | 低层中间表示，接近目标平台的操作 |
| Pipeline | `Valkyrie/Compiler/Pipeline/` | 编译管线编排、缓存、产物管理 |
| Semantic | `Valkyrie/Compiler/Semantic/` | 语义分析：声明收集、模块依赖图 |
| TypeChecker | `Valkyrie/TypeChecker/` | 类型检查与类型推断 |
| TypeSystem | `Valkyrie/TypeSystem/` | Valkyrie 类型系统定义 |

### 多语言注册

`Nyar.Language/` 根目录提供各语言的 `ILanguage` 实现：

| 语言 | 文件 | 当前状态 |
|:---|:---|:---|
| Valkyrie | `Valkyrie/` | 完整前端 + lowering 管线 |
| Python | `Python/PythonLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| TypeScript | `Typescript/TypeScriptLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| JavaScript | `JavaScript/JavaScriptLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Rust | `Rust/RustLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Julia | `Julia/JuliaLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| C | `C/CLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Lua | `Lua/LuaLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Bash | `Bash/BashLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| PowerShell | `PowerShell/PowerShellLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Batch | `Batch/BatchLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| SQL | `Sql/SqlLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |
| Wasm | `Wasm/WasmLanguage.cs` | 解析器在 `Sonic.Standard.Data.Text`，由 `LegacyVM` 编译 |

> 注意：上述语言的 Lexer/Parser 当前位于 `Sonic.Standard.Data.Text` 项目中，通过 `LegacyVM.csproj` 的 `<Compile Include>` 引用编译。从架构角度看，这些解析器最终应迁入 `Oak.*` 命名空间，而非继续留在 `Sonic` 中。

### Web Style 统一管线

| 项目 | 目录 | 职责 |
|:---|:---|:---|
| **WebStyle** | `WebStyle/` | **推荐接入面** — CSS / SCSS / Tailwind 的统一编译、合并与输出门面 |
| CSS | `Css/` | 底层 CSS 数据模型与合并工具（`CssMerger` 是底层工具，非最终接入面） |
| SCSS | `Scss/` | SCSS 数据模型 |
| Tailwind | `Tailwind/` | Tailwind 配置与工具类数据模型 |
| AWSL/Asgard | `Awsl.Asgard/` | AWSL 组件编译、DevServer、SSR、HMR |

#### WebStylePipeline — 统一门面

`Nyar.Language.WebStyle.WebStylePipeline` 是所有下游接入 Web 样式能力的**唯一推荐入口**。

**能力**：
- 原始 CSS 合并（多来源 → 统一 bundle）
- SCSS 编译为 CSS（通过 `Std.Data.Text.Scss.StyleSheet.parse()`）
- Tailwind 扫描生成 CSS（通过 `Nyar.Language.Tailwind`）
- 三类产物统一合并为单一 bundle

**下游接入方式**：
```csharp
var pipeline = new WebStylePipeline
{
    Header = "/* 我的项目样式 — 自动生成 */",
};

var assets = new List<WebStyleAsset>
{
    WebStyleAsset.css("API 文档样式", apiCssText),
    WebStyleAsset.scss("dark-theme", scssSource),
};

var bundle = pipeline.compile_all(assets, bundleName: "app-styles");
File.WriteAllText("app.css", bundle.Css);
```

**禁止方向**：
- 下游不得自行实现等价的 CSS bundle 合并逻辑 — 必须通过 `WebStylePipeline` 接入
- 下游不得自行实现 SCSS 序列化 — 编译委托给 `compile_scss()`
- 下游只负责样式来源发现与输出落盘
- `CssMerger` 是底层工具，不应作为下游直接面对的唯一入口

## 语言接入路径

### 主流语言（推荐路径）

对于未来要进入 `NyarVM` 主性能路线的高价值语言：

1. 在 `Oak.<Lang>` 实现 Lexer 和 Parser（文本解码）
2. 在 `Nyar.Language.<Lang>` 实现语义分析和 lowering
3. 建立到 `Nyar` IR 的桥接
4. 让产物进入 `NyarVM` 正式执行
5. 仅在必要时补一个 `LegacyVM` 参考 evaluator

### Legacy / 小众语言（替代路径）

对于长尾 legacy 语言：

1. 解析器保留在 `Sonic.Standard.Data.Text`（短期）或迁入 `Oak`（长期）
2. 在 `Nyar.Language` 注册 `ILanguage` 实现
3. 在 `LegacyVM` 建立 evaluator 进行源码级解释执行
4. 直接作为生产可用方案交付
5. 只有在收益足够高时才考虑迁入主线

### 不推荐的路径

- 在 `Nyar.Language` 中实现优化器或代码生成
- 在 `LegacyVM` 中复制完整的 lowering 管线
- 为每种语言在 `NyarVM` 中增加前端特定的执行分支

## 与其他项目的关系

### 与 Oak 的关系

`Nyar.Language` 消费 Oak 的 Lexer 和 Parser 产出的 AST，不自己实现文本编解码。

### 与 Nyar 的关系

`Nyar.Language` 将 lowering 后的产物送入 `Nyar` 进行统一优化。`Nyar` 负责方言、IR、规则、优化和 ABI 桥接。

### 与 LegacyVM 的关系

`Nyar.Language` 为 `LegacyVM` 提供语言发现和注册能力。当前除 Valkyrie 外的大多数语言通过 `LegacyVM` 解释执行，`Nyar.Language` 帮助 `legend` 正确调度这些语言。

### 与 NyarVM 的关系

`Nyar.Language` 为 `NyarVM` 提供 lowering 后的统一产物。当前只有 Valkyrie 走这条路径，未来可扩展到更多高价值语言。

## 一句话总结

`Nyar.Language` 的正确定位是：

- 多语言前端解析与语义分析的统一入口
- Valkyrie 完整 lowering 管线的承载者
- 其他语言 `ILanguage` 注册与发现的基础设施
- `Nyar` 优化器和 `NyarVM` 执行器的上游输入源
- 不是优化器、不是代码生成器、不是 VM 运行时

## 相关文档

- [Nyar](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar/readme.md)
- [Nyar.VM.LegacyVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.LegacyVM/readme.md)
- [Nyar.VM.NyarVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.NyarVM/readme.md)
- [legend](file:///e:/RiderProjects/NyarVM.cs/tools/legend/readme.md)
- [legacy-full-pipeline spec](file:///e:/RiderProjects/.trae/specs/legacy-full-pipeline/spec.md)