# 📝 二进制特性系统

本文档描述 Nyar.Binary 的声明式特性系统。用户仅需编写 `partial struct` 并标注特性，源生成器负责剩余所有工作。

## 核心特性

### BinarySerializableAttribute

标记一个结构体为二进制可序列化类型。

```csharp
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinarySerializableAttribute : Attribute
{
    public Endianness Endianness { get; set; }
    public int ExplicitAlignment { get; set; } = 0;
}
```

**参数说明：**

| 参数 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Endianness` | `Endianness` | `Little` | 字节序（大端/小端） |
| `ExplicitAlignment` | `int` | `0` | 显式对齐字节数，0 表示自动 |

**示例：**

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct GlbHeader
{
    // ...
}
```

---

### FieldAttribute

标记结构体中的字段，指定序列化行为。

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class FieldAttribute : Attribute
{
    public int Order { get; set; }
    public int Length { get; set; } = -1;
    public string LengthField { get; set; }
    public string ConditionalOn { get; set; }
    public bool Optional { get; set; }
    public Endianness Endianness { get; set; }
    public string Encoding { get; set; } = "utf-8";
}
```

**参数说明：**

| 参数 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `Order` | `int` | - | 序列化顺序（必填） |
| `Length` | `int` | `-1` | 固定长度（用于数组/字符串） |
| `LengthField` | `string` | `null` | 变长数组的长度来源字段名 |
| `ConditionalOn` | `string` | `null` | 条件存在：指向 `bool` 属性/方法名 |
| `Optional` | `bool` | `false` | 是否可选字段 |
| `Endianness` | `Endianness` | 继承自结构体 | 局部覆盖字节序 |
| `Encoding` | `string` | `"utf-8"` | 字符串编码 |

**示例：**

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct MyStruct
{
    [Field(Order = 0, Length = 4)]
    public FixedBytes4 Magic;

    [Field(Order = 1)]
    public uint Count;

    [Field(Order = 2, LengthField = nameof(Count))]
    public byte[] Data;

    [Field(Order = 3, ConditionalOn = nameof(HasExtra))]
    public byte[] Extra;

    public bool HasExtra => Count > 100;
}
```

---

### BitFieldAttribute

标记位域字段，用于位级压缩。

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class BitFieldAttribute : Attribute
{
    public int BitOffset { get; set; }
    public int BitCount { get; set; }
}
```

**参数说明：**

| 参数 | 类型 | 说明 |
|---|---|---|
| `BitOffset` | `int` | 起始位（0 = 最低位） |
| `BitCount` | `int` | 占用位数（1 – 64） |

**示例：**

```csharp
[BinarySerializable]
public partial struct PackedFlags
{
    [Field(Order = 0)]
    public byte Raw;

    [BitField(BitOffset = 0, BitCount = 1)]
    public bool IsValid;

    [BitField(BitOffset = 1, BitCount = 3)]
    public byte Mode;

    [BitField(BitOffset = 4, BitCount = 4)]
    public byte Reserved;
}
```

---

### ArrayLengthFromAttribute

标记数组长度来源于另一个字段。

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class ArrayLengthFromAttribute : Attribute
{
    public string FieldName { get; set; }
}
```

**示例：**

```csharp
[BinarySerializable]
public partial struct ArrayWrapper
{
    [Field(Order = 0)]
    public int Count;

    [Field(Order = 1)]
    [ArrayLengthFrom(nameof(Count))]
    public int[] Items;
}
```

---

### OffsetTableAttribute

标记偏移表字段，用于复杂文件格式解析。

```csharp
[AttributeUsage(AttributeTargets.Struct)]
public sealed class OffsetTableAttribute : Attribute
{
    public string OffsetField { get; set; }
    public Type TargetType { get; set; }
    public string RelativeTo { get; set; } = "start";
}
```

**参数说明：**

| 参数 | 类型 | 默认值 | 说明 |
|---|---|---|---|
| `OffsetField` | `string` | - | 引用存储偏移的字段名 |
| `TargetType` | `Type` | - | 目标结构体类型 |
| `RelativeTo` | `string` | `"start"` | 偏移基准：start / header_end / etc. |

**示例：**

```csharp
[BinarySerializable(Endianness = Endianness.Little)]
public partial struct Live2DFile
{
    [Field(Order = 0)]
    public uint Version;

    [Field(Order = 1)]
    public uint SectionOffset;

    [OffsetTable(OffsetField = nameof(SectionOffset), TargetType = typeof(SectionTable))]
    public SectionTable Sections;
}
```

---

### AlgebraicUnionAttribute

标记代数联合类型（判别联合）。

```csharp
[AttributeUsage(AttributeTargets.Struct)]
public sealed class AlgebraicUnionAttribute : Attribute
{
    public string DiscriminatorField { get; set; }
    public Type[] Cases { get; set; }
}
```

**参数说明：**

| 参数 | 类型 | 说明 |
|---|---|---|
| `DiscriminatorField` | `string` | 哪个字段存储类型标签 |
| `Cases` | `Type[]` | 可能的子类型 |

**示例：**

```csharp
[BinarySerializable]
[AlgebraicUnion(
    DiscriminatorField = nameof(Type),
    Cases = new[] { typeof(OkFrame), typeof(ErrorFrame), typeof(EofFrame) }
)]
public partial struct MysqlFrame
{
    [Field(Order = 0)]
    public FrameType Type;

    [Field(Order = 1, Optional = true)]
    public OkFrame Ok;

    [Field(Order = 2, Optional = true)]
    public ErrorFrame Err;

    [Field(Order = 3, Optional = true)]
    public EofFrame Eof;
}
```

---

## 支持的声明式场景一览

| 场景 | 特性组合 | 生成行为 |
|---|---|---|
| 简单顺序字段 | `BinarySerializable` + `Field(Order=…)` | 依次读取/写入 |
| 固定长度数组/字符串 | `Field(Length=…)` | 读取指定字节数 |
| 变长数组（长度前缀） | `Field(LengthField="Len")` | 先读长度，再读数组 |
| 条件存在字段 | `Field(ConditionalOn="HasX")` | 调用 `bool HasX` 判断是否读取 |
| 位域 | `BitField(BitOffset=…, BitCount=…)` | 生成位运算提取/插入 |
| 偏移表 | `OffsetTableAttribute` | 先读头部，再跳转 Position 读取目标 |
| 代数帧（判别联合） | `AlgebraicUnion` + 各 case 类型 | 根据判别符读取相应子帧 |

---

## 注意事项

> **所有特性均不包含字符串表达式求值**（除 `ConditionalOn` 使用简单的属性名比较），复杂条件由用户实现的 `bool ShouldSerialize()` 方法处理，生成器会调用。