# 二进制源生成器

本文档描述 Nyar.Binary 的源生成器契约。源生成器在编译时将特性标记的 `partial struct` 转化为完全内联、零虚调用、零反射的读取/写入代码。

## 生成器契约

对每个 `[BinarySerializable] struct T`，源生成器生成如下方法：

### 同步方法

```csharp
public static bool TryRead(ref ByteBuffer buffer, out T value);
public void WriteTo(ref ByteBufferWriter writer);
public int GetSize();
```

**说明：**

| 方法 | 说明 |
|---|---|
| `TryRead` | 从缓冲区读取结构体，返回是否成功 |
| `WriteTo` | 将结构体写入缓冲区 |
| `GetSize` | 获取序列化后的大小，变长字段返回 `-1` |

### 异步方法（可选）

```csharp
public static ValueTask<T?> TryReadAsync(PipeReader reader, CancellationToken ct = default);
```

### 代数帧方法

对于 `[AlgebraicUnion]` 标记的类型，生成：

```csharp
public TResult Match<TResult>(Func<Case1, TResult> case1, Func<Case2, TResult> case2, ...);
public void Switch(Action<Case1> case1, Action<Case2> case2, ...);
```

### 偏移表方法

对于 `[OffsetTable]` 标记的类型，生成：

```csharp
public static bool TryRead(ref ByteBuffer buffer, out T value);
// 内部自动处理跳转和子结构体读取
```

---

## 生成器实现细节

### 基于 Roslyn 增量生成器

- 只依赖 `ReadOnlySpan<byte>` 和 `Span<byte>` 操作
- 不引入任何外部引用
- 支持增量编译，只重新生成修改的类型

### 零分配设计

- 所有生成的代码使用 `ref struct` 和 `Span<T>`
- 无装箱、无虚调用
- 完全内联可调试

### 用户可覆盖

生成的所有方法都是 `partial` 方法，用户可以在自己的 `partial struct` 中覆盖：

```csharp
[BinarySerializable]
public partial struct MyStruct
{
    [Field(Order = 0)]
    public int Value;

    // 用户覆盖生成的方法
    public static bool TryRead(ref ByteBuffer buffer, out MyStruct value)
    {
        // 自定义读取逻辑
    }
}
```

---

## 使用示例

### 基本读取

```csharp
var buffer = new ByteBuffer(rawData);
if (GlbHeader.TryRead(ref buffer, out var header))
{
    Console.WriteLine($"Version: {header.Version}");
    Console.WriteLine($"Length: {header.Length}");
}
```

### 基本写入

```csharp
var header = new GlbHeader
{
    Magic = new FixedBytes4("glTF"),
    Version = 2,
    Length = 1024
};

var writer = new ByteBufferWriter(buffer);
header.WriteTo(ref writer);
```

### 异步读取

```csharp
var reader = PipeReader.Create(stream);
var header = await GlbHeader.TryReadAsync(reader);

if (header.HasValue)
{
    Console.WriteLine($"Version: {header.Value.Version}");
}
```

### 代数帧匹配

```csharp
var frame = MysqlFrame.FromFrame(rawFrame);

frame.Match(
    ok => Console.WriteLine($"OK: {ok.AffectedRows}"),
    err => Console.WriteLine($"Error: {err.Message}"),
    eof => Console.WriteLine("EOF")
);
```

---

## 调试生成代码

生成的代码会输出到编译器的 `generated` 目录，可以在 IDE 中查看：

```
obj/
└── Debug/
    └── net11.0/
        └── generated/
            └── Nyar.Binary.Generator/
                └── BinarySerializableGenerator/
                    └── MyStruct.g.cs
```

在项目文件中启用输出：

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```