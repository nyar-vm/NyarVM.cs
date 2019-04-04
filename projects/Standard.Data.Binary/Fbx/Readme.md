# 📦 Acorn.Fbx

Autodesk FBX 模型/动画格式编解码器。

## 📐 格式布局

### FBX 二进制文件头

| 字段    | 偏移 | 大小 | 说明                                       | 对应类                     |
|---------|------|------|--------------------------------------------|----------------------------|
| Magic   | 0x00 | 23   | `"Kaydara FBX Binary\x20\x0A\x00\x1A\x00"` | `FbxConstants.BinaryMagic` |
| Version | 0x17 | 4    | 版本号                                     | `FbxVersions`              |

### FBX 节点记录

| 字段            | 大小 | 说明         | 对应类               |
|-----------------|------|--------------|----------------------|
| EndOffset       | 4    | 节点结束偏移 | -                    |
| NumProperties   | 4    | 属性数量     | -                    |
| PropertyListLen | 4    | 属性列表长度 | -                    |
| NameLen         | 1    | 名称长度     | -                    |
| Name            | N    | 节点名称     | `FbxNode.Name`       |
| PropertyList    | N    | 属性列表     | `FbxNode.Properties` |
| Children        | N    | 子节点       | `FbxNode.Children`   |

### FBX 属性类型

| 类型代码 | 说明      | 对应类                                |
|----------|-----------|---------------------------------------|
| 'C'      | Boolean   | `FbxConstants.PropertyType.Boolean`   |
| 'Y'      | Int8      | `FbxConstants.PropertyType.Int8`      |
| 'h'      | Int16     | `FbxConstants.PropertyType.Int16`     |
| 'i'      | Int32     | `FbxConstants.PropertyType.Int32`     |
| 'l'      | Int64     | `FbxConstants.PropertyType.Int64`     |
| 'f'      | Float32   | `FbxConstants.PropertyType.Float32`   |
| 'd'      | Float64   | `FbxConstants.PropertyType.Float64`   |
| 'S'      | String    | `FbxConstants.PropertyType.String`    |
| 'R'      | RawBuffer | `FbxConstants.PropertyType.RawBuffer` |

## 🏗️ 核心类

| 类             | 说明             | 文件                                           |
|----------------|------------------|------------------------------------------------|
| `FbxFileData`  | FBX 文件完整数据 | [Data/FbxFileData.cs](Data/FbxFileData.cs)     |
| `FbxNode`      | FBX 节点         | [Data/FbxFileData.cs](Data/FbxFileData.cs)     |
| `FbxProperty`  | FBX 属性         | [Data/FbxFileData.cs](Data/FbxFileData.cs)     |
| `FbxConstants` | FBX 常量         | [Data/FbxConstants.cs](Data/FbxConstants.cs)   |
| `FbxDecoder`   | FBX 解码器       | [Decode/FbxDecoder.cs](Decode/FbxDecoder.cs)   |
| `FbxScanner`   | FBX 扫描器       | [Scanner/FbxScanner.cs](Scanner/FbxScanner.cs) |

## 📚 格式规范参考

- [FBX SDK Documentation](https://help.autodesk.com/view/FBX/2020/ENU/)
- [FBX Binary File Format](https://code.blender.org/2013/08/fbx-binary-file-format-specification/)
