# 📦 Acorn.Zip

ZIP 归档格式编解码器。

## 📐 格式布局

### 本地文件头（Local File Header）

| 字段               | 偏移 | 大小 | 说明                         | 对应类                           |
|--------------------|------|------|------------------------------|----------------------------------|
| Signature          | 0x00 | 4    | `0x04034B50`（"PK\x03\x04"） | `ZipFileData`（内部解析）        |
| VersionNeeded      | 0x04 | 2    | 所需版本                     | -                                |
| GeneralPurposeFlag | 0x06 | 2    | 通用标志                     | -                                |
| CompressionMethod  | 0x08 | 2    | 压缩方法                     | `ZipEntryData.CompressionMethod` |
| LastModTime        | 0x0A | 2    | 最后修改时间                 | -                                |
| LastModDate        | 0x0C | 2    | 最后修改日期                 | -                                |
| CRC32              | 0x0E | 4    | CRC-32 校验                  | -                                |
| CompressedSize     | 0x12 | 4    | 压缩后大小                   | `ZipEntryData.CompressedSize`    |
| UncompressedSize   | 0x16 | 4    | 未压缩大小                   | `ZipEntryData.Size`              |
| FileNameLength     | 0x1A | 2    | 文件名长度                   | -                                |
| ExtraFieldLength   | 0x1C | 2    | 扩展字段长度                 | -                                |
| FileName           | 变长 | N    | 文件名                       | `ZipEntryData.Name`              |
| ExtraField         | 变长 | N    | 扩展字段                     | -                                |

### 压缩方法

| 值 | 名称      | 说明              |
|----|-----------|-------------------|
| 0  | Stored    | 无压缩            |
| 1  | Shrunk    | Shrunk            |
| 6  | Implode   | Implode           |
| 8  | Deflate   | Deflate（最常用） |
| 9  | Deflate64 | Deflate64         |
| 12 | BZIP2     | BZIP2             |
| 14 | LZMA      | LZMA              |
| 93 | Zstandard | Zstandard         |
| 95 | XZ        | XZ                |
| 99 | AES       | AES 加密          |

### 中央目录（Central Directory）

| 字段                   | 偏移 | 大小 | 说明                         |
|------------------------|------|------|------------------------------|
| Signature              | 0x00 | 4    | `0x02014B50`（"PK\x01\x02"） |
| VersionMadeBy          | 0x04 | 2    | 创建版本                     |
| VersionNeeded          | 0x06 | 2    | 所需版本                     |
| GeneralPurposeFlag     | 0x08 | 2    | 通用标志                     |
| CompressionMethod      | 0x0A | 2    | 压缩方法                     |
| LastModTime            | 0x0C | 2    | 最后修改时间                 |
| LastModDate            | 0x0E | 2    | 最后修改日期                 |
| CRC32                  | 0x10 | 4    | CRC-32                       |
| CompressedSize         | 0x14 | 4    | 压缩后大小                   |
| UncompressedSize       | 0x18 | 4    | 未压缩大小                   |
| FileNameLength         | 0x1C | 2    | 文件名长度                   |
| ExtraFieldLength       | 0x1E | 2    | 扩展字段长度                 |
| CommentLength          | 0x20 | 2    | 注释长度                     |
| DiskNumberStart        | 0x22 | 2    | 起始磁盘号                   |
| InternalFileAttributes | 0x24 | 2    | 内部文件属性                 |
| ExternalFileAttributes | 0x26 | 4    | 外部文件属性                 |
| RelativeOffset         | 0x2A | 4    | 本地文件头偏移               |

### 中央目录结束记录（End of Central Directory）

| 字段             | 偏移 | 大小 | 说明                         |
|------------------|------|------|------------------------------|
| Signature        | 0x00 | 4    | `0x06054B50`（"PK\x05\x06"） |
| DiskNumber       | 0x04 | 2    | 当前磁盘号                   |
| CentralDirDisk   | 0x06 | 2    | 中央目录所在磁盘             |
| EntriesOnDisk    | 0x08 | 2    | 当前磁盘条目数               |
| TotalEntries     | 0x0A | 2    | 总条目数                     |
| CentralDirSize   | 0x0C | 4    | 中央目录大小                 |
| CentralDirOffset | 0x10 | 4    | 中央目录偏移                 |
| CommentLength    | 0x14 | 2    | 注释长度                     |

## 🏗️ 核心类

| 类             | 说明             | 文件                                           |
|----------------|------------------|------------------------------------------------|
| `ZipFileData`  | ZIP 文件完整数据 | [Data/ZipFileData.cs](Data/ZipFileData.cs)     |
| `ZipEntryData` | ZIP 条目         | [Data/ZipFileData.cs](Data/ZipFileData.cs)     |
| `ZipDecoder`   | ZIP 解码器       | [Decode/ZipDecoder.cs](Decode/ZipDecoder.cs)   |
| `ZipScanner`   | ZIP 扫描器       | [Scanner/ZipScanner.cs](Scanner/ZipScanner.cs) |

## 📚 格式规范参考

- [ZIP File Format Specification](https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT)
- [Wikipedia - ZIP (file format)](https://en.wikipedia.org/wiki/ZIP_(file_format))
