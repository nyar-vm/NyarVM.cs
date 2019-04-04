# Hardware 方言

## 概述

Hardware 方言面向芯片设计与高层次综合（HLS），将硬件描述提升为可优化的意图表示。

## 节点定义

### HIR 层（硬件结构）

| 节点 | 描述 | 示例 |
|------|------|------|
| Module | 硬件模块定义 | `(Module name ports body)` |
| Port | 输入输出端口 | `(Port Input "clk" 1)` |
| Wire / Reg | 线网与寄存器 | `(Reg "cnt" 32)` |
| Always | 时序/组合逻辑块 | `(Always posedge clk ...)` |

### MIR 层（微架构意图）

| 节点 | 描述 | 示例 |
|------|------|------|
| Pipeline | 流水线深度意图 | `(Pipeline stageCount body)` |
| Unroll | 循环展开 | `(Unroll factor loop)` |
| SystolicArray | 脉动阵列结构 | `(SystolicArray rows cols cellFn)` |

### LIR 层（门级）

| 节点 | 描述 |
|------|------|
| Gate | 基本门级单元（AND, OR, NOT...） |
| FlipFlop | D 触发器 |
| BitSlice, Concat, Mux, AddrGen | 位级操作 |

## 等价规则

| 规则 | 模式 | 重写 | 说明 |
|------|------|------|------|
| 逻辑化简 | `(a & b) \| (a & ~b)` | `a` | 布尔代数 |
| 寄存器重定时 | 移动寄存器位置 | 等价变换 | 时序优化 |
| 资源共享 | 互斥使用的乘法器合并 | 一个乘法器 | 面积优化 |
| 流水线插入 | `(Seq stages...)` | `(Pipeline depth (Seq stages...))` | 提高吞吐 |

## 降级路径

```
Hardware HIR → MIR（流水线、展开） → LIR（门级） → 后端
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| Verilog | `.v` 文本 | ASIC/FPGA 综合 |
| FIRRTL | `.fir` 文本 | Chisel 生态 |
| C++ 仿真 | `.cpp` | 快速功能验证 |
| FPGA 比特流 | 厂商工具链 | 实际部署 |

## 与部分求值协同

部分求值特化固定参数（如位宽、流水线深度），生成无动态判断的纯组合逻辑。例如 `stride=2` 的卷积层特化后，所有循环边界变为常量，生成无分支硬件描述。

## 参数化设计零开销

特化后生成无动态判断的纯组合逻辑。同一设计可生成 ASIC 综合用的 Verilog、FPGA 比特流、甚至 C++ 模型。
