# Standard 方言

## 概述

Standard 方言是 Core 方言的直接扩展，提供所有高级语言共需的基础类型和操作。其方言 ID 为 1。

Standard 方言不引入新的 Core 级语义原子，而是作为内建函数和类型构造器存在。它定义了一系列外部可调用符号，这些符号在 VM 启动时预注册。

## 新增节点

### 数值转换

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| int8, int16, int32, int64, uint8, uint16, uint32, uint64, float32, float64 | (Any) -> T | 强制类型转换 |
| trunc, zext, sext, fptrunc, fpext, fptoui, fptosi, uitofp, sitofp | 精确转换 | 按 LLVM 语义 |

### 位操作

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| and, or, xor, shl, lshr, ashr | (T, T) -> T | 整数位运算 |

### 布尔逻辑

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| bool_and, bool_or | (Bool, Bool) -> Bool | 短路求值由前端保证 |

### UTF-8 文本操作

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| utf8_concat | (utf8, utf8) -> utf8 | UTF-8 文本拼接 |
| utf8_length | (utf8) -> I32 | UTF-8 文本长度 |
| utf8_substring | (utf8, I32, I32) -> utf8 | UTF-8 文本子串提取 |
| utf8_compare | (utf8, utf8) -> I32 | UTF-8 文本比较 |
| utf8_from_utf8, utf8_to_utf8 | | 编解码 |

### 数组/切片

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| array_new | (I32, T) -> Array<T> | 定长数组 |
| array_length | (Array<T>) -> I32 | 数组长度 |
| array_get, array_set | | 读写元素 |
| slice_new | (Ptr<T>, I32) -> Slice<T> | 切片（不拥有内存） |
| slice_length, slice_get, slice_set | | 切片操作 |

### 指针运算

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| ptr_offset | (Ptr<T>, I32) -> Ptr<T> | 带类型大小的偏移 |
| ptr_diff | (Ptr<T>, Ptr<T>) -> I32 | 指针差值 |
| ptr_load, ptr_store | | 直接内存访问（同 Core Load/Store，但携带类型信息） |

### 结构体/枚举

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| struct_declare | 元操作，声明结构布局 | 通过 Witness Table 实现 |
| struct_new | (fields...) -> Struct | 结构体构造 |
| struct_get, struct_set | | 字段访问 |
| enum_declare, union_declare | | 枚举/联合声明 |
| enum_inject, enum_project | | 构造与解构 |

### 控制流辅助

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| assert | (Bool) -> Unit | 触发 AssertionFailure 效应 |
| unreachable | () -> Never | 触发 UB 效应 |

### 并发原语

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| atomic_load, atomic_store, atomic_cas, atomic_rmw | 带内存顺序参数 | 可映射到 Core Load/Store + 内存顺序 |

### 内省/反射

| 符号 | 类型签名 | 说明 |
|------|----------|------|
| type_of | (Any) -> TypeInfo | 返回类型元数据 |
| sizeof, alignof | (Type) -> I32 | 编译时求值 |

## 标准效应

Standard 方言定义了一组预定义效应，所有 Nyar 程序均可使用。VM 提供默认处理器，也可被用户覆盖。

| 效应类型 | 操作 | 参数 | 返回 | 默认处理器行为 |
|----------|------|------|------|----------------|
| Console | Write | (String) | () | 写入 stdout |
| | ReadLine | () | String | 从 stdin 读取一行 |
| FileSystem | Open | (String, Mode) | Handle | 打开文件句柄 |
| | Read | (Handle, I32) | Slice<U8> | 读取字节 |
| | Write | (Handle, Slice<U8>) | I32 | 写入字节 |
| | Close | (Handle) | () | 关闭文件 |
| | Delete | (String) | Bool | 删除文件 |
| Network | TcpConnect | (String, U16) | Handle | TCP 连接 |
| | TcpRead, TcpWrite | | | TCP 读写 |
| | HttpRequest | (Method, Url, Headers, Body) | Response | 默认通过系统 HTTP 客户端 |
| Timer | Sleep | (Duration) | () | 挂起当前协程 |
| | Now | () | Timestamp | 当前时间 |
| Random | Seed | (U64) | () | 初始化 PRNG |
| | Next | () | U64 | 生成随机数 |
| Environment | GetEnv | (String) | Option<String> | 获取环境变量 |
| | GetArgs | () | Slice<String> | 命令行参数 |
| Process | Exit | (I32) | Never | 退出程序 |
| | Spawn | (String, Slice<String>) | Handle | 创建子进程 |
| Eval | Eval | (String) | Any | 动态编译执行代码（需安全沙箱） |
| Debug | Break | () | () | 触发调试器断点 |
| | Trace | (String) | () | 输出调试信息 |

## 标准类型 Witness Table

Standard 方言为基本类型提供预定义的 Witness Table，使得动态语言特性（如反射、接口调用）成为可能。

```csharp
// 伪代码示意
TypeInfo {
    name: "String",
    size: 16,  // 指针+长度
    align: 8,
    methods: {
        "concat": fn(ptr, ptr) -> ptr,
        "length": fn(ptr) -> i32,
        ...
    },
    interfaces: ["Show", "Eq", "Ord", "Hash"]
}
```

## 与 Core 的协作

所有 Standard 符号最终都降级为 Core 节点序列。例如 `utf8_concat` 降级为 Alloc + 循环 Load/Store + Call 内存复制函数。

类型转换节点在 Core 层面仅为 Literal 携带不同 TypeAnnotation，实际转换由后端或运行时库完成。

Standard 效应映射到 Perform 节点，其效应类型 ID 在 Standard 方言注册表分配。
