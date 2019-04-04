# 📦 Acorn.Ttf

TrueType/OpenType 字体格式编解码器。

## 📐 格式布局

### 偏移表（Offset Table）

| 字段          | 偏移 | 大小 | 说明       | 对应类                       |
|---------------|------|------|------------|------------------------------|
| sfVersion     | 0x00 | 4    | 版本标识   | `TtfConstants.TrueTypeMagic` |
| numTables     | 0x04 | 2    | 表数量     | `TtfFontData.TableCount`     |
| searchRange   | 0x06 | 2    | 搜索范围   | -                            |
| entrySelector | 0x08 | 2    | 入口选择器 | -                            |
| rangeShift    | 0x0A | 2    | 范围偏移   | -                            |

### 表记录（Table Record）

| 字段     | 大小 | 说明   | 对应类                    |
|----------|------|--------|---------------------------|
| tag      | 4    | 表标签 | `TtfTableRecord.Tag`      |
| checkSum | 4    | 校验和 | `TtfTableRecord.Checksum` |
| offset   | 4    | 偏移量 | `TtfTableRecord.Offset`   |
| length   | 4    | 长度   | `TtfTableRecord.Length`   |

## 🏗️ 核心类

| 类               | 说明         | 文件                                           |
|------------------|--------------|------------------------------------------------|
| `TtfFontData`    | 字体完整数据 | [Data/TtfFontData.cs](Data/TtfFontData.cs)     |
| `TtfTableRecord` | 表记录       | [Data/TtfFontData.cs](Data/TtfFontData.cs)     |
| `TtfHeadInfo`    | head 表信息  | [Data/TtfFontData.cs](Data/TtfFontData.cs)     |
| `TtfNameInfo`    | name 表信息  | [Data/TtfFontData.cs](Data/TtfFontData.cs)     |
| `TtfConstants`   | TTF 常量     | [Data/TtfConstants.cs](Data/TtfConstants.cs)   |
| `TtfDecoder`     | TTF 解码器   | [Decode/TtfDecoder.cs](Decode/TtfDecoder.cs)   |
| `TtfScanner`     | TTF 扫描器   | [Scanner/TtfScanner.cs](Scanner/TtfScanner.cs) |

## 📚 格式规范参考

- [OpenType Specification](https://docs.microsoft.com/en-us/typography/opentype/)
- [TrueType Reference Manual](https://developer.apple.com/fonts/TrueType-Reference-Manual/)
