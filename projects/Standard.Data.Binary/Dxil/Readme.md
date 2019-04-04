# 📦 Acorn.Dxil

DXIL（DirectX Intermediate Language）/ DXContainer 着色器中间表示编解码器。

## 📐 格式布局

### DXContainer 文件头（20 字节）

| 字段          | 偏移 | 大小 | 说明                          | 对应类                           |
|---------------|------|------|-------------------------------|----------------------------------|
| MagicNumber   | 0x00 | 4    | `0x44434247`（"DXBC" 小端序） | `DxContainerHeader.MagicNumber`  |
| Version.Major | 0x04 | 2    | 容器格式主版本号              | `DxContainerHeader.VersionMajor` |
| Version.Minor | 0x06 | 2    | 容器格式次版本号              | `DxContainerHeader.VersionMinor` |
| FileSize      | 0x08 | 4    | 文件总大小                    | `DxContainerHeader.FileSize`     |
| PartCount     | 0x0C | 4    | Part 数量                     | `DxContainerHeader.PartCount`    |

### DXContainer Part 头（8 字节）

| 字段   | 偏移 | 大小 | 说明                          | 对应类                         |
|--------|------|------|-------------------------------|--------------------------------|
| FourCC | 0x00 | 4    | Part 类型标识（4 字符 ASCII） | `DxContainerPartHeader.FourCC` |
| Size   | 0x04 | 4    | Part 数据大小（不含 Part 头） | `DxContainerPartHeader.Size`   |

### DXIL Part 内部结构

| 字段          | 偏移 | 大小 | 说明                    | 对应类              |
|---------------|------|------|-------------------------|---------------------|
| ProgramHeader | 0x00 | 24   | 着色器程序头            | `DxilProgramHeader` |
| BitcodeHeader | 0x18 | 8    | LLVM Bitcode 偏移与大小 | `DxilBitcodeHeader` |
| Bitcode       | 变长 | 变长 | LLVM 3.7 Bitcode 数据   | -                   |

### DXIL Program Header（24 字节）

| 字段            | 偏移 | 大小 | 说明                                |
|-----------------|------|------|-------------------------------------|
| MajorVersion    | 0x00 | 1    | DXIL 主版本号                       |
| MinorVersion    | 0x01 | 1    | DXIL 次版本号                       |
| ShaderModelKind | 0x02 | 1    | 着色器模型类型                      |
| Padding         | 0x03 | 1    | 对齐填充                            |
| Size            | 0x04 | 4    | DXIL 数据总大小（含 ProgramHeader） |
| BitcodeOffset   | 0x08 | 4    | Bitcode 相对偏移                    |
| BitcodeSize     | 0x0C | 4    | Bitcode 大小                        |

### 常见 Part FourCC

| FourCC | 说明                |
|--------|---------------------|
| DXIL   | DXIL 着色器程序     |
| DXIL1  | DXIL 1.x 着色器程序 |
| ILDB   | 调试信息            |
| SFI0   | 着色器特征标志      |
| HASH   | 着色器哈希          |
| PSV0   | 管线状态验证        |
| RDAT   | 运行时数据          |
| STAT   | 着色器统计          |

## 🏗️ 核心类

| 类                      | 说明                 | 文件                         |
|-------------------------|----------------------|------------------------------|
| `DxilConstants`         | DXIL 常量            | Data/DxilConstants.cs        |
| `DxContainerHeader`     | DXContainer 文件头   | Data/DxContainerData.cs      |
| `DxContainerPartHeader` | DXContainer Part 头  | Data/DxContainerData.cs      |
| `DxContainerData`       | DXContainer 完整数据 | Data/DxContainerData.cs      |
| `DxilProgramHeader`     | DXIL 程序头          | Data/DxilProgramData.cs      |
| `DxilOpCode`            | DXIL 操作码枚举      | Data/DxilConstants.cs        |
| `DxContainerDecoder`    | DXContainer 解码器   | Decode/DxContainerDecoder.cs |
| `DxContainerEncoder`    | DXContainer 编码器   | Encode/DxContainerEncoder.cs |
| `DxilScanner`           | DXIL 扫描器          | Scanner/DxilScanner.cs       |

## 📚 格式规范参考

- [DXIL Specification](https://github.com/microsoft/DirectXShaderCompiler/blob/main/docs/DXIL.rst)
- [DXContainer Format](https://llvm.org/docs/DirectX/DXContainer.html)
- [LLVM Bitcode Format](https://llvm.org/docs/BitCodeFormat.html)
