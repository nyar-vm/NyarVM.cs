# 📦 Acorn.Wav

WAV 音频格式编解码器。

## 📐 格式布局

### RIFF 容器头

| 字段      | 偏移 | 大小 | 说明         | 对应类                   |
|-----------|------|------|--------------|--------------------------|
| ChunkID   | 0x00 | 4    | `"RIFF"`     | `WavConstants.RiffMagic` |
| ChunkSize | 0x04 | 4    | 文件大小 - 8 | -                        |
| Format    | 0x08 | 4    | `"WAVE"`     | `WavConstants.WaveMagic` |

### fmt 子块

| 字段          | 大小 | 说明                   | 对应类                       |
|---------------|------|------------------------|------------------------------|
| SubChunkID    | 4    | `"fmt "`               | `WavConstants.FmtChunkId`    |
| SubChunkSize  | 4    | 子块大小（16 for PCM） | -                            |
| AudioFormat   | 2    | 格式标签               | `WavFormatTag`               |
| NumChannels   | 2    | 通道数                 | `WavAudioData.Channels`      |
| SampleRate    | 4    | 采样率                 | `WavAudioData.SampleRate`    |
| ByteRate      | 4    | 字节率                 | `WavAudioData.ByteRate`      |
| BlockAlign    | 2    | 块对齐                 | `WavAudioData.BlockAlign`    |
| BitsPerSample | 2    | 每样本位数             | `WavAudioData.BitsPerSample` |

### data 子块

| 字段         | 大小 | 说明         | 对应类                     |
|--------------|------|--------------|----------------------------|
| SubChunkID   | 4    | `"data"`     | `WavConstants.DataChunkId` |
| SubChunkSize | 4    | 数据大小     | -                          |
| Data         | N    | 音频采样数据 | `WavAudioData.SampleData`  |

## 🏗️ 核心类

| 类             | 说明             | 文件                                           |
|----------------|------------------|------------------------------------------------|
| `WavAudioData` | WAV 音频完整数据 | [Data/WavAudioData.cs](Data/WavAudioData.cs)   |
| `WavConstants` | WAV 常量         | [Data/WavConstants.cs](Data/WavConstants.cs)   |
| `WavFormatTag` | 格式标签枚举     | [Data/WavConstants.cs](Data/WavConstants.cs)   |
| `WavDecoder`   | WAV 解码器       | [Decode/WavDecoder.cs](Decode/WavDecoder.cs)   |
| `WavEncoder`   | WAV 编码器       | [Encode/WavEncoder.cs](Encode/WavEncoder.cs)   |
| `WavScanner`   | WAV 扫描器       | [Scanner/WavScanner.cs](Scanner/WavScanner.cs) |

## 📚 格式规范参考

- [Microsoft WAVE Audio File Format](https://docs.microsoft.com/en-us/windows/win32/multimedia/waveform-audio-file-format)
- [WAV File Format Wiki](https://en.wikipedia.org/wiki/WAV)
