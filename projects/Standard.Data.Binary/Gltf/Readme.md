# 📦 Acorn.Gltf

glTF 2.0（GL Transmission Format）二进制格式编解码器，支持 `.gltf` JSON 和 `.glb` 二进制容器格式。

## 📐 格式布局

### GLB 文件结构

GLB 是 glTF 的二进制容器格式，将 JSON 场景描述和二进制数据打包为单一文件。

| 部分               | 偏移   | 大小 | 说明                            | 对应类                         |
|--------------------|--------|------|---------------------------------|--------------------------------|
| 魔数（Magic）      | 0x00   | 4    | `"glTF"`（0x67 0x6C 0x54 0x46） | `GltfConstants.GlbMagicNumber` |
| 版本（Version）    | 0x04   | 4    | GLB 版本号（2）                 | `GlbHeader.Version`            |
| 文件长度（Length） | 0x08   | 4    | 完整文件长度                    | `GlbHeader.Length`             |
| JSON 块长度        | 0x0C   | 4    | JSON 块数据长度                 | `GlbChunkHeader.Length`        |
| JSON 块类型        | 0x10   | 4    | `0x4E4F534A`（"JSON"）          | `GltfConstants.ChunkTypeJson`  |
| JSON 块数据        | 0x14   | N    | UTF-8 JSON 数据                 | -                              |
| BIN 块长度         | 对齐后 | 4    | BIN 块数据长度                  | `GlbChunkHeader.Length`        |
| BIN 块类型         | 对齐后 | 4    | `0x004E4942`（"BIN\0"）         | `GltfConstants.ChunkTypeBin`   |
| BIN 块数据         | 对齐后 | N    | 二进制缓冲区数据                | -                              |

### GLTF JSON 核心数据结构

| 结构       | 说明                                          | 对应类           |
|------------|-----------------------------------------------|------------------|
| Asset      | 资产信息（版本、生成器、版权）                | `GltfAsset`      |
| Scene      | 场景（节点索引列表）                          | `GltfScene`      |
| Node       | 节点（变换、网格、相机、子节点）              | `GltfNode`       |
| Mesh       | 网格（图元列表）                              | `GltfMesh`       |
| Primitive  | 图元（属性、索引、材质、模式）                | `GltfPrimitive`  |
| Buffer     | 缓冲区（URI 或内联数据）                      | `GltfBuffer`     |
| BufferView | 缓冲区视图（偏移、长度、目标）                | `GltfBufferView` |
| Accessor   | 访问器（组件类型、类型、计数、范围）          | `GltfAccessor`   |
| Material   | 材质（PBR 金属度/粗糙度、法线、遮挡、自发光） | `GltfMaterial`   |
| Texture    | 纹理（采样器、图像索引）                      | `GltfTexture`    |
| Image      | 图像（URI 或缓冲区视图引用）                  | `GltfImage`      |
| Sampler    | 采样器（过滤模式、环绕模式）                  | `GltfSampler`    |
| Skin       | 蒙皮（逆绑定矩阵、关节节点）                  | `GltfSkin`       |
| Animation  | 动画（通道、采样器）                          | `GltfAnimation`  |

### Accessor 组件类型

| 值   | 类型             | 大小 |
|------|------------------|------|
| 5120 | `byte`           | 1    |
| 5121 | `unsigned byte`  | 1    |
| 5122 | `short`          | 2    |
| 5123 | `unsigned short` | 2    |
| 5125 | `unsigned int`   | 4    |
| 5126 | `float`          | 4    |

## 🏗️ 核心类

| 类               | 说明              | 文件                                             |
|------------------|-------------------|--------------------------------------------------|
| `GltfModelData`  | GLTF 模型完整数据 | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfAsset`      | 资产信息          | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfScene`      | 场景              | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfNode`       | 节点              | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfMesh`       | 网格              | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfAccessor`   | 访问器            | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfMaterial`   | 材质              | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfBuffer`     | 缓冲区            | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfBufferView` | 缓冲区视图        | [Data/GltfModelData.cs](Data/GltfModelData.cs)   |
| `GltfConstants`  | GLTF 常量         | [Data/GltfConstants.cs](Data/GltfConstants.cs)   |
| `GltfDecoder`    | GLTF/GLB 解码器   | [Decode/GltfDecoder.cs](Decode/GltfDecoder.cs)   |
| `GltfEncoder`    | GLTF/GLB 编码器   | [Encode/GltfEncoder.cs](Encode/GltfEncoder.cs)   |
| `GltfScanner`    | GLTF/GLB 扫描器   | [Scanner/GltfScanner.cs](Scanner/GltfScanner.cs) |

## 📚 格式规范参考

- [glTF 2.0 规范](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html)
- [glTF 2.0 快速参考](https://www.khronos.org/files/gltf20-reference-guide.pdf)
- [Khronos glTF GitHub](https://github.com/KhronosGroup/glTF)
