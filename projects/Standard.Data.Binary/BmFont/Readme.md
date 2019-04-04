# 📦 Acorn.BmFont

AngelCode BMFont 位图字体格式编解码器（二进制格式）。

## 📐 格式布局

### BMFont 二进制文件头

| 字段    | 偏移 | 大小 | 说明        | 对应类                          |
|---------|------|------|-------------|---------------------------------|
| Magic   | 0x00 | 3    | `"BMF"`     | `BmFontConstants.BinaryMagic`   |
| Version | 0x03 | 1    | 版本号（3） | `BmFontConstants.BinaryVersion` |

### 块结构

| 字段      | 大小 | 说明       |
|-----------|------|------------|
| BlockType | 1    | 块类型 ID  |
| BlockSize | 4    | 块数据大小 |
| Data      | N    | 块数据     |

### 块类型

| ID | 说明     | 对应类                              |
|----|----------|-------------------------------------|
| 1  | 字体信息 | `BmFontConstants.BlockInfo`         |
| 2  | 通用信息 | `BmFontConstants.BlockCommon`       |
| 3  | 页面名称 | `BmFontConstants.BlockPages`        |
| 4  | 字符数据 | `BmFontConstants.BlockChars`        |
| 5  | 字距数据 | `BmFontConstants.BlockKerningPairs` |

## 🏗️ 核心类

| 类                  | 说明            | 文件                                                 |
|---------------------|-----------------|------------------------------------------------------|
| `BmFontData`        | BMFont 完整数据 | [Data/BmFontData.cs](Data/BmFontData.cs)             |
| `BmFontInfo`        | 字体信息        | [Data/BmFontData.cs](Data/BmFontData.cs)             |
| `BmFontCommon`      | 通用信息        | [Data/BmFontData.cs](Data/BmFontData.cs)             |
| `BmFontChar`        | 字符信息        | [Data/BmFontData.cs](Data/BmFontData.cs)             |
| `BmFontKerningPair` | 字距对          | [Data/BmFontData.cs](Data/BmFontData.cs)             |
| `BmFontConstants`   | BMFont 常量     | [Data/BmFontConstants.cs](Data/BmFontConstants.cs)   |
| `BmFontDecoder`     | BMFont 解码器   | [Decode/BmFontDecoder.cs](Decode/BmFontDecoder.cs)   |
| `BmFontScanner`     | BMFont 扫描器   | [Scanner/BmFontScanner.cs](Scanner/BmFontScanner.cs) |

## 📚 格式规范参考

- [AngelCode BMFont Documentation](https://www.angelcode.com/products/bmfont/doc/file_format.html)
