# Sonic.Audio 功能规划

**版本**: 0.1  
**状态**: 规划中  
**日期**: 2026-05-30

## 1. 目标

`Sonic.Audio` 是建立在 `Sonic.Core.Media` 与 `Sonic.Standard.Media` 之上的音频子域包，负责构建对标 `FFmpeg` 音频链路、
`SoX` / `librosa` 式音频处理与分析能力的统一运行时。

核心原则:

- `Sonic.Core.Media` 负责音频基础契约，`Sonic.Standard.Media` 负责标准缓冲实现，`Sonic.Audio` 负责更高层的音频子域能力。
- 音频格式编解码统一委托给 `Acorn`，不得在 `Sonic.Audio` 中重复实现 `WAV`/`FLAC`/`Ogg`/`Opus` 等格式细节。
- `FFmpeg` 的“容器/码流/编解码格式”能力归 `Acorn` 或 `FFmpeg` 桥接扩展；`FFmpeg` 的“滤镜图/重采样/媒体编排”能力归
  `Sonic.Audio`。
- `DL/ML`、语音识别、语音合成、音乐生成等智能能力不进入核心包，通过扩展形式接入。

## 1.1 在 `valkyrie.v` 生态中的位置

`Sonic` 当前这套媒体分层，应被视为未来 `valkyrie.v` 生态的预演。

| 当前项目               | 未来映射                 | 定位                          |
|:-----------------------|:-------------------------|:------------------------------|
| `Sonic.Core`           | `core.v`                 | 基础契约与核心原语            |
| `Sonic.Standard`       | `std.v`                  | 标准实现与默认运行时能力      |
| `Sonic.Core.Media`     | `core.v::media`          | 媒体公共抽象与最低层模态契约  |
| `Sonic.Standard.Media` | `std.v::media`           | 标准图像/音频/视频默认实现    |
| `Sonic.Audio`          | `std.v` 或上层媒体子域包 | DSP、分析、格式门面、扩展宿主 |

## 2. 与其他媒体子系统的边界

| 项目          | 核心职责                                   | 典型能力                                    |
|:--------------|:-------------------------------------------|:--------------------------------------------|
| `Sonic.Image` | 单帧图像、动画图像、静态视觉               | `OpenCV imgproc` / `features2d` / `calib3d` |
| `Sonic.Audio` | 音频帧、DSP、分析、滤镜图、重采样混音      | `FFmpeg` 音频链路 + `SoX` / `librosa`       |
| `Sonic.Video` | 视频帧序列、时域处理、转码编排、音视频同步 | `FFmpeg` 视频链路 + `OpenCV video`          |

统一原则:

- `Acorn` 是媒体二进制格式的唯一 Source
- `Sonic.Audio` 不手写媒体文件格式和码流规范
- `Sonic.Video` 负责音视频同步与时间轴编排，`Sonic.Audio` 负责纯音频侧处理
- `Acorn` 不承载 `Format Handler` 抽象、`Format Detector` 抽象和 `Registry`

### 2.1 `Sonic.Core` / `Sonic.Standard` 归位

音频侧的基础分层应以 `Sonic.Core.Media` 和 `Sonic.Standard.Media` 为准，而不是继续拆新的 `Sonic.Audio.Abstractions`。

`Sonic.Audio` 侧的归位目标如下:

| 旧位置                                              | 动作                                   | 新位置                 |
|:----------------------------------------------------|:---------------------------------------|:-----------------------|
| `projects/Sonic.Core/Media/Audio/IAudio.cs`         | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/Audio/AudioAttribute.cs` | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/SampleFormat.cs`         | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/ChannelLayout.cs`        | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Standard/Media/Audio.cs`            | 标准缓冲实现并待增强                   | `Sonic.Standard.Media` |
| `projects/Sonic.Core/Media/IFilterGraph.cs`         | 保持在媒体公共层，后续按模态细化       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IMediaDecoder.cs`        | 保持在媒体公共层，具体格式门面上移     | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IMediaEncoder.cs`        | 保持在媒体公共层，具体格式门面上移     | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IHardwareCodec.cs`       | 保持在媒体公共层，具体桥接在上层扩展   | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IDeviceContext.cs`       | 保持在媒体公共层，设备细节在扩展层落地 | `Sonic.Core.Media`     |

迁移后依赖规则:

- `Sonic.Core.Media` 不依赖 `Sonic.Standard` / `Sonic.Image` / `Sonic.Audio` / `Sonic.Video`
- `Sonic.Standard.Media` 依赖 `Sonic.Core.Media`
- `Sonic.Audio` 依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- 音视频同步相关抽象不放入 `Sonic.Audio` 的最低层
- `Sonic.Core.Media` 保留为真正的媒体公共层

### 2.2 统一媒体项目拓扑

为和 `Sonic.Image`、`Sonic.Video` 保持一致，建议采用如下媒体项目拓扑:

| 项目                   | 职责                                 | 允许依赖                                              |
|:-----------------------|:-------------------------------------|:------------------------------------------------------|
| `Sonic.Core.Media`     | 媒体公共抽象与最低层契约             | 无标准实现依赖                                        |
| `Sonic.Standard.Media` | 标准图像/音频/视频默认实现           | `Sonic.Core.Media`                                    |
| `Sonic.Image`          | 图像处理、静态视觉、格式门面         | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |
| `Sonic.Audio`          | DSP、分析、滤镜图、格式门面          | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |
| `Sonic.Video`          | 时域处理、编排、`A/V Sync`、格式门面 | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |

## 3. 对标范围

### 3.1 对标 `FFmpeg` 的音频能力

`Sonic.Audio` 对标以下能力域:

- 音频帧与样本格式模型
- 声道布局与重排
- 重采样、重格式化、重混音
- 音频滤镜图
- 音量、均衡器、压缩器、限制器、噪声门
- 时域处理与频域处理
- 分段、拼接、淡入淡出、归一化
- 波形、频谱、梅尔频谱、STFT、倒谱等分析能力

### 3.2 不纳入核心包的部分

- 音频文件格式二进制规范实现
- 音频设备驱动、系统播放后端、系统录音后端
- 语音识别模型、TTS 模型、音乐生成模型
- DAW 工程文件、完整非线性编辑器

## 4. 职责边界

`Sonic.Audio` 应只负责以下内容:

- 基于 `Sonic.Core.Media` 与 `Sonic.Standard.Media` 扩展音频子域能力，而不是重新定义最低层抽象
- 音频内存模型: 样本缓冲、平面/交错布局、时间范围、片段与流
- DSP: 重采样、重混音、滤波、卷积、均衡、动态处理
- 分析: FFT、STFT、频谱、梅尔频谱、响度、节拍、特征提取
- 图编排: `FilterGraph`、音频处理管线、批处理与流式处理
- 编解码适配: `load` / `save` / `transcode` 门面，但格式细节委托给 `Acorn`

`Sonic.Audio` 不负责以下内容:

- 自建 `WAV`/`FLAC`/`Ogg`/`Opus`/`MP3`/`AAC` 文件格式规范
- 自建容器 mux/demux bitstream 解析器
- 视频时间轴与 A/V 同步
- 深度学习训练、推理框架和模型管理
- 音频设备驱动和跨平台底层播放抽象

## 5. 与 `Acorn` 和 `FFmpeg` 的关系

### 5.1 架构分层

建议采用如下分层:

```text
Sonic.Core.Media
  ├── 音频抽象与媒体公共原语
  └── `IAudio` / `SampleFormat` / `ChannelLayout` / `IFilterGraph`

Sonic.Standard.Media
  ├── `AudioBuffer<T>`
  └── 后续 `AudioFrame` / `AudioClip`

Sonic.Audio
  ├── DSP / Analysis / FilterGraph
  ├── AudioIO / AudioOps / AudioAnalysis / AudioFormatRegistry
  └── 上层音频子域门面

Acorn
  ├── 音频文件格式与码流规范
  ├── Scanner / Decode / Encode / Data
  └── 例如 Wav / Flac / Ogg / Opus / Future: Mp3 / Aac / Mp4Audio

Sonic.Audio.Extensions.FFmpeg
  └── 在 `Acorn` 尚未覆盖的格式与硬件能力上提供桥接
```

这里的分层要求是:

- `Sonic.Core.Media` 只提供抽象与公共原语
- `Sonic.Standard.Media` 只提供标准默认实现
- `Sonic.Audio` 才定义 `IAudioFormatHandler`、`IAudioFormatDetector`、`AudioFormatRegistry`
- `FFmpeg` 桥接扩展只接入 `Sonic.Audio` 的上层格式门面，不向 `Acorn` 反向注入抽象

### 5.2 `FFmpeg` 功能映射

| `FFmpeg` 能力                 | 归属                           |
|:------------------------------|:-------------------------------|
| `libavformat` 容器解析        | `Acorn` 或过渡期 `FFmpeg` 扩展 |
| `libavcodec` 音频码流解码     | `Acorn` 或过渡期 `FFmpeg` 扩展 |
| `libswresample` 重采样/重混音 | `Sonic.Audio` 核心             |
| `libavfilter` 音频滤镜图      | `Sonic.Audio` 核心             |
| 硬件编解码与平台设备          | 扩展层                         |

### 5.3 当前可直接复用的 `Acorn` 能力

当前仓库中已有:

- `Acorn.Wav`
- `Acorn.Flac`
- `Acorn.Ogg`
- `Acorn.Opus`

后续建议补齐:

- `Acorn.Mp3`
- `Acorn.Aac`
- `Acorn.M4a`
- `Acorn.Mp4`
- `Acorn.Matroska`

## 6. 目标项目结构

```text
projects/
├── Sonic.Core/
│   └── Media/
│       ├── Audio/IAudio.cs
│       ├── Audio/AudioAttribute.cs
│       ├── SampleFormat.cs
│       ├── ChannelLayout.cs
│       └── 后续公共媒体原语
├── Sonic.Standard/
│   └── Media/
│       ├── Audio.cs
│       ├── 后续 AudioFrame.cs
│       └── 后续 AudioClip.cs
└── Sonic.Audio/
    ├── DSP/
    ├── Analysis/
    ├── Graph/
    ├── Formats/
    │   ├── IAudioFormatHandler.cs
    │   ├── IAudioFormatDetector.cs
    │   ├── AudioFormatRegistry.cs
    │   └── Acorn/
    ├── Extensions/
    └── Interop/
```

## 7. 对外 API 方向

### 7.1 基础模型

```csharp
public interface IAudio
{
    int sample_rate { get; }
    int channels { get; }
    SampleFormat format { get; }
}

public readonly struct AudioFrame<TSample> : IAudio
    where TSample : unmanaged
{
    public int sample_rate { get; }
    public int channels { get; }
    public SampleFormat format { get; }
    public ChannelLayout layout { get; }
    public ReadOnlySpan<TSample> readonly_span();
}
```

### 7.2 处理与分析

```csharp
public static class AudioOps
{
    public static AudioFrame<float> resample(AudioFrame<float> audio, int sample_rate);
    public static AudioFrame<float> remix(AudioFrame<float> audio, ChannelLayout layout);
    public static AudioFrame<float> normalize(AudioFrame<float> audio, float target_lufs);
    public static AudioFrame<float> fade_in(AudioFrame<float> audio, int milliseconds);
}

public static class AudioAnalysis
{
    public static Spectrum compute_spectrum(AudioFrame<float> audio);
    public static MelSpectrogram compute_mel(AudioFrame<float> audio);
    public static BeatTrack detect_beats(AudioFrame<float> audio);
}
```

### 7.3 编解码入口

```csharp
public static class AudioIO
{
    public static AudioFrame<float> load(ReadOnlySpan<byte> data);
    public static AudioFrame<float> load(string path);
    public static byte[] save_wav(AudioFrame<float> audio);
    public static byte[] save_flac(AudioFrame<float> audio);
}
```

## 8. 第三方可扩展性

建议支持以下扩展类型:

- `Format Provider`
- `Processor`
- `Analyzer`
- `Importer`
- `Exporter`
- `ML Bridge`

典型扩展包:

- `Sonic.Audio.Extensions.FFmpeg`
- `Sonic.Audio.Extensions.PortAudio`
- `Sonic.Audio.Extensions.ASR`
- `Sonic.Audio.Extensions.TTS`

边界原则:

- `ASR` / `TTS` / 音频生成模型只通过扩展接入
- 设备采集和播放后端只通过扩展接入
- `FFmpeg` 桥接扩展只作为过渡方案，不取代 `Acorn` 作为格式 Source 的角色
- `Format Provider` 和 `AudioFormatRegistry` 只存在于 `Sonic.Audio`，不下沉到 `Acorn`

## 9. 分阶段实施

### Phase 1: 抽象收口

- 收口 `Sonic.Core.Media` 中的音频基础契约
- 明确 `IAudio`、`SampleFormat`、`ChannelLayout` 的唯一归属
- 在 `Sonic.Standard.Media` 中补齐并强化 `AudioBuffer<T>`
- `Sonic.Audio` 改为依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- 在历史命名空间位置保留过渡转发层

### Phase 2: 基础模型补齐

- `AudioFrame`
- `AudioClip`
- `AudioBuffer`
- 时间范围与布局模型

### Phase 3: `Acorn` 适配

- 接入 `WAV`
- 接入 `FLAC`
- 接入 `Ogg`
- 接入 `Opus`
- 提供统一 `AudioIO`
- 建立 `AudioFormatRegistry`

### Phase 4: `FFmpeg` 音频核心能力补齐

- 重采样
- 重混音
- 音频滤镜图
- 基础动态处理
- 卷积与均衡

### Phase 5: 分析能力补齐

- FFT / STFT
- 频谱 / 梅尔频谱
- 响度 / 节拍 / 特征

### Phase 6: 扩展生态

- `FFmpeg` 桥接扩展
- 播放/录音设备扩展
- `ASR` / `TTS` / 音乐生成扩展

## 10. 验收标准

- `IAudio`、`SampleFormat`、`ChannelLayout` 的唯一主实现位于 `Sonic.Audio`
- 音频格式编解码统一通过 `Acorn` 或受控的过渡桥接完成
- `Sonic.Audio` 核心可覆盖 `FFmpeg` 音频链路中的重采样、滤镜图和 DSP 能力
- 智能语音与生成式能力全部通过扩展接入
- `Sonic.Video` 中不重复实现音频 DSP

## 11. 结论

`Sonic.Audio` 的定位不是“播放器封装”或“音频文件工具箱”，而是媒体栈中的音频运行时与 DSP 基础设施:

- `Acorn` 负责格式和码流
- `Sonic.Audio` 负责音频帧、DSP、分析和滤镜图
- `DL/ML` 与平台设备通过扩展接入

按照这个规划推进后，`Sonic.Audio` 将与 `Sonic.Image`、`Sonic.Video` 形成职责清晰、可协作的统一媒体子系统。
