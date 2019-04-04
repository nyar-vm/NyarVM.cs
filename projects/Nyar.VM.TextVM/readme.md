# Nyar.VM.TextVM

## 概述

`Nyar.VM.TextVM` 是一个**多编码正则引擎运行时**。它在 ASCII、UTF-8、UTF-16LE、UTF-16BE 四种编码的字节切片上直接工作，无需转码。

核心设计原则是**静态已知性**：因为查询模式（Q）和编码（E）在编译期完全已知，TextVM 可以在编译期通过四维静态分析将不同复杂度的任务路由到最优算法——使纯字面量搜索等价于手写 BM，同时又支持布尔正则（交 &、并 |、补 !）和 DFA 级保证的线性时间匹配。

## 核心能力

- **多编码码点迭代器**：`CodePointIter` 将任意编码字节切片统一抽象为 `(byteLen, char)` 码点流，处理无效字节序列时使用 U+FFFD 替换策略保持字节偏移对齐
- **编码类型**：`TextEncoding.Ascii` / `Utf8` / `Utf16Le` / `Utf16Be`
- **匹配类型**：`TvmOperation.Exists` / `Find` / `Replace`
- **零拷贝结果**：`Match` 返回字节偏移量，直接指向原始输入切片

## 运行时入口

```csharp
using Nyar.VM.TextVM;

// 加载 .tvm 字节码并执行
Byte[] engine = File.ReadAllBytes("rules/schema.tvm");
Byte[] input = Encoding.UTF8.GetBytes("Hello ERROR 404");

Boolean found = Tvm.IsMatch(engine, input);
Match? first = Tvm.Find(engine, input);
IEnumerable<Match> all = Tvm.FindAll(engine, input);
Byte[] replaced = Tvm.Replace(engine, input, "[REDACTED]"u8);
```

> **注意**：运行时不接受 pattern 字符串。所有模式必须通过编译工具链预编译为 `.tvm` 字节码。见 [Nyar.VM.TextVM.Compiler](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.TextVM.Compiler/readme.md)。

## 架构分层

### 1. 编码抽象层（CodePointIter）

将字节切片抽象为码点迭代器，产出 `(byteLen, char)` 元组，字节偏移始终对齐。

| 编码 | 解码方式 | 无效序列 |
|------|----------|----------|
| ASCII | 单字节，≤ 0x7F | `(1, '\uFFFD')` |
| UTF-8 | `Rune.DecodeFromUtf8` | `(1, '\uFFFD')` |
| UTF-16LE | `lo \| (hi << 8)` | `(2, '\uFFFD')` |
| UTF-16BE | `(lo << 8) \| hi` | `(2, '\uFFFD')` |
| 代理对 | 高 + 低 → `(4, char)` | 孤立代理 `(2, '\uFFFD')` |

### 2. 执行策略层（CompiledUnit）

五种执行策略，由编译器前端通过静态分析路由选择：

| 策略 | 适用场景 | 算法 |
|------|----------|------|
| `LiteralExecutor` | 纯字面量搜索 | Two-Way BM |
| `DfaExecutor` | 带通配符/字符类的正则 | DFA 状态机 |
| `BitParallelNfaExecutor` | 状态数受限的复杂模式 | Glushkov 位并行 |
| `SimdScanExecutor` | 纯字符类扫描 | SIMD 位掩码（架构预留） |
| `BacktrackExecutor` | 带反向引用的模式 | 回溯 + CancellationToken（架构预留） |

### 3. 查询接口层（StaticQuery / DynamicQuery）

- **静态查询** `StaticQuery.Compile()`：构造时完成全量分析、DFA 构建、序列化。运行时无锁、无查表。
- **动态查询** `TextVM.Execute()`：三级路由（LRU 缓存 → 后台投机结果 → 同步降级），利用用户按键间隙投机编译。
- **后台编译器** `BackgroundCompiler`：支持撤销令牌机制，新按键使旧令牌失效，丢弃半成品。

## .tvm 产物格式

`.tvm` 是纯执行数据格式，**不包含任何警告字符串、AST 或捕获组名称**。

```
偏移  大小  字段
0     4     Magic "TVM\0"
4     4     Version (uint32)
8     1     Encoding (0=ASCII,1=UTF8,2=UTF16LE,3=UTF16BE)
9     1     Operation (0=None,1=Exists,2=Find,3=Replace)
10    2     Flags (IsLiteral=1, HasPrefix=2, HasCapture=4, IsNfa=8)
12    4     MinMatchLen (uint32)
16    4     LiteralPrefixOffset (uint32)
20    4     DfaTableOffset (uint32)
24    4     DfaStateCount (uint32)
28    4     TotalSize (uint32)
```

## 性能特性

- **纯字面量场景**：短路到 Two-Way BM，与手写 `IndexOf` 等价
- **UTF-16 场景**：直接操作 `ushort[]`，省去转码 UTF-8 的 O(N) 开销
- **字符类场景**：DFA / 位并行 NFA，保证 O(N) 线性时间
- **布尔正则场景**：编译期常量折叠消除运行时逻辑指令

## 与 Nyar.VM.NyarVM 的关系

`TextVM` 是独立于 `NyarVM` 的专用文本处理运行时，不依赖 Nyar 的 dialect/IR 体系，可独立用于任意需要多编码正则匹配的 C# 项目。两者是互补关系：

- `NyarVM` 负责通用 bytecode 执行
- `TextVM` 负责高效文本模式匹配与替换

## MSBuild 集成

参见 [Nyar.VM.TextVM.MsBuild](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.TextVM.MsBuild/TvmCompileTask.cs)。

在 `.csproj` 中添加 `.tvm.pattern` 文件即可在 `dotnet build` 时自动编译：

```xml
<ItemGroup>
    <TvmPattern Include="rules/**/*.tvm.pattern" />
</ItemGroup>
```

编译产物自动嵌入为 `EmbeddedResource`，通过 `GeneratedPatterns` 静态类访问。

## 相关文档

- [Nyar.VM.TextVM.Compiler](file:///e:/RiderProjects/NyarVM.cs/projects/Nyar.VM.TextVM.Compiler/readme.md)
- [Spec: implement-textvm-regex-engine](file:///e:/RiderProjects/.trae/specs/implement-textvm-regex-engine/spec.md)
