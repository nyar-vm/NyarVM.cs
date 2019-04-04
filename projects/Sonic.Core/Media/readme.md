# Sonic.Core.Media 规划总纲

**版本**: 0.1  
**状态**: 规划中  
**日期**: 2026-05-30

## 1. 定位

`Sonic.Core.Media` 是 `Sonic` 媒体体系的 **核心抽象层**。它不是图像库、音频库或视频库本身，而是为这些子系统提供统一、稳定、可复用的媒体基础契约。

这套设计的真正目标不是只服务当前 `NyarVM.cs` 仓库，而是作为未来 `valkyrie.v` 生态的预演:

- `Sonic.Core` 对应未来的 `core.v`
- `Sonic.Standard` 对应未来的 `std.v`
- `Sonic.Core.Media` 对应未来 `core.v` 中的媒体基础层
- `Sonic.Standard.Media` 对应未来 `std.v` 中的媒体标准实现层
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` 对应建立在 `core.v` / `std.v` 之上的媒体子域包

## 2. 媒体边界

`Media` 的广度不应只等于 `Image` / `Audio` / `Video` 三者。

当前最先落地的是这三类能力，但长期看，`Media` 应覆盖更宽的领域:

- 图像、音频、视频
- 动画图像与帧序列
- 时间语义，例如 `Timestamp`、`Duration`、`TimeBase`
- 轨道、流、片段、时间线、同步策略
- 字幕、歌词、Timed Text、Caption
- 元数据、封面、章节、附件流
- 采集、设备、会话、实时流
- 滤镜图、转码编排、媒体会话

因此，`Media` 是 **上位域**，`Image` / `Audio` / `Video` 是其下的核心子域，而不是全部。

## 3. 分层原则

建议媒体栈按三层组织:

```text
Sonic.Core.Media
  ├── 纯抽象、纯契约、纯枚举
  ├── 跨模态公共原语
  └── 各模态最低层接口

Sonic.Standard.Media
  ├── 标准内存模型
  ├── 默认容器实现
  ├── 基础缓冲与布局工具
  └── 不含具体格式规范实现

Sonic.Image / Sonic.Audio / Sonic.Video
  ├── 模态专属处理能力
  ├── 高层 API 与处理管线
  ├── 格式门面、注册表、扩展宿主
  └── 第三方桥接与 `Acorn` 适配
```

对应职责:

| 层                                            | 定位          | 典型内容                                                                                 |
|:----------------------------------------------|:--------------|:-----------------------------------------------------------------------------------------|
| `Sonic.Core.Media`                            | `core.v` 预演 | `IImage`、`IAudio`、`IVideoFrame`、`PixelFormat`、`SampleFormat`、`IMediaMuxer`          |
| `Sonic.Standard.Media`                        | `std.v` 预演  | `Image<TPixel>`、`AudioBuffer<T>`、未来的 `VideoFrame<TPixel>`、`AudioClip`、`VideoClip` |
| `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` | 垂直子域包    | `ImageOps`、`AudioOps`、`VideoVision`、格式门面、第三方扩展                              |

## 4. 当前代码归位

当前仓库已经具备这套分层的雏形:

### 4.1 `Sonic.Core.Media`

当前已在 `projects/Sonic.Core/Media/` 中落位:

- `Image/IImage.cs`
- `Image/ImageAttribute.cs`
- `Audio/IAudio.cs`
- `Audio/AudioAttribute.cs`
- `Video/IVideoFrame.cs`
- `Video/VideoFrameAttribute.cs`
- `PixelFormat.cs`
- `SampleFormat.cs`
- `ChannelLayout.cs`
- `IMediaDecoder.cs`
- `IMediaEncoder.cs`
- `IMediaMuxer.cs`
- `IMediaDemuxer.cs`
- `IFilterGraph.cs`
- `IHardwareCodec.cs`
- `IDeviceContext.cs`
- `HardwareAcceleratedAttribute.cs`

### 4.2 `Sonic.Standard.Media`

当前已在 `projects/Sonic.Standard/Media/` 中落位:

- `Image.cs`，标准图像容器实现
- `Audio.cs`，标准音频缓冲实现

后续建议补齐:

- `Video.cs` 或等价的标准视频帧/片段实现
- 共用缓冲、布局和元数据模型

## 5. 依赖规则

媒体层的依赖方向建议固定如下:

```text
Sonic.Core.Media
    ↑
Sonic.Standard.Media
    ↑
Sonic.Image / Sonic.Audio / Sonic.Video
    ↑
Sonic.Plotter / Sonic.Animator / 其他上层应用
```

约束如下:

- `Sonic.Core.Media` 不依赖 `Sonic.Standard`
- `Sonic.Core.Media` 不依赖 `Acorn`
- `Sonic.Standard.Media` 只依赖 `Sonic.Core.Media`
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` 依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- 媒体二进制格式的规范实现仍然只属于 `Acorn`
- `Format Handler`、`Registry`、高层 `IO` 门面只属于 `Sonic.Image` / `Sonic.Audio` / `Sonic.Video`

## 6. 与 `Acorn` 的关系

`Sonic.Core.Media` 和 `Sonic.Standard.Media` 都不是格式实现层。

职责边界如下:

- `Acorn`：二进制格式规范、`Data`、`Encode`、`Decode`、`Scanner`
- `Sonic.Core.Media`：媒体抽象和公共原语
- `Sonic.Standard.Media`：标准内存模型和默认实现
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video`：格式门面、注册、算法、处理图、宿主扩展

也就是说:

- `Acorn` 不拥有 `Registry`
- `Acorn` 不拥有 `Format Handler`
- `Sonic.Core.Media` 不写 PNG/JPEG/WAV/MP4 等格式细节
- `Sonic.Standard.Media` 也不写格式细节

## 7. 四份文档的关系

这次需要理清的四份文档应形成如下关系:

| 文档                         | 角色                                                                          |
|:-----------------------------|:------------------------------------------------------------------------------|
| `Sonic.Core/Media/readme.md` | 媒体总纲，定义 `core.v` / `std.v` 预演下的总分层                              |
| `Sonic.Image/readme.md`      | 图像子域规划，说明如何建立在 `Sonic.Core.Media` / `Sonic.Standard.Media` 之上 |
| `Sonic.Audio/readme.md`      | 音频子域规划，说明如何建立在 `Sonic.Core.Media` / `Sonic.Standard.Media` 之上 |
| `Sonic.Video/readme.md`      | 视频子域规划，说明如何建立在 `Sonic.Core.Media` / `Sonic.Standard.Media` 之上 |

这四份文档不应彼此争夺“谁是抽象层”。

## 8. 迁移原则

从现在开始，媒体相关规划统一采用以下原则:

- 不再新建 `Sonic.Image.Abstractions`、`Sonic.Audio.Abstractions`、`Sonic.Video.Abstractions`
- 抽象统一收口到 `Sonic.Core.Media`
- 标准默认实现统一收口到 `Sonic.Standard.Media`
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` 专注模态专属能力
- 若未来出现比三模态更通用的媒体公共原语，优先放进 `Sonic.Core.Media`

## 9. 结论

`Sonic` 这套媒体体系的正确理解不是“三个平行项目各自搭一套底层”，而是:

- `Sonic.Core.Media` 提供媒体基础语言
- `Sonic.Standard.Media` 提供标准实现
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` 提供垂直能力
- 整体作为未来 `valkyrie.v` 生态中 `core.v` / `std.v` 媒体分层的预演
