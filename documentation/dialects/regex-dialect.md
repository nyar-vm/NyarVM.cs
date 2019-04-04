# Regex 方言

## 概述

Regex 方言将正则表达式匹配提升为可优化的意图表示。性能最好的正则实现本质上是专用虚拟机，Nyar 通过方言机制统一了不同后端的选择。

## 节点定义

### HIR 层

| 节点 | 描述 | 示例 |
|------|------|------|
| Regex | 正则表达式字面量意图 | `(Regex "a(b|c)*d")` |
| Match | 对字符串执行匹配 | `(Match regex str)` |

### MIR 层

| 节点 | 描述 | 示例 |
|------|------|------|
| Alt, Seq, Star, Plus, Opt | 正则 AST 节点 | `(Star (Alt (Char 'a') (Char 'b')))` |
| Char, Class, Capture | 字符与捕获 | `(Class "ab")` |
| Compile | 编译为可执行匹配器 | `(Compile ast backend)` |

### LIR 层

| 节点 | 描述 |
|------|------|
| NFA | NFA 状态转移表 |
| DFA | DFA 跳转表 |
| JITCode | JIT 编译后的机器码 |
| VMBytecode | 专用虚拟机字节码 |

## 等价规则

| 规则 | 模式 | 重写 | 说明 |
|------|------|------|------|
| 字符类合并 | `(Alt (Char 'a') (Char 'b'))` | `(Class "ab")` | 缩小状态空间 |
| 星号分布 | `(Seq (Star (Alt a b)) c)` | `(Star (Alt (Seq a c) (Seq b c)))` | 为 DFA 化准备 |
| 锚点传播 | `(Seq ^ pattern)` | 优化前缀匹配 | 加速 |
| 后端选择 | `(Compile ast)` | 根据成本选择后端 | DFA vs VM |

## 后端策略

| 后端 | 适用场景 | 特点 |
|------|----------|------|
| DFA | 模式简单、长文本反复匹配 | 编译时间长，内存占用大 |
| NFA 模拟 | 复杂模式（捕获组、环视） | 基于栈的 Thompson NFA |
| JIT 编译 | 热路径 | 类似 PCRE2 JIT |
| VM 字节码 | 通用默认 | RE2、Rust regex 风格，平衡性能与内存 |

## 与 Nyar VM 的协同

正则匹配过程中的回溯/挂起可通过代数效应表达，允许 Nyar VM 在执行正则时协作调度其他协程，避免阻塞。

Nyar VM 内置了一个高效的正则 VM 执行器，可直接解释该字节码。由于 Nyar VM 本身就是元虚拟机，正则 VM 可以与其共享基础设施（如 JIT 编译器、GC），实现深度优化（例如将正则匹配的 JIT 代码与周围的业务逻辑一起内联）。
