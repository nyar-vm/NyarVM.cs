# Acorn.Coff

Acorn COFF 格式库，提供 Windows 目标文件（.obj）的扫描和解码功能。

## 功能特性

- **COFF 文件解码**：解析 Windows 目标文件格式
- **COFF 文件扫描**：快速扫描 COFF 文件结构
- **符号表解析**：提取符号信息
- **重定位解析**：提取重定位信息
- **支持多种架构**：x86, x64, ARM, ARM64, IA64
- **轻量级实现**：不依赖第三方库，纯 C# 实现
- **与 Acorn 核心集成**：使用 Acorn 核心的编解码接口

## 安装

```bash
dotnet add package Acorn.Coff
```

## 使用示例

### 解码 COFF 文件

```csharp
using Acorn.Coff.Decode;

var data = File.ReadAllBytes("example.obj");

var decoder = new CoffDecoder();
var coffFile = decoder.Decode(data);

Console.WriteLine($"Machine: {coffFile.Header.Machine}");
Console.WriteLine($"Sections: {coffFile.Header.NumberOfSections}");
Console.WriteLine($"Symbols: {coffFile.Symbols.Count}");

foreach (var section in coffFile.Sections)
{
    Console.WriteLine($"- {section.Name}: {section.SizeOfRawData} bytes");
}
```

### 扫描 COFF 文件

```csharp
using Acorn.Coff.Scanner;

var data = File.ReadAllBytes("example.obj");

var scanResult = CoffScanner.Scan(data);
Console.WriteLine(scanResult);
```

## 项目结构

- `Acorn.Coff/`
  - `Data/` - 数据结构
    - `CoffData.cs` - COFF 文件数据结构
  - `Decode/` - 解码器
    - `CoffDecoder.cs` - COFF 文件解码器
  - `Scanner/` - 扫描器
    - `CoffScanner.cs` - COFF 文件扫描器

## 支持的格式

- **COFF**：Windows 目标文件格式
- **OBJ**：目标文件

## 依赖

- .NET 11.0+
- Acorn.Core

## 许可证

MPL-2.0
