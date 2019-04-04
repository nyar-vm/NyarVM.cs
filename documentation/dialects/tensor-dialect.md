# Tensor 方言

## 概述

Tensor 方言面向深度学习与科学计算领域，覆盖从训练到推理的完整管线。

## 节点定义

### HIR 层（算子层）

| 节点 | 描述 | 示例 |
|------|------|------|
| Conv2D | 二维卷积 | `(Conv2D x w)` |
| MatMul | 矩阵乘法 | `(MatMul a b)` |
| Pool | 池化操作 | `(Pool Max x)` |
| BatchNorm | 批归一化 | `(BatchNorm x scale bias)` |
| Relu, Softmax | 激活函数 | `(Relu x)` |
| Reshape, Transpose, Concat, Slice | 张量操作 | `(Transpose x perm)` |
| TensorConst, Placeholder | 数据源 | `(TensorConst [1 2 3])` |
| FusedConvBNRelu | 融合算子（作为重写目标） | `(FusedConvBNRelu x w ...)` |

### 训练相关节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Grad | 计算梯度 | `(Grad loss_fn params)` |
| OptimizerStep | 参数更新 | `(OptimizerStep Adam params grads)` |
| Loss | 损失函数 | `(Loss CrossEntropy logits labels)` |
| BatchNormTraining | 训练模式批归一化 | `(BatchNormTraining x ... running_mean ...)` |
| Dropout | 随机丢弃 | `(Dropout x rate)` |
| Checkpoint | 梯度检查点 | `(Checkpoint segment_fn inputs)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| 算子融合 | `BatchNorm(Conv2D(x,w), ...)` | `FusedConvBN(x,w,...)` | 推理模式 |
| 转置消除 | `Transpose(Transpose(x, p1), p2)` | `Transpose(x, compose(p1,p2))` | |
| 梯度融合 | `Grad(Compose f g, x)` | 通过链式法则分解 | 减少中间张量 |
| 重计算策略 | `Checkpoint segment` | `Recompute segment` 或 `Materialize segment` | 显存/计算权衡 |
| Adam 特化 | `OptimizerStep Adam ...` | 展开为无循环计算 | 静态超参已知 |

## 降级路径

```
Tensor HIR → MIR（自动微分图，融合规则） → LIR（循环嵌套，分块，向量化） → 后端
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| CUDA | `.cu` 内核 | NVIDIA GPU |
| Metal | `.metal` 内核 | Apple GPU |
| WebGPU | WGSL 着色器 | 浏览器 GPU |
| Core 循环 | CPU 循环嵌套 | 通用 CPU |
| PyTorch/XLA | 后端调用 | 训练框架集成 |

## 与部分求值协同

若卷积核权重为常量，PE 可将 Conv2D 部分求值为针对该权重的特化循环。若 batch size 在推理时固定，PE 可将循环边界特化为常量，展开循环生成无分支代码。
