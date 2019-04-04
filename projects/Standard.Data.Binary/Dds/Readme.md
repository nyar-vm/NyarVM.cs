# 📦 Acorn.Dds

DirectDraw Surface (DDS) 纹理格式编解码器。

## 📐 格式布局

### DDS 文件头

| 字段              | 偏移 | 大小 | 说明             | 对应类                       |
|-------------------|------|------|------------------|------------------------------|
| Magic             | 0x00 | 4    | `"DDS "`         | `DdsConstants.MagicNumber`   |
| Size              | 0x04 | 4    | 头部大小（124）  | `DdsConstants.HeaderSize`    |
| Flags             | 0x08 | 4    | 表面标志位       | `DdsFlags`                   |
| Height            | 0x0C | 4    | 纹理高度         | `DdsTextureData.Height`      |
| Width             | 0x10 | 4    | 纹理宽度         | `DdsTextureData.Width`       |
| PitchOrLinearSize | 0x14 | 4    | 间距或线性大小   | -                            |
| Depth             | 0x18 | 4    | 深度（体积纹理） | `DdsTextureData.Depth`       |
| MipMapCount       | 0x1C | 4    | Mipmap 级别数    | `DdsTextureData.MipMapCount` |
| Reserved1         | 0x20 | 44   | 保留             | -                            |

### DDS 像素格式（DDPIXELFORMAT）

| 字段        | 偏移 | 大小 | 说明               | 对应类                           |
|-------------|------|------|--------------------|----------------------------------|
| Size        | 0x4C | 4    | 像素格式大小（32） | `DdsConstants.PixelFormatSize`   |
| Flags       | 0x50 | 4    | 像素格式标志       | `DdsPixelFormatFlags`            |
| FourCC      | 0x54 | 4    | 四字符代码         | `DdsPixelFormatData.FourCC`      |
| RGBBitCount | 0x58 | 4    | 每像素位数         | `DdsPixelFormatData.RGBBitCount` |
| RBitMask    | 0x5C | 4    | 红色位掩码         | `DdsPixelFormatData.RBitMask`    |
| GBitMask    | 0x60 | 4    | 绿色位掩码         | `DdsPixelFormatData.GBitMask`    |
| BBitMask    | 0x64 | 4    | 蓝色位掩码         | `DdsPixelFormatData.BBitMask`    |
| ABitMask    | 0x68 | 4    | Alpha 位掩码       | `DdsPixelFormatData.ABitMask`    |

### DDS DX10 扩展头（可选，FourCC 为 "DX10" 时存在）

| 字段              | 大小 | 说明         |
|-------------------|------|--------------|
| DXGI_FORMAT       | 4    | DXGI 格式    |
| ResourceDimension | 4    | 资源维度     |
| MiscFlag          | 4    | 杂项标志     |
| ArraySize         | 4    | 纹理数组大小 |
| MiscFlags2        | 4    | 杂项标志 2   |

## 🏗️ 核心类

| 类                    | 说明               | 文件                                             |
|-----------------------|--------------------|--------------------------------------------------|
| `DdsTextureData`      | DDS 纹理完整数据   | [Data/DdsTextureData.cs](Data/DdsTextureData.cs) |
| `DdsPixelFormatData`  | DDS 像素格式       | [Data/DdsTextureData.cs](Data/DdsTextureData.cs) |
| `DdsConstants`        | DDS 常量           | [Data/DdsConstants.cs](Data/DdsConstants.cs)     |
| `DdsFlags`            | 表面标志位枚举     | [Data/DdsConstants.cs](Data/DdsConstants.cs)     |
| `DdsPixelFormatFlags` | 像素格式标志位枚举 | [Data/DdsConstants.cs](Data/DdsConstants.cs)     |
| `DdsDecoder`          | DDS 解码器         | [Decode/DdsDecoder.cs](Decode/DdsDecoder.cs)     |
| `DdsEncoder`          | DDS 编码器         | [Encode/DdsEncoder.cs](Encode/DdsEncoder.cs)     |
| `DdsScanner`          | DDS 扫描器         | [Scanner/DdsScanner.cs](Scanner/DdsScanner.cs)   |

## 📚 格式规范参考

- [Microsoft DDS Documentation](https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide)
- [DDS File Format Wiki](https://en.wikipedia.org/wiki/DirectDraw_Surface)
