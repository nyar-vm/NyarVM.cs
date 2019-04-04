# Nyar.VM.TextVM.Compiler

## 概述

`Nyar.VM.TextVM.Compiler` 是 TextVM 的**构建时编译器库**，无独立 CLI。它由 MSBuild Task（[Nyar.VM.TextVM.MsBuild](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.TextVM.MsBuild/TvmCompileTask.cs)）在 `dotnet build` 时隐式调用，将 `.tvm.pattern` 文件编译为 `.tvm` 字节码产物。

设计讨论基于 Rust，落地为 C#。

## 编译管道

```
模式字符串
    ↓
PatternParser.Parse()       ← 递归下降解析，支持 & | ! - 等扩展运算符
    ↓
BooleanFolder.Fold()        ← 编译期常量折叠（交 & 补 ! 差 -）
    ↓
StaticAnalyzer.Analyze()    ← 四维静态分析
    ├─ ComplexityClassifier  ── Literal / Regular / ContextFree
    ├─ PrefixExtractor       ── 字面量前缀集合
    ├─ EncodingAnalyzer      ── ASCII 纯子集 / BMP 检测
    └─ StateExplosionEstimator ── DFA 状态爆炸预估
    ↓
DFABuilder.Build()          ← Brzozowski 导数驱动的惰性 DFA 构造
    ↓
Serialize()                 ← 写入 .tvm 二进制头 + DFA 表
```

## AST 节点类型

支持 16 种节点，涵盖标准正则 + 布尔正则扩展：

| 节点 | 含义 | 对应语法 |
|------|------|----------|
| `LiteralNode` | 字面量字符串 | `abc` |
| `CharClassNode` | 字符类 | `[a-z]`、`[^0-9]` |
| `AnyNode` | 任意字符 | `.` |
| `ConcatNode` | 连接 | `ab` |
| `AltNode` | 并集（Alternation） | `a|b` |
| `StarNode` | Kleene 星 | `a*` |
| `PlusNode` | 正闭包 | `a+` |
| `OptionalNode` | 可选 | `a?` |
| `IntersectNode` | 交集 | `a&b` |
| `ComplementNode` | 补集 | `!a` |
| `DifferenceNode` | 差集 | `a-b` |
| `CaptureNode` | 捕获组 | `(a)` |
| `BackrefNode` | 反向引用 | `\1` |
| `AnchorNode` | 锚点 | `^`、`$`、`\b` |

## 静态分析四维判决器

### 1. 语言复杂度判决

- **Literal**：AST 根节点为单一字面量 → 运行时绕过所有自动机逻辑，走 Two-Way BM 快速路径
- **Regular**：仅含 `|`、`*`、`+`、`?`、`&`、`!`，无反向引用 → 路由到 DFA/JIT 快车道
- **ContextFree**：含 `\1` 反向引用 → 降级为安全回溯（强制超时）

### 2. 前缀/后缀物化

从 AST 中贪婪剥离头部必经的字面量链。例如 `https?://[^\s]+` 剥离出 `{"http://", "https://"}`，运行时先走 AC 多模预过滤。

### 3. 编码边界分析

- 若模式所有字符码点 < 128，标记 `IsAsciiOnly = true`，JIT 生成的代码不调用解码函数
- 若编码为 UTF-16 且模式全为 BMP 字符，标记 `IsBmpOnly = true`，省略代理对检测

### 4. 状态爆炸预估

基于 AST 节点数和嵌套深度估算 DFA 状态数：
- **Small**：< 10 节点 → < 100 状态，全量编译
- **Medium**：< 50 节点 → < 1000 状态，惰性 JIT
- **Large**：> 1000 状态，改走位并行 NFA

## 布尔代数常量折叠

对于 `IntersectNode`、`ComplementNode`、`DifferenceNode`，若操作数为 `CharClassNode`，在编译期执行集运算并重写为等价 `CharClassNode`，消除运行时 AND/OR/NOT 指令。

示例：`[aeiou] & [^e]` → `[aiou]`

## Brzozowski 导数驱动的 DFA

使用 Brzozowski 导数（Derivative）作为核心算法：

- `D_c(a|b) = D_c(a) | D_c(b)`
- `D_c(a&b) = D_c(a) & D_c(b)`
- `D_c(!a) = !D_c(a)`
- `D_c(a*) = D_c(a) · a*`

`Nullable(r)` 判断节点是否可匹配空串。`Normalize(r)` 消除冗余结构。

DFA 构造采用惰性策略：状态 = 规范化后的 AST 节点，转移 = 对每个探针字符取导数。默认上限 1024 状态，超出时触发降级信号。

## 产物格式

编译器输出 `.tvm` 二进制文件，包含 32 字节头（`TvmHeader`）+ DFA 表 + 可选字面量。**不包含**任何警告字符串、AST 或捕获组名称。

## 无 CLI 原则

编译器不以独立可执行形式存在。它由 MSBuild Task 或 Source Generator 隐式调用：

- 开发者创建 `.tvm.pattern` 文件
- `dotnet build` 时 `TvmCompileTask` 自动编译并嵌入
- 运行时通过静态 `GeneratedPatterns` 类访问编译产物

## 相关文档

- [Nyar.VM.TextVM](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.TextVM/readme.md)
- [Spec: implement-textvm-regex-engine](file:///e:/RiderProjects/.trae/specs/implement-textvm-regex-engine/spec.md)
