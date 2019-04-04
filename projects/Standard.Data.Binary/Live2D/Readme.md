# 📦 Acorn.Live2D

Live2D Cubism `.moc3` 模型二进制格式编解码器。

## 📐 格式布局

moc3 格式采用 **Structure of Arrays (SoA)** 范式：每个数据字段存储为独立的连续数组，而非数组的结构体。 段偏移表（Section
Offset Table）记录各段在文件中的偏移量，解码器通过偏移表定位各段数据。

### 文件整体结构

```
┌──────────────────────────────┐
│         文件头 (8 字节)       │
├──────────────────────────────┤
│    段偏移表 (i32 数组)        │
├──────────────────────────────┤
│         CanvasInfo 段         │
├──────────────────────────────┤
│         CountInfo 段          │
├──────────────────────────────┤
│      ParameterIds 段          │
├──────────────────────────────┤
│      ParameterMinValues 段    │
├──────────────────────────────┤
│            ...               │
├──────────────────────────────┤
│      DrawableIndices 段       │
├──────────────────────────────┤
│   (v4+) DeformerIds 段       │
├──────────────────────────────┤
│            ...               │
└──────────────────────────────┘
```

### moc3 文件头

| 字段      | 偏移 | 大小 | 说明                         | 对应类                            |
|-----------|------|------|------------------------------|-----------------------------------|
| Signature | 0x00 | 4    | `"MOC3"` 签名                | `Live2DConstants.Moc3MagicNumber` |
| Version   | 0x04 | 1    | moc3 版本（3/4/5）           | `Live2DModelData.Version`         |
| Flags     | 0x05 | 1    | 标志（0=小端序，非0=大端序） | `Live2DModelData.IsBigEndian`     |
| Revision  | 0x06 | 2    | 版本修订号（i16）            | `Live2DModelData.Revision`        |

### 段偏移表（Section Offset Table）

紧跟文件头，每个条目为 i32，表示对应段在文件中的偏移量（相对于文件起始位置）。偏移量为 0 表示该段不存在。

| 版本 | 条目数 | 大小（字节） |
|------|--------|--------------|
| v3   | 22     | 88           |
| v4   | 25     | 100          |
| v5   | 28     | 112          |

### 段偏移表索引

| 索引 | 段名                      | 数据类型            | 版本 | 对应枚举                                |
|------|---------------------------|---------------------|------|-----------------------------------------|
| 0    | CanvasInfo                | 5 × f32             | v3+  | `Moc3Section.CanvasInfo`                |
| 1    | CountInfo                 | 5 × i32             | v3+  | `Moc3Section.CountInfo`                 |
| 2    | ParameterIds              | null 终止字符串数组 | v3+  | `Moc3Section.ParameterIds`              |
| 3    | ParameterMinimumValues    | f32 数组            | v3+  | `Moc3Section.ParameterMinimumValues`    |
| 4    | ParameterMaximumValues    | f32 数组            | v3+  | `Moc3Section.ParameterMaximumValues`    |
| 5    | ParameterDefaultValues    | f32 数组            | v3+  | `Moc3Section.ParameterDefaultValues`    |
| 6    | PartIds                   | null 终止字符串数组 | v3+  | `Moc3Section.PartIds`                   |
| 7    | PartParentPartIndices     | i32 数组            | v3+  | `Moc3Section.PartParentPartIndices`     |
| 8    | DrawableIds               | null 终止字符串数组 | v3+  | `Moc3Section.DrawableIds`               |
| 9    | DrawableConstantFlags     | u8 数组             | v3+  | `Moc3Section.DrawableConstantFlags`     |
| 10   | DrawableTextureIndices    | i32 数组            | v3+  | `Moc3Section.DrawableTextureIndices`    |
| 11   | DrawableDrawOrders        | i32 数组            | v3+  | `Moc3Section.DrawableDrawOrders`        |
| 12   | DrawableRenderOrders      | i32 数组            | v3+  | `Moc3Section.DrawableRenderOrders`      |
| 13   | DrawableMaskCounts        | i32 数组            | v3+  | `Moc3Section.DrawableMaskCounts`        |
| 14   | DrawableMasks             | i32 二维数组        | v3+  | `Moc3Section.DrawableMasks`             |
| 15   | DrawableVertexCounts      | i32 数组            | v3+  | `Moc3Section.DrawableVertexCounts`      |
| 16   | DrawableVertexPositions   | f32 二维数组        | v3+  | `Moc3Section.DrawableVertexPositions`   |
| 17   | DrawableVertexUvs         | f32 二维数组        | v3+  | `Moc3Section.DrawableVertexUvs`         |
| 18   | DrawableIndices           | i32 二维数组        | v3+  | `Moc3Section.DrawableIndices`           |
| 19   | DrawableRepeatFlags       | u8 数组             | v3+  | `Moc3Section.DrawableRepeatFlags`       |
| 20   | DeformerIds               | null 终止字符串数组 | v4+  | `Moc3Section.DeformerIds`               |
| 21   | DeformerTypes             | u8 数组             | v4+  | `Moc3Section.DeformerTypes`             |
| 22   | DeformerParentIndices     | i32 数组            | v4+  | `Moc3Section.DeformerParentIndices`     |
| 23   | DeformerBoundingBoxX      | f32 数组            | v4+  | `Moc3Section.DeformerBoundingBoxX`      |
| 24   | DeformerBoundingBoxY      | f32 数组            | v5+  | `Moc3Section.DeformerBoundingBoxY`      |
| 25   | DeformerBoundingBoxWidth  | f32 数组            | v5+  | `Moc3Section.DeformerBoundingBoxWidth`  |
| 26   | DeformerBoundingBoxHeight | f32 数组            | v5+  | `Moc3Section.DeformerBoundingBoxHeight` |
| 27   | DeformerRotation          | f32 数组            | v5+  | `Moc3Section.DeformerRotation`          |

### CountInfo 段字段偏移

| 字段           | 偏移 | 大小 | 说明              |
|----------------|------|------|-------------------|
| ParameterCount | 0    | 4    | 参数数量          |
| PartCount      | 4    | 4    | 部件数量          |
| DrawableCount  | 8    | 4    | 绘制对象数量      |
| DeformerCount  | 12   | 4    | 变形器数量（v4+） |
| TextureCount   | 16   | 4    | 纹理数量          |

### CanvasInfo 段字段

| 字段          | 偏移 | 大小 | 说明            |
|---------------|------|------|-----------------|
| Width         | 0    | 4    | 画布宽度        |
| Height        | 4    | 4    | 画布高度        |
| CenterX       | 8    | 4    | 画布中心 X 坐标 |
| CenterY       | 12   | 4    | 画布中心 Y 坐标 |
| PixelsPerUnit | 16   | 4    | 像素密度        |

## 🏗️ 核心类

| 类                 | 说明              | 文件                                                   |
|--------------------|-------------------|--------------------------------------------------------|
| `Live2DModelData`  | moc3 模型完整数据 | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DCanvasInfo` | 画布信息          | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DParameter`  | 参数定义          | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DPart`       | 部件定义          | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DDrawable`   | 绘制对象          | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DDeformer`   | 变形器            | [Data/Live2DModelData.cs](Data/Live2DModelData.cs)     |
| `Live2DConstants`  | Live2D 常量       | [Data/Live2DConstants.cs](Data/Live2DConstants.cs)     |
| `Moc3Section`      | 段偏移表索引枚举  | [Data/Live2DConstants.cs](Data/Live2DConstants.cs)     |
| `Live2DDecoder`    | moc3 解码器       | [Decode/Live2DDecoder.cs](Decode/Live2DDecoder.cs)     |
| `Live2DEncoder`    | moc3 编码器       | [Encode/Live2DEncoder.cs](Encode/Live2DEncoder.cs)     |
| `ILive2DScanner`   | 扫描器接口        | [Scanner/ILive2DScanner.cs](Scanner/ILive2DScanner.cs) |
| `Live2DScanner`    | moc3 扫描器       | [Scanner/Live2DScanner.cs](Scanner/Live2DScanner.cs)   |

## 📚 格式规范参考

- [Live2D Cubism SDK 文档](https://docs.live2d.com/cubism-sdk-manual/)
- [Live2D 官方 GitHub](https://github.com/Live2D)
