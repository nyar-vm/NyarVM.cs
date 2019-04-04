# Quantum 方言

## 概述

Quantum 方言面向量子电路构建、优化与量子硬件编译领域，覆盖从算法级量子程序到物理量子比特映射的完整管线。它将量子门操作、电路模块和量子算法模式提升为意图节点，使优化器能够在逻辑层和物理层进行跨层优化。

## 节点定义

### 量子比特与寄存器节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Qubit | 单量子比特 | `(Qubit "q0")` |
| QuantumRegister | 量子寄存器 | `(QuantumRegister "qr" 4)` |
| ClassicalRegister | 经典寄存器 | `(ClassicalRegister "cr" 4)` |

### 单量子比特门节点

| 节点 | 描述 | 示例 |
|------|------|------|
| SingleQubitGate | 单量子比特门 | `(SingleQubitGate H q0)` |
| ParametricSingleGate | 参数化单门 | `(ParametricSingleGate Rx q0 angle)` |

支持的门类型：`X`, `Y`, `Z`, `H`, `S`, `T`, `Rx`, `Ry`, `Rz`

### 多量子比特门节点

| 节点 | 描述 | 示例 |
|------|------|------|
| ControlledGate | 受控门 | `(ControlledGate CX q0 q1)` |
| MultiControlledGate | 多控制门 | `(MultiControlledGate CX [q0 q1] q2)` |
| SwapGate | SWAP 门 | `(SwapGate q0 q1)` |
| ToffoliGate | Toffoli 门 | `(ToffoliGate q0 q1 q2)` |

### 测量与重置节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Measure | 测量 | `(Measure q0 c0)` |
| Reset | 重置 | `(Reset q0)` |
| ConditionalOperation | 条件操作 | `(ConditionalOperation c0 (X q1))` |

### 电路结构节点

| 节点 | 描述 | 示例 |
|------|------|------|
| QuantumCircuit | 量子电路 | `(QuantumCircuit [H q0, CX q0 q1])` |
| CircuitModule | 电路模块 | `(CircuitModule "Bell" [a b] [H a, CX a b])` |
| ModuleInstance | 模块实例化 | `(ModuleInstance "Bell" {a: q0, b: q1})` |
| CircuitDepth | 电路深度 | `(CircuitDepth circuit)` |
| QubitConnectivity | 量子比特连通性 | `(QubitConnectivity [(q0 q1) (q1 q2)])` |

### 量子算法节点

| 节点 | 描述 | 示例 |
|------|------|------|
| QuantumFourierTransform | QFT | `(QuantumFourierTransform [q0 q1 q2 q3])` |
| GroverDiffusion | Grover 扩散 | `(GroverDiffusion [q0 q1])` |
| PhaseEstimation | 相位估计 | `(PhaseEstimation U eigen phase)` |
| VariationalQuantumEigensolver | VQE | `(VariationalQuantumEigensolver ansatz H optimizer)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| Hadamard 幂等 | `H(H(q))` | `q` | |
| Pauli-X 幂等 | `X(X(q))` | `I` | |
| CNOT 自消除 | `CX(q, q)` | `I` | 控制位等于目标位 |
| 电路扁平化 | `QC([QC(a,b), c])` | `QC(a, b, c)` | |
| 门交换 | `CX(a,b); H(b)` | `H(b); CX(a,b)` | 当可交换时 |
| 测量延迟 | `Measure(q); H(q)` | 无效组合 | 检测错误 |

## 降级路径

```
Quantum HIR → 电路优化（门合并、消除） → 物理映射（路由、调度） → 后端指令
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| QASM | OpenQASM 代码 | 通用量子硬件 |
| Qiskit | Python 代码 | IBM 量子设备 |
| Cirq | Python 代码 | Google 量子设备 |
| Quil | Quil 指令 | Rigetti 设备 |
| 模拟器 | 经典模拟代码 | 算法验证、小规模测试 |
| Core 方言 | 通用控制流 | 量子-经典混合程序 |

## 与部分求值协同

若量子电路参数在编译时已知（如变分量子算法中的固定超参数），PE 可将参数化门特化为具体旋转角度，生成更紧凑的电路。若目标量子硬件的连通性已知，PE 可预计算 SWAP 插入策略，消除运行时的路由开销。
