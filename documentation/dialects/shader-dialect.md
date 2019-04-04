# Shader 方言

## 概述

Shader 方言面向 GPU 着色器编程领域，覆盖顶点着色器、片段着色器、计算着色器等各类 GPU 程序。它将图形管线中的着色计算和通用 GPU 计算提升为意图节点，使优化器能够跨着色器阶段进行全局优化。

## 节点定义

### 着色器阶段节点

| 节点 | 描述 | 示例 |
|------|------|------|
| VertexShader | 顶点着色器 | `(VertexShader inputs main_fn)` |
| FragmentShader | 片段着色器 | `(FragmentShader inputs main_fn)` |
| ComputeShader | 计算着色器 | `(ComputeShader [x y z] main_fn)` |
| GeometryShader | 几何着色器 | `(GeometryShader inputs outputs main_fn)` |

### 图形计算节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Sample | 纹理采样 | `(Sample texture uv)` |
| Interpolate | 属性插值 | `(Interpolate vertex_attrs barycentric)` |
| Rasterize | 光栅化 | `(Rasterize triangles viewport)` |
| DepthTest | 深度测试 | `(DepthTest fragment depth_buffer)` |
| Blend | 颜色混合 | `(Blend src dst BlendMode.Alpha)` |

### 向量与矩阵操作节点

| 节点 | 描述 | 示例 |
|------|------|------|
| Dot | 点积 | `(Dot a b)` |
| Cross | 叉积 | `(Cross a b)` |
| Normalize | 归一化 | `(Normalize v)` |
| Transform | 矩阵变换 | `(Transform m v)` |
| Reflect | 反射向量 | `(Reflect incident normal)` |
| Refract | 折射向量 | `(Refract incident normal eta)` |

### 着色效果节点

| 节点 | 描述 | 示例 |
|------|------|------|
| PhongLighting | Phong 光照模型 | `(PhongLighting pos normal light material)` |
| PbrMaterial | PBR 材质 | `(PbrMaterial albedo roughness metallic)` |
| ToneMap | 色调映射 | `(ToneMap hdr_color ACES)` |
| GammaCorrect | Gamma 校正 | `(GammaCorrect linear 2.2)` |

## 等价规则

| 规则 | 模式 | 重写 | 条件 |
|------|------|------|------|
| 矩阵链合并 | `Transform(M2, Transform(M1, v))` | `Transform(M2*M1, v)` | 矩阵可预乘 |
| 采样消除 | `Sample(const_tex, uv)` | 常量颜色 | 纹理为常量 |
| 插值优化 | `Interpolate([c,c,c], _)` | `c` | 所有顶点属性相同 |
| 光照预计算 | `PhongLighting` | 查找表 | 光源和材质静态 |
| 混合简化 | `Blend(a, 0, _)` | `a` | 目标为透明 |

## 降级路径

```
Shader HIR → 着色器特化（常量传播） → 指令选择（SIMD/向量） → 后端代码生成
```

### 后端选择

| 后端 | 输出 | 适用场景 |
|------|------|----------|
| GLSL | OpenGL 着色器 | 桌面/移动 OpenGL |
| HLSL | DirectX 着色器 | Windows 平台 |
| WGSL | WebGPU 着色器 | 浏览器 GPU |
| SPIR-V | Vulkan 中间码 | Vulkan/Metal |
| MSL | Metal 着色器 | Apple 平台 |
| CUDA | GPU 计算内核 | NVIDIA 通用计算 |
| Core 方言 | 通用控制流 | CPU 回退路径 |

## 与部分求值协同

若材质参数在编译时已知（如静态场景中的固定材质），PE 可将 PBR 计算特化为预计算的 BRDF 查找表，大幅减少运行时计算。若变换矩阵静态，PE 可将 `Transform` 链合并为单个矩阵乘法，甚至完全消除变换节点。
