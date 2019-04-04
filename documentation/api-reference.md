# API 参考

> 本文档描述 Nyar OA 架构的 API。所有接口均基于对象代数模式。

## 方言定义 API

### 方言标记

```csharp

/// <summary>

/// 标记一个接口为 Nyar 方言。

/// Source Generator 将自动生成对应的 OA 工厂接口和 Builder 实现。

/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public class DialectAttribute : Attribute
{
    public string Name { get; }

    public DialectAttribute(string name)
    {
        Name = name;
    }
}


/// <summary>

/// 标记方言接口中的一个操作（语言构造）。

/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OpAttribute : Attribute
{
    public string Name { get; }

    public OpAttribute(string name)
    {
        Name = name;
    }
}
```

### 方言定义示例

```csharp
[Dialect("core")]
public partial interface ICore
{
    [Op("i32")] Expr<int> Int32(int value);
    [Op("add")] Expr<int> Add(Expr<int> left, Expr<int> right);
    [Op("mul")] Expr<int> Mul(Expr<int> left, Expr<int> right);
}
```

Source Generator 自动生成：
- `ICoreAlg<E>` —— 泛型 OA 工厂接口
- `CorePsiBuilder` —— IDE PSI 构造器
- `CoreEgraphBuilder` —— EGraph 优化器前端
- `CorePEBuilder` —— 部分求值器前端
- `CoreInterpreter` —— 解释器骨架

## OA 工厂接口（自动生成）

```csharp

/// <summary>

/// Core 方言的 OA 工厂接口。

/// E 为表示类型：不同工厂实现绑定不同的 E。

/// </summary>
public interface ICoreAlg<E>
{
    E Int32(int value);
    E Add(E left, E right);
    E Mul(E left, E right);
}
```

## Nyar.Core

### EGraph

```csharp

/// <summary>

/// EGraph 饱和优化引擎。

/// </summary>
public class EGraph
{
    
/// <summary>添加节点，返回其等价类标识符。</summary>
    public EClass Add(ENode node);

    
/// <summary>合并两个等价类。</summary>
    public void Union(EClass a, EClass b);

    
/// <summary>应用规则直到不动点。</summary>
    public void Saturate(IReadOnlyList<IRewriteRule> rules);

    
/// <summary>从等价类提取成本最低的表示。</summary>
    public ENode Extract(EClass root, ICostModel costModel);
}


/// <summary>等价类标识符。</summary>
public readonly record struct EClass(int Id);


/// <summary>EGraph 节点：操作码 + 子节点等价类列表。</summary>
public readonly record struct ENode(OpCode Op, params EClass[] Children);
```

### 重写规则

```csharp

/// <summary>

/// 重写规则接口（OA 风格）。

/// </summary>
public interface IRewriteRule<Alg> where Alg : class
{
    
/// <summary>向引擎注册规则。</summary>
    void Register(RewriteEngine<Alg> engine);
}


/// <summary>

/// 重写规则引擎。

/// </summary>
public class RewriteEngine<Alg> where Alg : class
{
    
/// <summary>注册一条规则的模式侧。</summary>
    public RuleBuilder Rule(Expression<Func<Alg, EClassId>> pattern);

    
/// <summary>创建一个模式变量，匹配任意同类型节点。</summary>
    public EClassId Any<T>();
}


/// <summary>

/// 规则构建器，指定替换侧。

/// </summary>
public class RuleBuilder
{
    
/// <summary>指定规则的替换侧。</summary>
    public void Replace(Expression<Func<..., EClassId>> replacement);
}
```

### 成本模型

```csharp

/// <summary>

/// 成本模型接口。

/// </summary>
public interface ICostModel
{
    
/// <summary>评估节点的成本。</summary>
    CostVector Evaluate(ENode node, Context ctx);

    
/// <summary>优化目标。</summary>
    OptimizationGoal Goal { get; }

    
/// <summary>约束条件（如空间上限）。</summary>
    IReadOnlyList<IConstraint> Constraints { get; }
}


/// <summary>

/// 多维成本向量：时间、空间、能量等。

/// </summary>
public readonly record struct CostVector(
    double Time,
    double Space,
    double Energy,
    double Bandwidth
);


/// <summary>

/// 优化目标。

/// </summary>
public enum OptimizationGoal
{
    MinimizeLatency,
    MinimizeSpace,
    MinimizeEnergy,
    MaximizeThroughput
}
```

## Nyar.Optimizer

```csharp

/// <summary>

/// Nyar 优化器：OA 编译管线的编排者。

/// </summary>
public class NyarOptimizer
{
    
/// <summary>使用指定成本模型优化 OA 程序函数。</summary>
    public TOutput Optimize<TAlg, TOutput>(
        Func<TAlg, TOutput> program,
        ICostModel costModel,
        IReadOnlyList<IRewriteRule> rules
    ) where TAlg : class;

    
/// <summary>执行部分求值。</summary>
    public Func<TAlg, (bool, object, object?)> Specialize<TAlg>(
        Func<TAlg, object> program
    ) where TAlg : class;
}
```

## Nyar.VM（NyarVM 后端）

```csharp

/// <summary>

/// NyarVM 运行时。

/// </summary>
public class NyarVM
{
    
/// <summary>加载 .nyar 模块。</summary>
    public void LoadModule(string path);

    
/// <summary>注册效应处理器。</summary>
    public void RegisterEffectHandler(EffectType type, IEffectHandler handler);

    
/// <summary>执行入口函数。</summary>
    public Value Execute(string entryPoint, params Value[] args);

    
/// <summary>热替换模块。</summary>
    public void HotReload(string modulePath);
}


/// <summary>

/// JIT 编译器接口。

/// </summary>
public interface IJitCompiler
{
    CompiledFunction Compile(EGraph egraph, ProfilingData data);
    void Install(CompiledFunction func);
}
```

## Nyar.Backends

```csharp

/// <summary>

/// 代码生成后端接口（OA 工厂，E = 目标平台数据结构）。

/// </summary>
public interface IBackend<in TInput, out TOutput>
{
    
/// <summary>目标平台三元组。</summary>
    string TargetTriple { get; }

    
/// <summary>将优化后的表示编码为目标格式。</summary>
    TOutput Encode(TInput input);
}


/// <summary>NyarVM 后端。</summary>
public class NyarVMBackend : IBackend<EGraph, NyarModuleData> { ... }


/// <summary>JVM 后端。</summary>
public class JvmBackend : IBackend<EGraph, JvmClassFileData> { ... }


/// <summary>WASM 后端。</summary>
public class WasmBackend : IBackend<EGraph, WasmModuleData> { ... }


/// <summary>Native 后端。</summary>
public class NativeBackend : IBackend<EGraph, byte[]> { ... }
```

## PSI / IDE API

```csharp

/// <summary>

/// PSI 元素基类（自动生成）。

/// </summary>
public abstract class PsiElement
{
    public TextSpan Span { get; }
    public PsiElement Parent { get; }
    public IReadOnlyList<PsiElement> Children { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}


/// <summary>

/// PSI Builder 接口（OA 工厂，E = 具体 Psi 类型）。

/// </summary>
public interface IPsiBuilder<E> where E : PsiElement
{
    // 方言操作映射为 PsiElement 构造方法
}

---

## Nyar.Intelligence API

Nyar.Intelligence 提供纯语法解析基础设施的核心 API。

### ISource — 只读源文本

```csharp
public interface ISource
{
    char this[int index] { get; }
    int Length { get; }
    string Substring(Range range);
}
```

### TextSpan — 纯偏移范围

```csharp
public readonly struct TextSpan : IEquatable<TextSpan>
{
    public int Start { get; }
    public int Length { get; }
    public int End { get; }
    public bool Contains(int position);
    public bool OverlapsWith(TextSpan other);
}
```

### GreenNode — 不可变语法节点

```csharp
public abstract class GreenNode
{
    public abstract NodeKind Kind { get; }
    public abstract int Width { get; }
    public abstract int ChildCount { get; }
    public bool IsLeaf { get; }
    public abstract GreenNode? GetChild(int index);
    public IEnumerable<GreenNode> Children { get; }
}
```

子类：`GreenInternalNode`（内部节点）、`GreenLeafNode`（叶子节点）。

### RedNode — 带绝对位置的视图

```csharp
public readonly struct RedNode
{
    public NodeKind Kind { get; }
    public TextSpan Span { get; }
    public int ChildCount { get; }
    public bool IsLeaf { get; }
    public RedNode? Parent { get; }
    public RedNode GetChild(int index);
    public IEnumerable<RedNode> Children { get; }
    public IEnumerable<RedNode> Descendants();
    public IEnumerable<RedNode> Ancestors();
}
```

### SyntaxTree — 语法树

```csharp
public class SyntaxTree
{
    public ISource Source { get; }
    public GreenNode Root { get; }
    public SyntaxRoot? PrimaryRoot { get; }
    public IReadOnlyList<SyntaxRoot> AllRoots { get; }

    public RedNode GetRedRoot();
    public SyntaxRoot? GetRoot(string languageId);
    public SyntaxTree Edit(Edit edit, IncrementalParserRepo parsers);

    public event Action<TreeChangeEvent>? Changed;
}
```

### CstBuilder — Green 树构建器

```csharp
public ref struct CstBuilder
{
    public void StartNode(NodeKind kind);
    public void EndNode();
    public void AddToken(NodeKind kind, TextSpan sourceRange);
    public void AddChild(GreenNode node);
    public GreenNode Build();
}
```

### ParseContext — 解析上下文

```csharp
public ref struct ParseContext<TLanguage, TContext>
    where TLanguage : Language
    where TContext : ISyntaxContext
{
    public ISource Source { get; }
    public int Position { get; set; }
    public TLanguage Language { get; }
    public TContext Context { get; }
    public DiagnosticSink Diagnostics { get; }

    public char Current { get; }
    public void Advance();
    public TextSpan GetSpanFrom(int startPosition);
    public string GetText(TextSpan span);
}
```

### LanguageRegistry — 语言注册表

```csharp
public static class LanguageRegistry
{
    public static void Register(string languageId, Language language,
        Func<ISource, SyntaxRoot> parser);
    public static SyntaxRoot Parse(string languageId, ISource source);
    public static Language? GetLanguage(string languageId);
    public static bool IsRegistered(string languageId);
}
```

> 更多 API 细节参见 [数据模型](./core-systems/data-model.md) 和 [解析器辅助](./core-systems/parsing.md)。