# 代数效应

## 定位：NyarVM 运行时的控制流机制

代数效应是 **NyarVM** 的统一控制流抽象，属于 NyarVM 运行时的特性，而非 Nyar 框架的通用能力。其他后端（JVM、WASM、Native）使用各自平台的控制流机制。

## 为什么需要代数效应

传统语言将控制流原语（异常、协程、异步）内置到编译器中，导致语言设计者难以扩展。代数效应将控制流抽象为**可组合的、用户可定义的**操作。

在 NyarVM 中，效应使得意图表示能够保留高层控制流语义，而不是立即降低到跳转和调用。优化器可以跨效应边界进行重排（例如将两个连续的 `perform AsyncRead` 合并为批量读取）。

## 效应如何实现协程与异步

```
// 意图表示
Intent Perform { effect: AsyncRead, args: [url] }

// 运行时处理器
handler = {
    on AsyncRead(url) -> 
        // 挂起当前协程，保存状态，注册回调
        // 当数据到达时，恢复协程
}
```

这种机制允许 NyarVM 实现百万级协程，每个协程只占用少量内存（保存栈帧和寄存器）。

## 优化效应

传统观点认为效应会阻碍优化。但 NyarVM 通过**效应分析**（判断效应是否是纯的、可交换的）和**重写规则**（如 `(Perform e1; Perform e2)` 与 `(Perform e2; Perform e1)` 在 e1 和 e2 无依赖时等价），实现效应重排和合并。

## 标准效应

Standard 方言定义了一组预定义效应，所有 NyarVM 程序均可使用：

| 效应类型 | 操作 | 参数 | 返回 | 默认处理器行为 |
|:---|:---|:---|:---|:---|
| Console | Write | (String) | () | 写入 stdout |
| | ReadLine | () | String | 从 stdin 读取一行 |
| FileSystem | Open | (String, Mode) | Handle | 打开文件句柄 |
| | Read | (Handle, I32) | Slice<U8> | 读取字节 |
| | Write | (Handle, Slice<U8>) | I32 | 写入字节 |
| Network | TcpConnect | (String, U16) | Handle | TCP 连接 |
| | HttpRequest | (Method, Url, Headers, Body) | Response | HTTP 请求 |
| Timer | Sleep | (Duration) | () | 挂起当前协程 |
| Random | Next | () | U64 | 生成随机数 |
| Eval | Eval | (String) | Any | 动态编译执行代码 |

## 效应与非常规语言特性

代数效应是驯服非常规语言特性的核心机制：

| 特性 | 效应类型 | 处理方式 |
|:---|:---|:---|
| 异常 | Throw | 处理器捕获并恢复 |
| 协程 | Yield | 处理器挂起并恢复 |
| 异步 I/O | AsyncRead/Write | 调度到事件循环 |
| call/cc | CaptureCC | 栈具体化 |
| 逻辑变量 | ChoicePoint | 回溯栈维护 |
| 电子表格 | CellRead/Write | 依赖图拓扑处理器 |

> 注意：以上特性仅在 NyarVM 后端可用。其他后端需要通过目标平台的机制模拟。
