# 📦 Acorn.Onnx

Open Neural Network Exchange（ONNX）模型格式编解码器。

## 📐 格式布局

ONNX 使用 Protocol Buffers 编码，模型文件为二进制 protobuf 消息。

### ModelProto 结构

| 字段             | 编号 | 类型                     | 说明         | 对应类                          |
|------------------|------|--------------------------|--------------|---------------------------------|
| ir_version       | 1    | int64                    | ONNX IR 版本 | `OnnxModelData.IrVersion`       |
| opset_import     | 8    | OperatorSetIdProto[]     | 算子集导入   | `OnnxModelData.OpsetImport`     |
| producer_name    | 2    | string                   | 生产者名称   | `OnnxModelData.ProducerName`    |
| producer_version | 3    | string                   | 生产者版本   | `OnnxModelData.ProducerVersion` |
| domain           | 4    | string                   | 模型域       | `OnnxModelData.Domain`          |
| model_version    | 5    | int64                    | 模型版本     | `OnnxModelData.ModelVersion`    |
| doc_string       | 6    | string                   | 文档字符串   | `OnnxModelData.DocString`       |
| graph            | 7    | GraphProto               | 计算图       | `OnnxModelData.Graph`           |
| metadata_props   | 14   | StringStringEntryProto[] | 元数据属性   | `OnnxModelData.CustomMetadata`  |

### GraphProto 结构

| 字段        | 编号 | 类型             | 说明       | 对应类                     |
|-------------|------|------------------|------------|----------------------------|
| name        | 1    | string           | 图名称     | `OnnxGraph.Name`           |
| node        | 2    | NodeProto[]      | 节点列表   | `OnnxGraph.Node`           |
| initializer | 5    | TensorProto[]    | 初始化张量 | `OnnxGraph.Initialization` |
| doc_string  | 10   | string           | 文档字符串 | `OnnxGraph.DocString`      |
| input       | 11   | ValueInfoProto[] | 输入定义   | `OnnxGraph.Input`          |
| output      | 12   | ValueInfoProto[] | 输出定义   | `OnnxGraph.Output`         |
| value_info  | 13   | ValueInfoProto[] | 中间值信息 | `OnnxGraph.ValueInfo`      |

### NodeProto 结构

| 字段       | 编号 | 类型             | 说明         | 对应类               |
|------------|------|------------------|--------------|----------------------|
| name       | 1    | string           | 节点名称     | `OnnxNode.Name`      |
| op_type    | 2    | string           | 算子类型     | `OnnxNode.OpType`    |
| domain     | 7    | string           | 算子域       | `OnnxNode.Domain`    |
| input      | 3    | string[]         | 输入名称列表 | `OnnxNode.Input`     |
| output     | 4    | string[]         | 输出名称列表 | `OnnxNode.Output`    |
| attribute  | 5    | AttributeProto[] | 属性列表     | `OnnxNode.Attribute` |
| doc_string | 6    | string           | 文档字符串   | `OnnxNode.DocString` |

### TensorProto 结构

| 字段        | 编号 | 类型     | 说明                     | 对应类                  |
|-------------|------|----------|--------------------------|-------------------------|
| dims        | 1    | int64[]  | 张量形状                 | `OnnxTensor.Dims`       |
| data_type   | 2    | int32    | 数据类型（OnnxDataType） | `OnnxTensor.DataType`   |
| raw_data    | 9    | bytes    | 原始字节数据             | `OnnxTensor.RawData`    |
| float_data  | 4    | float[]  | float 数据（重复字段）   | `OnnxTensor.FloatData`  |
| int32_data  | 5    | int32[]  | int32 数据（重复字段）   | `OnnxTensor.Int32Data`  |
| int64_data  | 7    | int64[]  | int64 数据（重复字段）   | `OnnxTensor.Int64Data`  |
| double_data | 10   | double[] | double 数据（重复字段）  | `OnnxTensor.DoubleData` |

### 数据类型（OnnxDataType）

| 值 | 名称     | 说明             |
|----|----------|------------------|
| 1  | FLOAT    | 32 位浮点数      |
| 2  | UINT8    | 无符号 8 位整数  |
| 3  | INT8     | 有符号 8 位整数  |
| 6  | INT32    | 有符号 32 位整数 |
| 7  | INT64    | 有符号 64 位整数 |
| 10 | FLOAT16  | 16 位浮点数      |
| 11 | DOUBLE   | 64 位浮点数      |
| 16 | BFLOAT16 | 脑浮点 16 位     |

## 🏗️ 核心类

| 类                  | 说明              | 文件                                             |
|---------------------|-------------------|--------------------------------------------------|
| `OnnxModelData`     | ONNX 模型完整数据 | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxGraph`         | ONNX 计算图       | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxNode`          | ONNX 计算节点     | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxTensor`        | ONNX 张量         | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxAttribute`     | ONNX 节点属性     | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxValueInfo`     | ONNX 值信息       | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxOperatorSetId` | ONNX 算子集标识   | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxDataType`      | ONNX 数据类型枚举 | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxAttributeType` | ONNX 属性类型枚举 | [Data/OnnxModelData.cs](Data/OnnxModelData.cs)   |
| `OnnxDecoder`       | ONNX 模型解码器   | [Decode/OnnxDecoder.cs](Decode/OnnxDecoder.cs)   |
| `OnnxEncoder`       | ONNX 模型编码器   | [Encode/OnnxEncoder.cs](Encode/OnnxEncoder.cs)   |
| `OnnxScanner`       | ONNX 模型扫描器   | [Scanner/OnnxScanner.cs](Scanner/OnnxScanner.cs) |

## 📚 格式规范参考

- [ONNX Specification](https://onnx.ai/onnx/intro/)
- [ONNX Protobuf Definitions](https://github.com/onnx/onnx/blob/main/onnx/onnx.proto3)
- [ONNX Operators](https://onnx.ai/onnx/operators/)
