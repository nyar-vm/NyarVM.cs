# 📦 Acorn.DWARF

DWARF 调试信息格式编解码器。

## 📐 格式布局

DWARF 是一种用于可执行文件的调试信息格式，包含编译单元、调试行、调试帧等信息。

### 编译单元头（Compilation Unit Header）

| 字段                | 偏移 | 大小 | 说明                                             | 对应类                                         |
|---------------------|------|------|--------------------------------------------------|------------------------------------------------|
| UnitLength          | 0x00 | 4/12 | 单元长度（不含自身），4字节或12字节（64位DWARF） | `DWARFCompilationUnitData.UnitLength`          |
| Version             | 0x04 | 2    | DWARF 版本（2/3/4/5）                            | `DWARFCompilationUnitData.Version`             |
| DebugInfoOffset     | 0x06 | 4/8  | 缩略码表偏移（DWARF 4+）                         | `DWARFCompilationUnitData.DebugInfoOffset`     |
| AddressSize         | 0x0A | 1    | 地址大小（4/8）                                  | `DWARFCompilationUnitData.AddressSize`         |
| SegmentSelectorSize | 0x0B | 1    | 段选择子大小                                     | `DWARFCompilationUnitData.SegmentSelectorSize` |

### 调试信息条目（DIE - Debugging Information Entry）

| 字段             | 大小   | 说明                       | 对应类                            |
|------------------|--------|----------------------------|-----------------------------------|
| AbbreviationCode | LEB128 | 缩略码（0 表示兄弟链结束） | `DWARFEntryData.AbbreviationCode` |
| Tag              | 隐含   | 标签类型（由缩略码定义）   | `DWARFEntryData.Tag`              |
| HasChildren      | 1      | 是否有子条目               | `DWARFEntryData.HasChildren`      |
| Attributes       | 变长   | 属性列表                   | `DWARFEntryData.Attributes`       |

### 属性（Attribute）

| 字段  | 大小 | 说明                     | 对应类                     |
|-------|------|--------------------------|----------------------------|
| Name  | 隐含 | 属性名称（由缩略码定义） | `DWARFAttributeData.Name`  |
| Form  | 隐含 | 属性形式（编码方式）     | `DWARFAttributeData.Form`  |
| Value | 变长 | 属性值（根据 Form 编码） | `DWARFAttributeData.Value` |

### 常见属性形式（Form）

| Form                | 说明           | 编码                  |
|---------------------|----------------|-----------------------|
| DW_FORM_addr        | 地址           | 按 AddressSize        |
| DW_FORM_block       | 数据块         | ULEB128 长度 + 数据   |
| DW_FORM_block1/2/4  | 固定长度数据块 | 1/2/4 字节长度 + 数据 |
| DW_FORM_data1/2/4/8 | 无符号整数     | 1/2/4/8 字节          |
| DW_FORM_sdata       | 有符号整数     | SLEB128               |
| DW_FORM_udata       | 无符号整数     | ULEB128               |
| DW_FORM_string      | 字符串         | 以 null 结尾          |
| DW_FORM_strp        | 字符串指针     | 节区偏移              |
| DW_FORM_ref1/2/4/8  | 引用           | 1/2/4/8 字节偏移      |
| DW_FORM_flag        | 布尔值         | 1 字节                |
| DW_FORM_exprloc     | 表达式位置     | ULEB128 长度 + 数据   |

## 🏗️ 核心类

| 类                         | 说明           | 文件                                               |
|----------------------------|----------------|----------------------------------------------------|
| `DWARFCompilationUnitData` | DWARF 编译单元 | [Data/DWARFData.cs](Data/DWARFData.cs)             |
| `DWARFEntryData`           | 调试信息条目   | [Data/DWARFData.cs](Data/DWARFData.cs)             |
| `DWARFAttributeData`       | 属性数据       | [Data/DWARFData.cs](Data/DWARFData.cs)             |
| `DWARFDecoder`             | DWARF 解码器   | [Decode/DWARFDecoder.cs](Decode/DWARFDecoder.cs)   |
| `DWARFScanner`             | DWARF 扫描器   | [Scanner/DWARFScanner.cs](Scanner/DWARFScanner.cs) |

## 📚 格式规范参考

- [DWARF 调试信息格式标准](https://dwarfstd.org/)
- [DWARF 5 规范 PDF](https://dwarfstd.org/doc/DWARF5.pdf)
- [DWARF Tutorial](https://wiki.dwarfstd.org/)
