# Sonic.Video 功能规划

**版本**: 0.1  
**状态**: 规划中  
**日期**: 2026-05-30

## 1. 目标

`Sonic.Video` 是建立在 `Sonic.Core.Media` 与 `Sonic.Standard.Media` 之上的视频子域包，负责构建对标 `FFmpeg` 视频链路与
`OpenCV video` 模块的统一运行时。

核心原则:

- `Sonic.Core.Media` 负责视频基础契约，`Sonic.Standard.Media` 负责标准帧模型实现，`Sonic.Video` 负责更高层的视频子域能力。
- 视频格式编解码、容器封装和码流规范统一委托给 `Acorn`，不得在 `Sonic.Video` 中重复实现 `MP4`/`MKV`/`WebM`/`H264`/`H265`/
  `VP9`/`AV1` 等格式细节。
- `OpenCV` 中单帧与静态几何视觉归 `Sonic.Image`，`OpenCV video` 模块中的时域视觉能力归 `Sonic.Video`。
- `DL/ML`、目标检测模型、分割模型、姿态模型等不进入核心包，通过扩展形式接入。

## 1.1 在 `valkyrie.v` 生态中的位置

`Sonic` 当前这套媒体分层，应被视为未来 `valkyrie.v` 生态的预演。

| 当前项目               | 未来映射                 | 定位                               |
|:-----------------------|:-------------------------|:-----------------------------------|
| `Sonic.Core`           | `core.v`                 | 基础契约与核心原语                 |
| `Sonic.Standard`       | `std.v`                  | 标准实现与默认运行时能力           |
| `Sonic.Core.Media`     | `core.v::media`          | 媒体公共抽象与最低层模态契约       |
| `Sonic.Standard.Media` | `std.v::media`           | 标准图像/音频/视频默认实现         |
| `Sonic.Video`          | `std.v` 或上层媒体子域包 | 时域处理、编排、格式门面、扩展宿主 |

## 2. 与其他媒体子系统的边界

| 项目          | 核心职责                                 | 典型能力                                    |
|:--------------|:-----------------------------------------|:--------------------------------------------|
| `Sonic.Image` | 单帧图像、动画图像、静态视觉             | `OpenCV imgproc` / `features2d` / `calib3d` |
| `Sonic.Audio` | 音频帧、DSP、分析、滤镜图                | `FFmpeg` 音频链路                           |
| `Sonic.Video` | 视频帧序列、时域处理、转码编排、A/V 同步 | `FFmpeg` 视频链路 + `OpenCV video`          |

统一原则:

- 单帧处理优先落在 `Sonic.Image`
- 音频 DSP 优先落在 `Sonic.Audio`
- 涉及时间序列、帧间关系、音视频同步、媒体编排的逻辑落在 `Sonic.Video`
- `Acorn` 不承载 `Format Handler` 抽象、`Format Detector` 抽象和 `Registry`

### 2.1 `Sonic.Core` / `Sonic.Standard` 归位

视频侧的基础分层应以 `Sonic.Core.Media` 和 `Sonic.Standard.Media` 为准，而不是继续拆新的 `Sonic.Video.Abstractions`。

`Sonic.Video` 侧的归位目标如下:

| 旧位置                                                   | 动作                                   | 新位置                 |
|:---------------------------------------------------------|:---------------------------------------|:-----------------------|
| `projects/Sonic.Core/Media/Video/IVideoFrame.cs`         | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/Video/VideoFrameAttribute.cs` | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/PixelFormat.cs`               | 保持在核心抽象层                       | `Sonic.Core.Media`     |
| `projects/Sonic.Standard/Media/Video.cs`                 | 后续新增标准视频帧/片段实现            | `Sonic.Standard.Media` |
| `projects/Sonic.Core/Media/IMediaMuxer.cs`               | 保持在媒体公共层，容器门面在上层细化   | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IMediaDemuxer.cs`             | 保持在媒体公共层，容器门面在上层细化   | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IFilterGraph.cs`              | 保持在媒体公共层，视频图在上层特化     | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IHardwareCodec.cs`            | 保持在媒体公共层，具体桥接在上层扩展   | `Sonic.Core.Media`     |
| `projects/Sonic.Core/Media/IDeviceContext.cs`            | 保持在媒体公共层，设备细节在扩展层落地 | `Sonic.Core.Media`     |

迁移后依赖规则:

- `Sonic.Core.Media` 不依赖 `Sonic.Standard` / `Sonic.Image` / `Sonic.Audio` / `Sonic.Video`
- `Sonic.Standard.Media` 依赖 `Sonic.Core.Media`
- `Sonic.Video` 依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- `Sonic.Video` 在需要音频协作时依赖 `Sonic.Audio`
- `Sonic.Core.Media` 保留为真正的媒体公共层

### 2.2 统一媒体项目拓扑

为和 `Sonic.Image`、`Sonic.Audio` 保持一致，建议采用如下媒体项目拓扑:

| 项目                   | 职责                                 | 允许依赖                                              |
|:-----------------------|:-------------------------------------|:------------------------------------------------------|
| `Sonic.Core.Media`     | 媒体公共抽象与最低层契约             | 无标准实现依赖                                        |
| `Sonic.Standard.Media` | 标准图像/音频/视频默认实现           | `Sonic.Core.Media`                                    |
| `Sonic.Image`          | 图像处理、静态视觉、格式门面         | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |
| `Sonic.Audio`          | DSP、分析、滤镜图、格式门面          | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |
| `Sonic.Video`          | 时域处理、编排、`A/V Sync`、格式门面 | `Sonic.Core.Media` + `Sonic.Standard.Media` + `Acorn` |

## 3. 对标范围

### 3.1 对标 `FFmpeg` 的视频能力

`Sonic.Video` 对标以下能力域:

- 视频帧模型与时间基
- 视频滤镜图
- 帧率变换、时间重采样、时间裁剪
- 像素格式转换、颜色空间转换、缩放
- 转码任务编排
- 音视频同步
- 媒体管线中的 packet/frame 级调度

### 3.2 对标 `OpenCV video` 的时域视觉能力

- 光流
- 背景建模
- 目标跟踪
- 视频稳定
- 帧间差分
- 多帧融合
- 时域事件检测

### 3.3 不纳入核心包的部分

- 视频容器与码流的底层二进制规范实现
- 媒体播放器 UI
- 操作系统摄像头驱动与底层设备接入
- 深度学习训练与推理框架
- 非线性编辑器工程系统

## 4. 职责边界

`Sonic.Video` 应只负责以下内容:

- 基于 `Sonic.Core.Media` 与 `Sonic.Standard.Media` 扩展视频子域能力，而不是重新定义最低层抽象
- 视频内存模型: 帧缓冲、帧序列、片段、流、时间基
- 时域处理: 帧采样、插帧、裁剪、拼接、转场前处理
- 视频图像操作: 缩放、色彩转换、去隔行、超采样前处理
- 时域视觉: 光流、跟踪、背景建模、稳定、运动分析
- 滤镜图与转码编排: `VideoFilterGraph`、`TranscodePipeline`
- 音视频同步: 与 `Sonic.Audio` 协作的 `A/V Sync`

`Sonic.Video` 不负责以下内容:

- 自建 `MP4`/`MKV`/`WebM`/`TS` 容器规范
- 自建 `H264`/`H265`/`VP9`/`AV1` 码流规范
- 单帧图像格式编解码
- 音频 DSP 和频谱分析
- 深度学习模型与推理运行时
- 媒体播放器和编辑器 UI

## 5. 与 `Acorn` 和 `FFmpeg` 的关系

### 5.1 架构分层

```text
Sonic.Core.Media
  ├── 视频抽象与媒体公共原语
  └── `IVideoFrame` / `PixelFormat` / `IMediaMuxer` / `IFilterGraph`

Sonic.Standard.Media
  ├── 后续 `VideoFrame<TPixel>`
  └── 后续 `VideoClip`

Sonic.Video
  ├── VideoOps / VideoVision / VideoFilterGraph
  ├── A/V Sync / TranscodePipeline / VideoFormatRegistry
  └── 上层视频子域门面

Acorn
  ├── 容器格式 / 码流规范 / Scanner / Decode / Encode / Data
  ├── Future: Mp4 / Matroska / WebM / H264 / H265 / AV1 / Vp9
  └── 媒体二进制 Source of Truth

Sonic.Video.Extensions.FFmpeg
  └── 在 `Acorn` 尚未覆盖的格式和硬件路径上提供桥接
```

这里的分层要求是:

- `Sonic.Core.Media` 只提供抽象与公共原语
- `Sonic.Standard.Media` 只提供标准默认实现
- `Sonic.Video` 才定义 `IVideoFormatHandler`、`IVideoFormatDetector`、`VideoFormatRegistry`
- `FFmpeg` 桥接扩展只接入 `Sonic.Video` 的上层格式门面，不向 `Acorn` 反向注入抽象

### 5.2 `FFmpeg` 功能映射

| `FFmpeg` 能力                   | 归属                           |
|:--------------------------------|:-------------------------------|
| `libavformat` 容器解析          | `Acorn` 或过渡期 `FFmpeg` 扩展 |
| `libavcodec` 视频码流解码       | `Acorn` 或过渡期 `FFmpeg` 扩展 |
| `libswscale` 缩放与像素格式转换 | `Sonic.Video` 核心             |
| `libavfilter` 视频滤镜图        | `Sonic.Video` 核心             |
| `ffmpeg` 命令式转码编排         | `Sonic.Video` 的高层管线 API   |
| 硬件编解码与设备后端            | 扩展层                         |

### 5.3 `OpenCV` 功能映射

| `OpenCV` 模块 | 归属                 |
|:--------------|:---------------------|
| `imgproc`     | `Sonic.Image`        |
| `features2d`  | `Sonic.Image`        |
| `calib3d`     | `Sonic.Image`        |
| `video`       | `Sonic.Video`        |
| `videoio`     | `Sonic.Video` 扩展层 |
| `dnn`         | 扩展层               |

## 6. 目标项目结构

```text
projects/
├── Sonic.Core/
│   └── Media/
│       ├── Video/IVideoFrame.cs
│       ├── Video/VideoFrameAttribute.cs
│       ├── PixelFormat.cs
│       ├── IMediaMuxer.cs
│       ├── IMediaDemuxer.cs
│       └── 后续公共媒体原语
├── Sonic.Standard/
│   └── Media/
│       ├── 后续 VideoFrame.cs
│       ├── 后续 VideoClip.cs
│       └── 后续 Timeline 相关标准模型
└── Sonic.Video/
    ├── Processing/
    ├── Vision/
    ├── Graph/
    ├── Formats/
    │   ├── IVideoFormatHandler.cs
    │   ├── IVideoFormatDetector.cs
    │   ├── VideoFormatRegistry.cs
    │   └── Acorn/
    ├── Extensions/
    └── Interop/
```

## 7. 对外 API 方向

### 7.1 基础模型

```csharp
public interface IVideoFrame
{
    int width { get; }
    int height { get; }
    PixelFormat format { get; }
    long timestamp { get; }
}

public readonly struct VideoFrame<TPixel> : IVideoFrame
    where TPixel : unmanaged
{
    public int width { get; }
    public int height { get; }
    public PixelFormat format { get; }
    public long timestamp { get; }
    public ReadOnlySpan<TPixel> readonly_span();
}
```

### 7.2 处理与视觉

```csharp
public static class VideoOps
{
    public static VideoFrame<Rgba32> scale(VideoFrame<Rgba32> frame, int width, int height);
    public static VideoClip trim(VideoClip clip, long start, long end);
    public static VideoClip concat(IReadOnlyList<VideoClip> clips);
}

public static class VideoVision
{
    public static OpticalFlowField calc_optical_flow(IVideoFrame previous, IVideoFrame current);
    public static TrackResult track(VideoClip clip, TrackRequest request);
    public static StabilizedClip stabilize(VideoClip clip);
}
```

### 7.3 媒体编排

```csharp
public static class VideoTranscode
{
    public static VideoClip decode(ReadOnlySpan<byte> data);
    public static byte[] encode(VideoClip clip, VideoEncodeOptions options);
    public static byte[] mux(VideoClip video, AudioFrame<float>? audio, MuxOptions options);
}
```

## 8. 第三方可扩展性

建议支持以下扩展类型:

- `Format Provider`
- `Processor`
- `Vision Operator`
- `Importer`
- `Exporter`
- `Device Bridge`
- `ML Bridge`

典型扩展包:

- `Sonic.Video.Extensions.FFmpeg`
- `Sonic.Video.Extensions.OpenCV`
- `Sonic.Video.Extensions.MediaFoundation`
- `Sonic.Video.Extensions.GStreamer`
- `Sonic.Video.Extensions.DNN`

边界原则:

- `FFmpeg` 扩展可作为容器/码流和硬件路径的过渡桥接
- `OpenCV` 扩展可桥接现成时域视觉算子
- `DNN`、目标检测、分割、姿态、OCR 视频流能力全部通过扩展接入
- `Format Provider` 和 `VideoFormatRegistry` 只存在于 `Sonic.Video`，不下沉到 `Acorn`

## 9. 分阶段实施

### Phase 1: 抽象收口

- 收口 `Sonic.Core.Media` 中的视频基础契约
- 明确 `IVideoFrame`、`PixelFormat`、`VideoFrameAttribute` 的唯一归属
- 在 `Sonic.Standard.Media` 中补齐标准视频帧与片段实现
- `Sonic.Video` 改为依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- 在历史命名空间位置保留过渡转发层

### Phase 2: 基础模型补齐

- `VideoFrame`
- `VideoClip`
- `Timeline`
- `AvSyncContext`

### Phase 3: `Acorn` 适配与桥接预留

- 抽象 `IVideoFormatHandler`
- 建立 `VideoFormatRegistry`
- 预留 `FFmpeg` 桥接扩展
- 推进 `Acorn` 视频容器与码流项目规划

### Phase 4: `FFmpeg` 视频核心能力补齐

- 缩放与像素格式转换
- 视频滤镜图
- 转码编排
- 音视频同步

### Phase 5: `OpenCV video` 能力补齐

- 光流
- 跟踪
- 背景建模
- 视频稳定
- 运动分析

### Phase 6: 扩展生态

- `FFmpeg` 扩展
- `OpenCV` 扩展
- 摄像头/采集后端扩展
- `DNN` 视频理解扩展

## 10. 验收标准

- `IVideoFrame` 的唯一主实现位于 `Sonic.Video`
- 单帧图像算法不再在 `Sonic.Video` 中重复实现
- 视频容器和码流规范统一通过 `Acorn` 或受控桥接完成
- `Sonic.Video` 核心可覆盖 `FFmpeg` 视频链路中的滤镜图、转码编排和 A/V 同步
- `Sonic.Video` 核心可覆盖 `OpenCV video` 模块中的时域视觉能力
- `DL/ML` 视频能力全部通过扩展接入

## 11. 结论

`Sonic.Video` 的定位不是“播放器封装”或“FFmpeg 命令包装器”，而是媒体栈中的视频运行时、时域处理与编排基础设施:

- `Acorn` 负责视频容器和码流规范
- `Sonic.Video` 负责视频帧、滤镜图、转码编排和时域视觉
- `Sonic.Audio` 提供音频协作与 DSP
- `DL/ML` 与设备采集通过扩展接入

按照这个规划推进后，`Sonic.Video` 将与 `Sonic.Image`、`Sonic.Audio` 形成职责清晰、可协作的统一媒体子系统。
