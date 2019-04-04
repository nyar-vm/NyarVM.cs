# 📦 Acorn.SafeTensors

HuggingFace SafeTensors 安全张量格式编解码器。

## 📐 格式布局

SafeTensors 格式将模型权重存储为 JSON 头 + 二进制张量数据，避免 pickle 的安全风险。

### 文件结构

| 部分         | 偏移   | 大小 | 说明                                   | 对应类                            |
|--------------|--------|------|----------------------------------------|-----------------------------------|
| HeaderLength | 0x00   | 8    | JSON 头长度（小端序 uint64，最大 8MB） | `SafeTensorsFileData`（内部解析） |
| HeaderJSON   | 0x08   | N    | UTF-8 JSON 头                          | `SafeTensorsFileData.Tensors`     |
| TensorData   | 对齐后 | N    | 二进制张量数据                         | `SafeTensorsFileData.Data`        |

### JSON 头结构

```json
{
  "tensor_name": {
    "dtype": "float32",
    "shape": [512, 768],
    "data_offsets": [0, 1572864]
  },
  "__metadata__": {
    "format": "pt"
  }
}
```

### 张量元数据字段

| 字段         | 类型   | 说明                         | 对应类                                     |
|--------------|--------|------------------------------|--------------------------------------------|
| dtype        | string | 数据类型                     | `SafeTensorMeta.DType`                     |
| shape        | int[]  | 张量形状                     | `SafeTensorMeta.Shape`                     |
| data_offsets | int[2] | 数据在文件中的起始和结束偏移 | `SafeTensorMeta.DataOffset` / `DataLength` |

### 数据类型（DType）

| 名称     | 说明             | 大小 |
|----------|------------------|------|
| BOOL     | 布尔             | 1    |
| UINT8    | 无符号 8 位整数  | 1    |
| INT8     | 有符号 8 位整数  | 1    |
| INT16    | 有符号 16 位整数 | 2    |
| INT32    | 有符号 32 位整数 | 4    |
| INT64    | 有符号 64 位整数 | 8    |
| FLOAT16  | 16 位浮点数      | 2    |
| FLOAT32  | 32 位浮点数      | 4    |
| FLOAT64  | 64 位浮点数      | 8    |
| BFLOAT16 | 脑浮点 16 位     | 2    |

## 🏗️ 核心类

| 类                    | 说明                     | 文件                                                           |
|-----------------------|--------------------------|----------------------------------------------------------------|
| `SafeTensorsFileData` | SafeTensors 文件完整数据 | [Data/SafeTensorsFileData.cs](Data/SafeTensorsFileData.cs)     |
| `SafeTensorMeta`      | 张量元数据               | [Data/SafeTensorsFileData.cs](Data/SafeTensorsFileData.cs)     |
| `SafeTensorData`      | 张量数据（含实际值）     | [Data/SafeTensorsFileData.cs](Data/SafeTensorsFileData.cs)     |
| `SafeTensorDType`     | 数据类型枚举             | [Data/SafeTensorsFileData.cs](Data/SafeTensorsFileData.cs)     |
| `SafeTensorsDecoder`  | SafeTensors 解码器       | [Decode/SafeTensorsDecoder.cs](Decode/SafeTensorsDecoder.cs)   |
| `SafeTensorsEncoder`  | SafeTensors 编码器       | [Encode/SafeTensorsEncoder.cs](Encode/SafeTensorsEncoder.cs)   |
| `SafeTensorsScanner`  | SafeTensors 扫描器       | [Scanner/SafeTensorsScanner.cs](Scanner/SafeTensorsScanner.cs) |

## 📚 格式规范参考

- [HuggingFace SafeTensors Specification](https://huggingface.co/docs/safetensors/index)
- [SafeTensors GitHub](https://github.com/huggingface/safetensors)
- [SafeTensors Format](https://huggingface.co/docs/safetensors/format)
