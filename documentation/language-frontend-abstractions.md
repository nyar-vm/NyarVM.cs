# 语言前端抽象层设计

## 概述

Nyar 的 OA 核心负责编译优化和代码生成，但源语言的**文本到 OA 程序函数**的转换同样关键。本文档定义语言前端的抽象基础设施。

这些抽象与 OA 方言接口的对应关系是：**语言前端将源码解析为对 OA 方言工厂的调用序列**。不同语言的 Parser 调用同一套 `IDialectAlg<E>` 接口，从而实现前端与后端的彻底解耦。

## 语言前端管线

```
源代码
   │
   ▼  ┌──────────────────┐
   │  │ 词法分析          │  ILexer: 字符流 → Token 流
   │  └────────┬─────────┘
   │           ▼
   │  ┌──────────────────┐
   │  │ 语法分析          │  IParser: Token 流 → AST
   │  └────────┬─────────┘
   │           ▼
   │  ┌──────────────────┐
   │  │ 语义分析          │  ISemanticAnalyzer
   │  └────────┬─────────┘
   │           ▼
   │  ┌──────────────────┐
   │  │ AST → OA 程序函数 │  IAstToOAConverter
   │  └────────┬─────────┘
   │           ▼
  OA 程序函数: static E P<IDialectAlg<E>>(IDialectAlg<E> alg) => ...
```

## 核心抽象

### 词法分析接口

```csharp

/// <summary>

/// 词法分析器接口：将源码字符流转换为 Token 流

/// </summary>
public interface ILexer
{
    
/// <summary>
    
/// 对源码执行词法分析
    
/// </summary>
    IEnumerable<Token> Lex(string source);
}


/// <summary>

/// 词法分析上下文

/// </summary>
public interface ILexerContext
{
    
/// <summary>
    
/// 当前源码
    
/// </summary>
    string Source { get; }

    
/// <summary>
    
/// 当前扫描位置
    
/// </summary>
    int Position { get; }

    
/// <summary>
    
/// 错误恢复策略
    
/// </summary>
    ILexerErrorRecovery ErrorRecovery { get; }
}


/// <summary>

/// 词法错误恢复策略

/// </summary>
public interface ILexerErrorRecovery
{
    
/// <summary>
    
/// 遇到非法字符时的处理
    
/// </summary>
    Token HandleInvalidChar(ILexerContext context);
}
```

### 语法分析接口

```csharp

/// <summary>

/// 语法分析器接口：将 Token 流转换为 AST

/// </summary>
public interface IParser
{
    
/// <summary>
    
/// 解析 Token 流为 AST
    
/// </summary>
    ICompilationUnit Parse(IEnumerable<Token> tokens);

    
/// <summary>
    
/// 解析 Token 流为 AST（带错误恢复）
    
/// </summary>
    ParseResult ParseWithErrors(IEnumerable<Token> tokens);
}


/// <summary>

/// 解析结果（包含 AST 或错误）

/// </summary>
public sealed class ParseResult
{
    
/// <summary>
    
/// 解析成功的 AST
    
/// </summary>
    public ICompilationUnit Ast { get; init; }

    
/// <summary>
    
/// 解析过程中收集的诊断信息
    
/// </summary>
    public IReadOnlyList<IDiagnostic> Diagnostics { get; init; }
}


/// <summary>

/// 语法错误恢复策略

/// </summary>
public enum ErrorRecoveryStrategy
{
    
/// <summary>Panic 模式：跳过直到找到同步 Token</summary>
    Panic,

    
/// <summary>跳过单个 Token</summary>
    Skip,

    
/// <summary>插入缺失 Token</summary>
    Insert,

    
/// <summary>合并多个错误尝试的 AST</summary>
    Merge,
}
```

### 语义分析接口

```csharp

/// <summary>

/// 语义分析器接口：对 AST 执行名称解析、类型检查等

/// </summary>
public interface ISemanticAnalyzer
{
    
/// <summary>
    
/// 分析 AST 的语义
    
/// </summary>
    ISemanticModel Analyze(ICompilationUnit ast);
}


/// <summary>

/// 语义模型：包含符号表、类型信息、诊断

/// </summary>
public interface ISemanticModel
{
    
/// <summary>
    
/// 获取指定 AST 节点的语义信息
    
/// </summary>
    ISymbol GetSymbol(IAstNode node);

    
/// <summary>
    
/// 获取指定 AST 节点的类型
    
/// </summary>
    IType GetType(IAstNode node);
}
```

### AST → OA 转换器

```csharp

/// <summary>

/// AST 到 OA 程序函数的转换器

/// </summary>

/// <typeparam name="TAlg">目标方言 OA 接口</typeparam>
public interface IASTToOAConverter<in TAlg>
{
    
/// <summary>
    
/// 将 AST 编译单元转换为 OA 程序函数
    
/// </summary>
    Func<TAlg, object> Convert(ICompilationUnit ast);
}
```

转换器对每个语言构造调用相应的 OA 方言方法：

```csharp
// 源语言: 1 + 2 * 3
// 转换为:
alg => alg.Add(alg.Lit(1), alg.Mul(alg.Lit(2), alg.Lit(3)))
```

转换器输出的程序函数不绑定任何具体表示，可以传递给任意工厂实现。

## AST 基类体系

### 绿色/红色树架构

借鉴旧版的 Green/Red Tree 设计，将 AST 节点分为持久化层（Green）和附加值层（Red）：

```csharp

/// <summary>

/// 绿色节点：不可变、无父引用、可以共享

/// </summary>
public abstract class GreenNode
{
    
/// <summary>
    
/// 语法种类
    
/// </summary>
    public int Kind { get; }

    
/// <summary>
    
/// 文本跨度长度
    
/// </summary>
    public int FullWidth { get; }

    
/// <summary>
    
/// 子节点列表
    
/// </summary>
    public IReadOnlyList<GreenNode> Children { get; }
}


/// <summary>

/// 红色节点：可附加父引用、PSI 元素、诊断信息

/// </summary>
public abstract class RedNode
{
    
/// <summary>
    
/// 绿色节点（底层数据）
    
/// </summary>
    public GreenNode Green { get; }

    
/// <summary>
    
/// 父节点
    
/// </summary>
    public RedNode Parent { get; }

    
/// <summary>
    
/// 在父节点中的索引
    
/// </summary>
    public int IndexInParent { get; }

    
/// <summary>
    
/// 文本跨度
    
/// </summary>
    public TextSpan Span { get; }

    
/// <summary>
    
/// 获取指定类型的子节点
    
/// </summary>
    public TChild GetChild<TChild>() where TChild : RedNode;

    
/// <summary>
    
/// 获取所有指定类型的后代节点
    
/// </summary>
    public IEnumerable<TChild> GetDescendants<TChild>() where TChild : RedNode;
}
```

Green Tree 和 Red Tree 分离的核心优势：
- **内存共享**：多个 RedNode 可以引用同一个 GreenNode（增量更新时）
- **事务性修改**：修改 = 创建新的 GreenNode 子树 + 复用未变的兄弟节点
- **DIFF 天然支持**：GreenNode 的不可变性使比较变得简单

## 错误恢复策略

对于任意语言的 Parser，错误恢复策略是保证可用性的关键。四种策略的分工：

| 策略 | 适用场景 | 机制 |
|:---|:---|:---|
| **Panic** | 语法严重错误，无法继续解析当前构造 | 跳到下一个同步 Token（`;` `}` `EOF`） |
| **Skip** | 单个非法 Token | 跳过并继续解析 |
| **Insert** | 缺失必要的 Token | 插入虚拟 Token，继续解析 |
| **Merge** | 歧义语法 | 并行尝试多种解析路径，选择最优 |

P0 优先级：Panic > Skip = Insert > Merge

## 与 OA 方言的关系

语言前端产出的 OA 程序函数遵循以下约定：

1. **一个源文件 = 一个模块**：每个源文件生成一个返回 `IModule` 的程序函数
2. **模块方法映射到 OA 方言方法**：源语言的函数/方法/运算符映射到对应的 OA `Op`
3. **类型转换为方言标记**：源语言的类型系统转换为方言的类型参数

```
源语言                              OA 程序函数
────────────────────────────────────────────────────
fn add(x: int, y: int) -> int {    static E AddModule<IMyAlg<E>>(IMyAlg<E> alg) =>
    x + y                              alg.DefineFunction("add",
}                                          alg.FuncParams(alg.IntType, alg.IntType),
                                           body => alg.Add(body.Param(0), body.Param(1)));
```

转换器只产生程序函数的**调用结构**，不产生任何具体数据表示。所有的优化、求值、代码生成都发生在接收特定工厂实例之后。

## 并发与增量处理

- **Lexer** 天然可分段并行（通过 `TextSpan` 分区）
- **Parser** 在函数/类级别可并行（先解析签名，后解析体）
- **语义分析** 的绑定阶段需要全局顺序，但类型检查可以并行
- **OA 程序函数** 本身是无副作用的纯函数，天然支持并发执行