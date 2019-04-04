# 📦 Acorn.Psd

Adobe Photoshop PSD 文档格式编解码器。

## 📐 格式布局

### PSD 文件头（File Header Section）

| 字段      | 偏移 | 大小 | 说明                            | 对应类                     |
|-----------|------|------|---------------------------------|----------------------------|
| Signature | 0x00 | 4    | `"8BPS"`（0x38 0x42 0x50 0x53） | `PsdConstants.MagicNumber` |
| Version   | 0x04 | 2    | 版本号（1）                     | `PsdConstants.Version`     |
| Reserved  | 0x06 | 6    | 保留（必须为0）                 | -                          |
| Channels  | 0x0C | 2    | 通道数量（1-56）                | `PsdImageData.Channels`    |
| Height    | 0x0E | 4    | 图像高度（像素）                | `PsdImageData.Height`      |
| Width     | 0x12 | 4    | 图像宽度（像素）                | `PsdImageData.Width`       |
| Depth     | 0x16 | 2    | 颜色深度（1/8/16/32）           | `PsdImageData.Depth`       |
| ColorMode | 0x18 | 2    | 颜色模式                        | `PsdImageData.ColorMode`   |

### 颜色模式数据（Color Mode Data Section）

| 字段   | 大小 | 说明                                |
|--------|------|-------------------------------------|
| Length | 4    | 颜色模式数据长度                    |
| Data   | N    | 颜色模式数据（索引色/双色调时存在） |

### 图像资源（Image Resources Section）

| 字段      | 大小 | 说明           |
|-----------|------|----------------|
| Length    | 4    | 资源数据长度   |
| Resources | N    | 图像资源块序列 |

### 图层和蒙版信息（Layer and Mask Information Section）

| 字段                | 大小 | 说明                                     |
|---------------------|------|------------------------------------------|
| Length              | 4/8  | 节区长度（4GB以下用4字节，否则为12字节） |
| LayerInfo           | N    | 图层信息                                 |
| GlobalLayerMaskInfo | N    | 全局图层蒙版信息                         |
| AdditionalLayerInfo | N    | 附加图层信息                             |

### 图层记录（Layer Record）

| 字段               | 大小    | 说明                 | 对应类                            |
|--------------------|---------|----------------------|-----------------------------------|
| Top                | 4       | 上边界               | `PsdLayer.Bounds.Top`             |
| Left               | 4       | 左边界               | `PsdLayer.Bounds.Left`            |
| Bottom             | 4       | 下边界               | `PsdLayer.Bounds.Bottom`          |
| Right              | 4       | 右边界               | `PsdLayer.Bounds.Right`           |
| ChannelCount       | 2       | 通道数量             | `PsdLayer.ChannelCount`           |
| ChannelInfo        | 6*Count | 通道信息（ID, 长度） | -                                 |
| BlendModeSignature | 4       | `"8BPS"`             | `PsdConstants.BlendModeSignature` |
| BlendModeKey       | 4       | 混合模式关键字       | `PsdLayer.BlendMode`              |
| Opacity            | 1       | 不透明度             | `PsdLayer.Opacity`                |
| Clipping           | 1       | 剪贴标志             | -                                 |
| Flags              | 1       | 图层标志             | `PsdLayer.IsVisible`              |
| Filler             | 1       | 填充                 | -                                 |
| ExtraDataLength    | 4       | 额外数据长度         | -                                 |

### 图像数据（Image Data Section）

| 字段        | 大小 | 说明                                         |
|-------------|------|----------------------------------------------|
| Compression | 2    | 压缩方式（0=原始, 1=RLE, 2=ZIP, 3=ZIP+预测） |
| Data        | N    | 压缩后的像素数据                             |

## 🏗️ 核心类

| 类               | 说明             | 文件                                           |
|------------------|------------------|------------------------------------------------|
| `PsdImageData`   | PSD 图像完整数据 | [Data/PsdImageData.cs](Data/PsdImageData.cs)   |
| `PsdLayer`       | PSD 图层         | [Data/PsdImageData.cs](Data/PsdImageData.cs)   |
| `PsdConstants`   | PSD 常量         | [Data/PsdConstants.cs](Data/PsdConstants.cs)   |
| `PsdColorMode`   | 颜色模式枚举     | [Data/PsdConstants.cs](Data/PsdConstants.cs)   |
| `PsdCompression` | 压缩方式枚举     | [Data/PsdConstants.cs](Data/PsdConstants.cs)   |
| `PsdDecoder`     | PSD 解码器       | [Decode/PsdDecoder.cs](Decode/PsdDecoder.cs)   |
| `PsdEncoder`     | PSD 编码器       | [Encode/PsdEncoder.cs](Encode/PsdEncoder.cs)   |
| `PsdScanner`     | PSD 扫描器       | [Scanner/PsdScanner.cs](Scanner/PsdScanner.cs) |

## 📚 格式规范参考

- [Adobe Photoshop File Formats Specification](https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/)
- [PSD File Format Wiki](https://en.wikipedia.org/wiki/Adobe_Photoshop#File_format)
