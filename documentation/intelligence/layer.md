# Nyar 体系集成 — Oak 与上层架构的分层

Oak 2.0 是纯语法框架，不涉足语义。语义分析、文件管理、LSP 协议等由 Nyar 体系的上层负责。以下是严格分层后的架构定义。

---

## 分层架构

```
┌──────────────────────────────────────────────┐
│     Nyar.Tooling (LSP, CLI, 测试工具)         │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Workspace (文件/项目/调度)           │
│     VirtualFile, Project, Workspace          │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Semantic (符号/类型/索引/语义高亮)   │
│     实现 ISyntaxContext, SemanticModel        │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Nyar.Syntax (纯语法高亮、语法便利查询)    │
│     SyntaxHighlighter, SyntaxQuery            │
└──────────────────────────────────────────────┘
                      │
┌──────────────────────────────────────────────┐
│     Oak 2.0 (纯语法: ISource, CST, AST,       │
│     Language, SyntaxTree 多根, 工厂, 增量)    │
└──────────────────────────────────────────────┘
```

---

## 各层精确定义

### 1. Nyar.Syntax 层 — 语法层的增强与着色

| 组件 | 职责 | 不在 Oak 的原因 |
|:---|:---|:---|
| `SyntaxHighlighter` | 遍历 `SyntaxRoot`，根据 `NodeKind` / `TokenKind` 输出高亮 token 流。完全不接触符号。 | Oak 不知道 IDE 概念，不定义高亮格式。 |
| `SyntaxQuery` | 提供 `DescendantsOfKind` 等便利扩展。 | 无状态工具类。 |
| `SyntaxAnnotator` (可选内部) | 简化为 `SyntaxHighlighter` 内部的模式识别器，不暴露。 | |

**不包含**：
- `SyntaxFactory` (已下放 Oak)
- `SyntaxTreeViewProvider` (已删除，功能在 Oak)
- 任何语义标注、跨文件引用

### 2. Nyar.Semantic 层 — 语义信息与语义高亮

| 组件 | 职责 |
|:---|:---|
| `SemanticModel` | 绑定单个 `SyntaxTree` 的符号表、类型信息、语义诊断。 |
| `ISymbolTable` / `IScope` / `ISymbol` | 符号体系。 |
| `TypeChecker` / `IType` | 类型检查器基类。 |
| `SemanticIndex` / `IStubBuilder` | 全局索引与 Stub 提取。 |
| `SemanticHighlighter` | 基于 `SemanticModel` 输出语义着色 token 流（如 `type`、`mutable variable` 等）。 |
| `SemanticSyntaxContext` | 实现 `ISyntaxContext`，为解析器提供解析时需的名称分类、优先级表。 |

**不包含**：任何文件操作、IO、项目组织。

### 3. Nyar.Workspace 层 — 文件、项目、调度

- `VirtualFile` 封装文件路径、内容、`LanguageId`。
- `Project` 管理文件集与依赖。
- `Workspace` 作为中央枢纽，负责 `VirtualFile` → `SyntaxTree` → `SemanticModel` 的级联与增量更新。
- 负责编辑事务（`ApplyChange`），触发 Oak 增量重解析，并协调语义索引更新。

**明确职责边界**：Workspace **不实现**任何语法解析或类型检查算法；它只**编排** Oak 和 Nyar.Semantic 的服务。

### 4. Nyar.Tooling 层

- LSP 服务：消费 `Workspace` 提供的数据，生成 LSP 响应，包括语法/语义高亮结果。
- 命令行编译器：串联 Oak 解析 → Nyar.Semantic 分析 → 后端代码生成。
- 测试工具。

---

## 关键修正点

### 1. SyntaxFactory 下放至 Oak

- **原错误**：将构造强类型 AST 的工厂方法放在 Nyar.Syntax。
- **修正**：程序化构造语法树是**纯语法行为**，不依赖任何语义或文件系统。Oak 必须提供：
  - 底层 `CstBuilder`（已有）。
  - **强类型 AST 工厂基类**：用户手写 `BinaryExpr.Create(...)` 时继承的 `SyntaxFactory` 泛型基类，以及从 `ISource` 文本直接解析出 `SyntaxTree` 的 `SyntaxTreeFactory`。
  - `LanguageRegistry.Parse()` 本身就是一个工厂入口。
- **结论**：`SyntaxFactory` 概念完全被 Oak 的 `CstBuilder` + `LanguageRegistry` + `ISource` 覆盖。**Nyar.Syntax 不再包含任何构造能力**。

### 2. SyntaxTreeViewProvider 无存在意义

- **原因**：Oak 2.0 的 `SyntaxTree` 已经原生支持多 `SyntaxRoot`（`PrimaryRoot`, `AllRoots`, `GetRoot(languageId)`）。它本身就是一个多语言语法树视图。
- **修正**：删除 `SyntaxTreeViewProvider`，功能由 `SyntaxTree` 直接提供。

### 3. 高亮彻底分家：SyntaxHighlight 与 SemanticHighlight

- **语法高亮**：基于词法 token 类型和纯语法结构（关键字、字符串、注释、括号匹配）。**不应离开语法层**。
- **语义高亮**：基于符号种类（类、函数、可变变量、不可变变量）、类型信息。**必须依赖语义模型**。
- **修正**：
  - **语法高亮**划入 **Nyar.Syntax**，由 `SyntaxHighlighter` 实现，输入 `SyntaxTree`。
  - **语义高亮**划入 **Nyar.Semantic**，由 `SemanticHighlighter` 实现，输入 `SemanticModel`。
  - 原 `SyntaxAnnotator` 概念简化为高亮的内部辅助，不再独立成核心类。

---

## 与 JetBrains PSI 的模型对应

| PSI 概念 | 对应 Nyar 体系 |
|:---|:---|
| `ASTNode` (底层树) | Oak `GreenNode` / `RedNode` |
| `PsiFile` (树根) | Oak `SyntaxTree` + `SyntaxRoot` |
| `FileViewProvider` (多视图) | Oak `SyntaxTree.AllRoots` / `GetRoot(languageId)` |
| `PsiBuilder` / `PsiParser` | Oak `CstBuilder` + 手写解析器 |
| `PsiElementFactory` (构造) | Oak `CstBuilder` + 强类型 AST 工厂 |
| Syntax Highlighting | Nyar.Syntax `SyntaxHighlighter` |
| Semantic Highlighting | Nyar.Semantic `SemanticHighlighter` |
| `PsiReference` / 引用解析 | Nyar.Semantic `ISymbolTable` + 索引 |
| `PsiStubs` / 索引 | Nyar.Semantic `StubTree` + `SemanticIndex` |
| `VirtualFile` / `Module` | Nyar.Workspace `VirtualFile` / `Project` |
| `PsiManager` / `PsiDocumentManager` | Nyar.Workspace `Workspace` (编辑调度) |

---

## 最终检查清单：谁不做什么

| 层级 | 绝对禁止的行为 |
|:---|:---|
| **Oak 2.0** | 语义分析、文件路径、高亮格式、项目模型 |
| **Nyar.Syntax** | 符号表、类型、跨文件引用、文件路径 |
| **Nyar.Semantic** | 文件系统操作、UI 渲染、LSP 协议 |
| **Nyar.Workspace** | 语法解析算法、类型检查细节、磁盘索引格式 |
| **Nyar.Tooling** | 定义任何语法/语义数据结构 |