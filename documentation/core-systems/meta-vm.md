# 元虚拟机

## 定位：Nyar 的一个运行时后端

NyarVM 是 Nyar 框架的**一个运行时后端**，不是 Nyar 的全部。Nyar 的元优化能力（EGraph 饱和优化、部分求值、成本模型驱动选择、多目标代码生成）适用于**所有后端**，NyarVM 只是其中一个特别完整的选项。

```
Nyar 框架（OA 元编译器）：适用于所有后端
├── EGraph 饱和优化
├── 部分求值 + Futamura 投影
├── 成本模型驱动的最优选择
├── 多目标代码生成
│
└── 后端（OA 工厂实现，E = 目标表示）
    ├── NyarVM ← 最完整的运行时（本节描述）
    │   ├── JIT 编译器
    │   ├── 精确非移动 GC
    │   ├── 代数效应（统一控制流）
    │   ├── Witness Table（动态派发 + 热更新）
    │   └── 模块热加载
    ├── JVM 后端 (.class)
    ├── CLR 后端 (.dll)
    ├── WASM 后端 (.wasm)
    └── Native 后端 (x86/ARM/RISC-V)
```

## NyarVM 的设计目标

NyarVM 的目标是提供一个**功能完备的参考运行时**，展示 OA 架构如何落地为可执行的虚拟机。它提供了所有后端中最完整的运行时支持：

- **语言无关的对象模型**：基于 Witness Table，支持多继承、接口混合、动态添加方法
- **高效的并发抽象**：基于代数效应实现协程、异步、轻量级线程
- **自适应执行**：解释器收集 profiling，热点 JIT 编译，支持 OSR（栈上替换）
- **精确非移动 GC**：支持外部 FFI 持有裸指针，同时保持并发低暂停
- **模块热加载**：`.nyar` 文件可在运行时替换，Witness Table 使现有对象透明迁移

## 统一对象模型与 Witness Table

NyarVM 定义一种极简的通用对象模型：

- 对象头部：`{ Witness* witness, uint32_t hash, ... }`
- Witness 结构：`{ TypeInfo* type, InterfaceTable*[] interfaces, DispatchTable methods }`

接口调用通过 `obj->witness->interfaces[interface_id][method_idx]` 间接派发。Witness 指针可替换，支持热更新。

> 注意：Witness Table 是 NyarVM 运行时的特性，不是 Nyar 框架的通用能力。JVM 后端使用 vtable，WASM 后端使用间接函数表，Native 后端使用直接调用——每个后端有自己的派发机制。

## 代数效应：统一控制流

NyarVM 将代数效应作为一等原语，统一所有非平凡控制流：

- **异常**：`effect Throw<T>`，处理者捕获并恢复
- **协程**：`effect Yield<T>`，处理者挂起并恢复
- **异步 I/O**：`effect AsyncRead`，处理者调度到事件循环
- **轻量级线程**：`effect Fork`，处理者创建新的执行上下文

效应处理器的实现可以定制。例如浏览器中的 `AsyncRead` 调用 `fetch`，服务器端调用 `epoll`。

> 注意：代数效应是 NyarVM 运行时的控制流机制。在其他后端（如 WASM），控制流由目标平台的机制实现（如 WASM 的 `call_indirect`）。

## 精确非移动 GC

NyarVM 的 GC 选择不移动对象，以便于 FFI 和嵌入式场景：

- **空闲链表合并**相邻空闲块
- **分代式**：年轻代移动，老年代不移动
- **区域回收**：整个区域所有对象死亡则直接回收
- **并发标记-清理**：标记阶段与 mutator 并发

> 注意：GC 是 NyarVM 运行时的特性。其他后端（如 WASM）依赖宿主 GC，Native 后端可能需要不同的内存管理策略。

## 与其他后端的关系

| 能力 | NyarVM | JVM 后端 | WASM 后端 | Native 后端 |
|:---|:---:|:---:|:---:|:---:|
| EGraph 优化 | ✅ | ✅ | ✅ | ✅ |
| 部分求值 | ✅ | ✅ | ✅ | ✅ |
| 成本模型 | ✅ | ✅ | ✅ | ✅ |
| JIT 编译 | ✅ | ❌（AOT） | ❌（AOT） | ❌（AOT） |
| GC | ✅ 精确非移动 | ✅ JVM GC | ✅ 宿主 GC | ❌ 手动/可选 |
| 代数效应 | ✅ | ❌（异常表） | ❌（WASM 机制） | ❌（setjmp） |
| Witness Table | ✅ | ❌（vtable） | ❌（间接表） | ❌（直接调用） |
| 热更新 | ✅ | ❌ | ❌ | ❌ |
