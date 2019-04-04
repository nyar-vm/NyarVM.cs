# 📦 Acorn.Office

Microsoft Office 97-2003 和 Office Open XML 格式编解码器。

## 📐 格式布局

### OLE2 复合文档格式（.xls/.doc/.ppt）

Office 97-2003 文件基于 OLE2 复合文档格式。

| 字段                    | 偏移 | 大小 | 说明                                      | 对应类                            |
|-------------------------|------|------|-------------------------------------------|-----------------------------------|
| Magic                   | 0x00 | 8    | `0xD0 0xCF 0x11 0xE0 0xA1 0xB1 0x1A 0xE1` | `OfficeConstants.Ole2MagicNumber` |
| MinorVersion            | 0x18 | 2    | 次版本号                                  | -                                 |
| MajorVersion            | 0x1A | 2    | 主版本号（3=512扇区, 4=4096扇区）         | -                                 |
| ByteOrder               | 0x1C | 2    | 字节序（0xFFFE=小端序）                   | -                                 |
| SectorSize              | 0x1E | 2    | 扇区大小（9=512, 12=4096）                | -                                 |
| MiniSectorSize          | 0x20 | 2    | 迷你扇区大小（6=64）                      | -                                 |
| TotalSectors            | 0x2C | 4    | 扇区总数                                  | -                                 |
| FATSectorCount          | 0x2C | 4    | FAT 扇区数量                              | -                                 |
| FirstDirectorySectorSID | 0x30 | 4    | 第一个目录扇区 SID                        | -                                 |
| TransactionSignature    | 0x34 | 4    | 事务签名                                  | -                                 |
| MiniStreamCutoffSize    | 0x38 | 4    | 迷你流截断大小（4096）                    | -                                 |
| FirstMiniFATSectorSID   | 0x3C | 4    | 第一个迷你 FAT 扇区 SID                   | -                                 |
| MiniFATSectorCount      | 0x40 | 4    | 迷你 FAT 扇区数量                         | -                                 |
| FirstDIFATSectorSID     | 0x44 | 4    | 第一个 DIFAT 扇区 SID                     | -                                 |
| DIFATSectorCount        | 0x48 | 4    | DIFAT 扇区数量                            | -                                 |

### BIFF 记录（Excel .xls）

| 字段         | 大小 | 说明         |
|--------------|------|--------------|
| RecordType   | 2    | 记录类型     |
| RecordLength | 2    | 记录数据长度 |
| RecordData   | N    | 记录数据     |

### 常见 BIFF 记录类型

| 值     | 名称     | 说明              |
|--------|----------|-------------------|
| 0x0009 | BOF      | 文件开始          |
| 0x000A | EOF      | 文件结束          |
| 0x0006 | Formula  | 公式              |
| 0x0018 | Label    | 标签              |
| 0x0203 | Number   | 数字              |
| 0x00FD | LabelSst | 共享字符串表标签  |
| 0x0208 | Row      | 行                |
| 0x027E | RK       | RK 值（紧凑数字） |

### Office Open XML（.xlsx/.docx/.pptx）

基于 ZIP 压缩包，包含 XML 文件。

| 文件                       | 说明                |
|----------------------------|---------------------|
| `[Content_Types].xml`      | 内容类型定义        |
| `_rels/.rels`              | 包关系              |
| `xl/workbook.xml`          | Excel 工作簿        |
| `xl/worksheets/sheet1.xml` | Excel 工作表        |
| `xl/sharedStrings.xml`     | 共享字符串表        |
| `word/document.xml`        | Word 文档           |
| `ppt/presentation.xml`     | PowerPoint 演示文稿 |

## 🏗️ 核心类

| 类                  | 说明              | 文件                                                 |
|---------------------|-------------------|------------------------------------------------------|
| `ExcelWorkbookData` | Excel 工作簿数据  | [Data/OfficeData.cs](Data/OfficeData.cs)             |
| `ExcelSheetData`    | Excel 工作表      | [Data/OfficeData.cs](Data/OfficeData.cs)             |
| `ExcelRowData`      | Excel 行          | [Data/OfficeData.cs](Data/OfficeData.cs)             |
| `ExcelCellData`     | Excel 单元格      | [Data/OfficeData.cs](Data/OfficeData.cs)             |
| `OfficeConstants`   | Office 常量       | [Data/OfficeConstants.cs](Data/OfficeConstants.cs)   |
| `XlsRecordType`     | XLS BIFF 记录类型 | [Data/OfficeConstants.cs](Data/OfficeConstants.cs)   |
| `PptRecordType`     | PPT 记录类型      | [Data/OfficeConstants.cs](Data/OfficeConstants.cs)   |
| `XlsDecoder`        | XLS 解码器        | [Decode/XlsDecoder.cs](Decode/XlsDecoder.cs)         |
| `XlsEncoder`        | XLS 编码器        | [Encode/XlsEncoder.cs](Encode/XlsEncoder.cs)         |
| `DocDecoder`        | DOC 解码器        | [Decode/DocDecoder.cs](Decode/DocDecoder.cs)         |
| `PptDecoder`        | PPT 解码器        | [Decode/PptDecoder.cs](Decode/PptDecoder.cs)         |
| `OpenXmlDecoder`    | Open XML 解码器   | [Decode/OpenXmlDecoder.cs](Decode/OpenXmlDecoder.cs) |
| `XlsScanner`        | XLS 扫描器        | [Scanner/XlsScanner.cs](Scanner/XlsScanner.cs)       |

## 📚 格式规范参考

- [Microsoft Office File Formats Documentation](https://learn.microsoft.com/en-us/openspecs/office_file_formats/)
- [Office Open XML ECMA-376](https://ecma-international.org/publications-and-standards/standards/ecma-376/)
- [MS-XLS - Excel Binary File Format](https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-xls/cd03cb5f-ec00-4f0e-8818-22e2b7969d6d)
- [MS-DOC - Word Binary File Format](https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-doc/)
- [MS-PPT - PowerPoint Binary File Format](https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-ppt/)
