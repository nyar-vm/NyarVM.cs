# 📦 Acorn.LLVM

LLVM Bitcode（`.bc`）二进制格式编解码器。

## 📐 格式布局

### LLVM Bitcode 文件头

| 字段    | 偏移 | 大小 | 说明                                  | 对应类                  |
|---------|------|------|---------------------------------------|-------------------------|
| Magic   | 0x00 | 4    | `0x42 0x43 0xC0 0xDE`（"BC\xC0\xDE"） | `LLVMMagicData.Magic`   |
| Version | 0x04 | 2    | LLVM 版本号                           | `LLVMMagicData.Version` |

### Bitcode 块结构

LLVM Bitcode 使用基于块的编码，每个块包含记录和子块。

| 元素        | 说明         | 对应类                    |
|-------------|--------------|---------------------------|
| ENTER_BLOCK | 块开始标记   | `LLVMBlockData`           |
| BlockID     | 块标识符     | `LLVMBlockData.BlockID`   |
| BlockSize   | 块大小（位） | `LLVMBlockData.BlockSize` |
| Records     | 记录列表     | `LLVMBlockData.Records`   |
| SubBlocks   | 子块列表     | `LLVMBlockData.SubBlocks` |
| END_BLOCK   | 块结束标记   | -                         |

### 记录结构

| 字段     | 说明                   | 对应类                    |
|----------|------------------------|---------------------------|
| Code     | 记录代码（操作码）     | `LLVMRecordData.Code`     |
| Operands | 操作数列表（变长整数） | `LLVMRecordData.Operands` |

### 标准块类型

| BlockID | 名称                      | 说明       |
|---------|---------------------------|------------|
| 0       | BLOCKINFO                 | 块信息     |
| 8       | MODULE_BLOCK              | LLVM 模块  |
| 9       | PARAMATTR_BLOCK           | 参数属性   |
| 10      | PARAMATTR_GROUP_BLOCK     | 参数属性组 |
| 11      | CONSTANTS_BLOCK           | 常量       |
| 12      | FUNCTION_BLOCK            | 函数       |
| 13      | TYPE_BLOCK                | 类型系统   |
| 14      | VALUE_SYMTAB_BLOCK        | 值符号表   |
| 15      | METADATA_BLOCK            | 元数据     |
| 16      | METADATA_ATTACHMENT_BLOCK | 元数据附件 |

## 🏗️ 核心类

| 类                | 说明                  | 文件                                               |
|-------------------|-----------------------|----------------------------------------------------|
| `LLVMBitcodeData` | LLVM 位码文件完整数据 | [Data/LLVMBitcodeData.cs](Data/LLVMBitcodeData.cs) |
| `LLVMMagicData`   | 魔数和标识数据        | [Data/LLVMBitcodeData.cs](Data/LLVMBitcodeData.cs) |
| `LLVMBlockData`   | 块数据                | [Data/LLVMBitcodeData.cs](Data/LLVMBitcodeData.cs) |
| `LLVMRecordData`  | 记录数据              | [Data/LLVMBitcodeData.cs](Data/LLVMBitcodeData.cs) |
| `LLVMModuleData`  | 模块数据              | [Data/LLVMBitcodeData.cs](Data/LLVMBitcodeData.cs) |
| `LLVMDecoder`     | LLVM 位码解码器       | [Decode/LLVMDecoder.cs](Decode/LLVMDecoder.cs)     |
| `LLVMScanner`     | LLVM 位码扫描器       | [Scanner/LLVMScanner.cs](Scanner/LLVMScanner.cs)   |

## 📚 格式规范参考

- [LLVM Bitcode File Format](https://llvm.org/docs/BitCodeFormat.html)
- [LLVM Language Reference](https://llvm.org/docs/LangRef.html)
