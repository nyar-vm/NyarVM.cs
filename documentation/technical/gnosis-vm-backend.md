# Gnosis VM 后端技术文档

## 概述

Gnosis VM 后端是 Nyar 元编译器体系的游戏特化后端，将 Nyar IR（意图）编译为专为游戏设计的 Gnosis VM 字节码。该后端面向领域优化，深度集成 ECS、AI、导航、渲染、网络等游戏子系统。

## 架构定位

```
┌─────────────────────────────────────────────────────────────┐
│                      Nyar 元编译器核心                        │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────────┐ │
│  │  意图构建器 │ -> │ EGraph 优化  │ -> │ 降低管道         │ │
│  └─────────────┘    └─────────────┘    └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │    GnosisDialect (方言层)       │
              │  ECS/AI/导航/渲染/网络节点优化    │
              └───────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │    GnosisBackend (后端层)       │
              │  NyarModule → .gnosis 字节码    │
              └───────────────────────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │    Gnosis.Runtime (执行层)      │
              │  加载 .gnosis → 指令分发 → 执行  │
              └───────────────────────────────┘
```

## 核心组件

### 1. Arch.GnosisVm

在 `Nyar.Core.Types.Arch` 中注册的架构标识：

```csharp

/// <summary>

///     Gnosis 游戏特化虚拟机

/// </summary>
GnosisVm
```

目标三元组格式：`gnosisvm-pc-windows-gnu`

### 2. GnosisBackend

实现 `IBackend` 接口，核心编译流程：

```csharp
public sealed class GnosisBackend : IBackend
{
    public string Name => "GnosisVM";
    public IReadOnlyList<Arch> SupportedArchs => new[] { Arch.GnosisVm };
    
    public GeneratedFiles Compile(NyarModule module, CompilationOptions options)
    {
        // 1. 函数转换
        // 2. ECS 元数据提取
        // 3. 字节码编码
        // 4. 调试信息生成
    }
}
```

### 3. GnosisDialect

方言 ID：100，名称：`gnosis`

**重写规则**：

| 规则 | 描述 |
|:---|:---|
| `gnosis-query-merge` | 合并多个独立 ECS 查询为批量查询 |
| `gnosis-entity-batch` | 连续实体创建合并为批量创建 |
| `gnosis-component-access-fusion` | 连续读写同一组件合并为直接引用 |
| `gnosis-ai-sense-batch` | 合并同类型 AI 感知检测 |
| `gnosis-behavior-tree-inline` | 小型行为树内联为指令序列 |
| `gnosis-nav-path-cache` | 缓存频繁查询的导航路径 |
| `gnosis-arena-promotion` | 堆分配提升为 Arena 分配 |

**成本模型**：

面向游戏帧预算感知的成本估算，关键操作成本：

| 操作类型 | 延迟周期 | 说明 |
|:---|:---|:---|
| ECS 查询 | 50 | 涉及 Archetype 匹配 |
| AI 规划 | 200 | GOAP 规划器开销 |
| 寻路 | 150 | A* 路径计算 |
| 渲染提交 | 100 | GPU 命令缓冲 |
| 网络 RPC | 80 | 序列化+传输 |
| Arena 分配 | 2 | 高效内存分配 |

### 4. Gnosis VM 字节码格式

> 完整规范请参阅 [gnosis-bytecode-format.md](../../../Gnosis.cs/documentation/technical/gnosis-bytecode-format.md)

**文件头**：

```
0x00-0x03: 魔数 "GNOS" (0x474E4F53)
0x04-0x05: 版本号 (u16 = 1)
0x06-...:  模块名称、常量池、符号表、指令段
```

**指令集分类**（Game 方言特化，操作码为单字节 `byte` 范围）：

| 范围 | 类别 | 说明 |
|:---|:---|:---|
| 0x00-0x01 | 控制 | Halt, Nop |
| 0x10-0x18 | 常量推送 | PushInt8/16/32/64, PushFloat32/64, PushTrue/False/Null |
| 0x20-0x21 | 栈操作 | Pop, Dup |
| 0x30-0x4B | 算术/比较/逻辑 | AddInt ~ Not |
| 0x50-0x53 | 调用 | Call, CallNative, Return, CallModule |
| 0x60-0x65 | 变量访问 | LoadLocal, StoreLocal, LoadGlobal, StoreGlobal, LoadField, StoreField |
| 0x70-0x72 | 对象 | NewObject, GetField, SetField |
| 0x80-0x8E | ECS（Game 方言特化） | SpawnEntity ~ WorldUpdate |
| 0x90-0x93 | UTF-8 文本 | PushUtf8, ConcatUtf8, Utf8Length, Utf8GetChar |
| 0xA0-0xA3 | 数组 | NewArray, ArrayGet, ArraySet, ArrayLength |
| 0xB0-0xB2 | 闭包 | MakeClosure, GetUpvalue, SetUpvalue |
| 0xC0-0xC2 | 类型检查 | IsNull, IsType, TypeOf |
| 0xD0-0xD1 | 协程 | Yield, Resume |

> ⛔ **已废弃**：旧版本文档中描述的双魔数格式（"GNOS" + "IS\0\0"）和 0x100+ 范围的多字节操作码已废弃。Gnosis VM 字节码使用单字节操作码，ECS 指令在 0x80-0x8E 范围。

## 降级路径

```
GnosisDialect (层级 3)
    │
    ├── ECS 节点 ────────┐
    ├── AI 节点 ─────────┤
    ├── 导航节点 ────────┤ → StandardDialect (层级 2)
    ├── 渲染节点 ────────┤    │
    ├── 网络节点 ────────┤    └── CoreDialect (层级 1)
    └── 物理节点 ────────┘         │
                                   └── 后端指令选择
                                       ├── x64 原生
                                       ├── Wasm
                                       └── Gnosis VM
```

## 跨团队协作

详见 `.trae/roadmaps/gnosis-vm-requirements.md`

## 使用示例

```csharp
// 创建编译目标
var target = new CompilationTarget
{
    Arch = Arch.GnosisVm,
    OS = OS.Windows,
    Environment = TargetEnvironment.Native
};

// 选择后端
var selector = new BackendSelector(new[]
{
    new ClrBackend(),
    new WasmBackend(),
    new GnosisBackend()
});

var backend = selector.SelectBackend(target);

// 编译
var files = backend.Compile(module, new CompilationOptions
{
    GenerateTextOutput = true
});

// 输出 .gnosis 字节码文件
var bytecode = files.GetFile("MyModule.gnosis");
```

## 调试输出格式

```
; Gnosis VM Module: MyModule
; Version: 1.0

; Utf8 Pool
; [0] "Update"
; [1] "Transform"

function system_Update() -> Void
  ; [System] Phase: Update
  param delta : F32
  local entities : QueryIterator

  IL_0000: Ldc 0
  IL_0001: StLoc 0
  IL_0002: QueryExecute [1]
  IL_0003: StLoc 1
  IL_0004: QueryIterate loc[1] label_0008
  IL_0005: LdLoc 1
  IL_0006: ComponentGet loc[0] [1]
  IL_0007: Br label_0004
  IL_0008: Ret
end
```
