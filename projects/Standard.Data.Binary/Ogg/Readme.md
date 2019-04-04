# 📦 Acorn.Ogg

OGG/Vorbis 音频格式编解码器。

## 📐 格式布局

### OGG 页面头

| 字段               | 偏移 | 大小 | 说明        | 对应类                        |
|--------------------|------|------|-------------|-------------------------------|
| CapturePattern     | 0x00 | 4    | `"OggS"`    | `OggConstants.CapturePattern` |
| Version            | 0x04 | 1    | 版本号（0） | `OggConstants.Version`        |
| Flags              | 0x05 | 1    | 页面标志位  | `OggPageFlags`                |
| GranulePosition    | 0x06 | 8    | 颗粒位置    | -                             |
| SerialNumber       | 0x0E | 4    | 串行号      | -                             |
| PageSequenceNumber | 0x12 | 4    | 页面序号    | -                             |
| Checksum           | 0x16 | 4    | CRC 校验    | -                             |
| SegmentCount       | 0x1A | 1    | 段数量      | -                             |
| SegmentTable       | 0x1B | N    | 段大小表    | -                             |

### Vorbis 标识头

| 字段           | 偏移 | 大小 | 说明               |
|----------------|------|------|--------------------|
| PacketType     | 0    | 1    | 包类型（1 = 标识） |
| VorbisId       | 1    | 6    | "vorbis"           |
| Version        | 7    | 4    | Vorbis 版本        |
| Channels       | 11   | 1    | 通道数             |
| SampleRate     | 12   | 4    | 采样率             |
| BitrateMaximum | 16   | 4    | 最大比特率         |
| NominalBitrate | 20   | 4    | 名义比特率         |
| MinimumBitrate | 24   | 4    | 最小比特率         |

## 🏗️ 核心类

| 类             | 说明             | 文件                                           |
|----------------|------------------|------------------------------------------------|
| `OggAudioData` | OGG 音频完整数据 | [Data/OggAudioData.cs](Data/OggAudioData.cs)   |
| `OggConstants` | OGG 常量         | [Data/OggConstants.cs](Data/OggConstants.cs)   |
| `OggCodecType` | 编解码类型枚举   | [Data/OggConstants.cs](Data/OggConstants.cs)   |
| `OggDecoder`   | OGG 解码器       | [Decode/OggDecoder.cs](Decode/OggDecoder.cs)   |
| `OggScanner`   | OGG 扫描器       | [Scanner/OggScanner.cs](Scanner/OggScanner.cs) |

## 📚 格式规范参考

- [OGG Container Format](https://xiph.org/ogg/doc/framing.html)
- [Vorbis I Specification](https://xiph.org/vorbis/doc/Vorbis_I_spec.html)
- [Opus Specification](https://opus-codec.org/docs/)
