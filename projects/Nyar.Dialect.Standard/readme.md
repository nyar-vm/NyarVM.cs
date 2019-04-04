# Nyar.Dialect.Standard

## 概述

Standard 方言是 NyarVM 所使用的标准语言，是 Core 方言的直接扩展，提供所有高级语言共需的基础类型和操作。

## 特殊性

Standard 方言在 Nyar 生态系统中具有特殊地位：

- **NyarVM 的标准语言**：Standard 方言是 Nyar 参考虚拟机的原生语言，所有在 NyarVM 上运行的代码最终都需要通过 Standard
  方言来表达
- **预注册符号**：Standard 方言定义了一系列外部可调用符号，这些符号在 VM 启动时预注册，无需额外加载
- **多语言运行的基础**：虽然 Nyar 作为元编译器框架不绑定任何 VM，但 NyarVM 作为参考实现，所有其他语言（Python、JavaScript、Rust
  等）都可以编译到 Standard 方言，然后在 NyarVM 上运行

## 核心功能

### 基础类型与操作

- 数值转换（int8/16/32/64、uint8/16/32/64、float32/64）
- 位操作（and、or、xor、shl、lshr、ashr）
- 布尔逻辑（bool_and、bool_or）
- UTF-8 文本操作（concat、length、substring、compare）
- 数组与切片操作
- 指针运算
- 结构体、枚举、联合声明与操作

### 宿主互操作

Standard 不把宿主能力伪装成普通方言节点。控制台、文件系统、网络、计时器等能力应通过 `IKun.Import` 加 `IKun.Apply`
表达，由目标运行时或后端将其绑定到具体宿主符号。

NyarVM / 目标后端可以预注册一组标准宿主符号，例如：

- **Console**：标准输入输出
- **FileSystem**：文件系统操作
- **Network**：网络操作
- **Timer**：定时器
- **Random**：随机数
- **Environment**：环境变量与命令行参数
- **Process**：进程管理
- **Eval**：动态编译执行
- **Debug**：调试功能

这些名称描述的是可绑定的宿主能力域，不意味着 `Standard` 应该直接声明 `Write`、`Print`、`ReadLine` 这类伪 FFI 节点。

## 与 Nyar 框架的关系

Nyar 是一个元编译器框架，本身不绑定任何 VM。而 Standard 方言是：

1. **NyarVM 的原生语言**：为 NyarVM 提供了一套完整的标准库和类型系统
2. **其他语言的编译目标**：所有通过 Nyar 元编译器处理的语言，都可以编译到 Standard 方言，然后在 NyarVM 上运行
3. **自举的基础**：Standard 方言足够强大，可以用来编写 Nyar 元编译器和 NyarVM 本身

## 方言 ID

Standard 方言的方言 ID 为 1。
