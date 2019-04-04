# Nyar.Language.Shader — Shader 方言占位包

## 状态

占位阶段（Placeholder）。等待 Shader 方言相关实现填充。

## 对 Nyar 体系的核心价值

### 1. GPU 计算与图形管线

Shader 方言提供完整的 GPU 计算与图形管线支持，包括计算着色器、顶点着色器、片段着色器、几何着色器、光线追踪管线。

## 验证重点：

- 计算着色器内核与工作组同步
- 存储缓冲与统一缓冲访问
- 向量与矩阵运算
- 纹理采样与原子操作
- 循环向量化优化
- 屏障消除优化

## 对标 Nyar 需求

| Shader 方言节点 | 对应核心功能   | 优先级 |
|:----------------|:---------------|:-------|
| ComputeKernel   | 计算着色器内核 | P0     |
| Barrier         | 工作组同步     | P0     |
| Vec/Mat         | 向量与矩阵运算 | P1     |
| Sample          | 纹理采样       | P1     |
| MatMul          | 矩阵乘法优化   | P2     |

## 参考资源

- [Documentation](../documentation/zh-hans/dialects/shader-dialect.md)
