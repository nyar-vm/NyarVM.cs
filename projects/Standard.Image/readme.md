# Sonic.Image 功能规划

**版本**: 0.1  
**状态**: 规划中  
**日期**: 2026-05-30

## 1. 目标

`Sonic.Image` 的目标不是再造一套图像格式库，也不是自带一层新的基础抽象，而是作为建立在 `Sonic.Core.Media` 与
`Sonic.Standard.Media` 之上的图像子域包，统一承接图像处理、静态视觉、格式门面和上层集成能力。

核心原则:

- `Sonic.Core.Media` 负责图像基础契约，`Sonic.Standard.Media` 负责标准图像容器实现，`Sonic.Image` 负责更高层的图像子域能力。
- `Sonic.Image` 的能力范围以 OpenCV 的经典图像处理、静态视觉、几何视觉模块为目标上限。
- 图像格式编解码统一委托给 `Acorn`，不得在 `Sonic.Image` 中重复实现 PNG/JPEG/GIF/BMP/TGA/DDS/PSD/EXR 等格式细节。
- 文本格式不放进二进制图像编解码层。若后续涉及 `SVG`，应走 `Oak` 文本编解码，而不是在 `Sonic.Image` 中自建解析器。
- `DL/ML`、推理框架、模型分发和训练能力不进入核心包，通过扩展形式接入。

### 1.1 在 `valkyrie.v` 生态中的位置

`Sonic` 当前这套媒体分层，应被视为未来 `valkyrie.v` 生态的预演，而不是一次性的本地项目拆分。

| 当前项目               | 未来映射                 | 定位                                   |
|:-----------------------|:-------------------------|:---------------------------------------|
| `Sonic.Core`           | `core.v`                 | 纯基础契约与核心原语                   |
| `Sonic.Standard`       | `std.v`                  | 标准实现与默认运行时能力               |
| `Sonic.Core.Media`     | `core.v::media`          | 媒体公共抽象与最低层模态契约           |
| `Sonic.Standard.Media` | `std.v::media`           | 标准图像/音频/视频内存模型与默认实现   |
| `Sonic.Image`          | `std.v` 或上层媒体子域包 | 图像处理、静态视觉、格式门面、扩展宿主 |

因此，`Sonic.Image` 的职责不是占有 `IImage`、`PixelFormat` 这类最低层抽象，而是消费 `Sonic.Core.Media` 与
`Sonic.Standard.Media`，向上提供图像子域能力。

### 1.2 与 `Sonic.Audio` / `Sonic.Video` 的统一定位

三者的统一媒体分工如下:

| 项目          | 核心职责                                                   | 对标能力域                                                         |
|:--------------|:-----------------------------------------------------------|:-------------------------------------------------------------------|
| `Sonic.Image` | 单帧图像、动画图像、静态图像处理、静态几何视觉             | `OpenCV imgcodecs` / `imgproc` / `features2d` / `calib3d` 的图像侧 |
| `Sonic.Audio` | 音频帧、DSP、频谱分析、滤镜图、重采样混音                  | `FFmpeg` 音频链路 + `SoX` / `librosa` 式音频处理                   |
| `Sonic.Video` | 视频帧序列、时域视频处理、转码编排、音视频同步、视频滤镜图 | `FFmpeg` 视频链路 + `OpenCV video` 模块                            |

统一原则:

- `Acorn` 负责所有媒体二进制格式的唯一编解码 Source
- `Sonic.Image` / `Sonic.Audio` / `Sonic.Video` 负责运行时模型、处理图、算法和上层门面
- `DL/ML` 统一通过扩展接入，不直接污染三者核心包

### 1.3 OpenCV 对标范围

这里所说的“覆盖 OpenCV”是指覆盖其 **经典算法与视觉基础设施能力域**，包括但不限于:

- 图像容器、像素格式、颜色空间、图像金字塔
- 图像 I/O、格式探测、元数据、动画图像
- 滤波、卷积、锐化、去噪、直方图、阈值
- 形态学、连通域、分割、边缘检测、轮廓分析
- 几何变换、重采样、仿射、透视、坐标映射
- 特征点、描述子、特征匹配、模板匹配
- 霍夫变换、角点检测、关键点检测
- 传统目标检测、区域分析、Blob 分析
- 相机模型、标定、畸变矫正、单应与基础几何
- 多图像配准、立体视觉、相机标定、基础几何

不包含的部分:

- 深度学习模型定义、训练、推理运行时
- 大模型视觉理解能力
- 视频容器、音视频复用、媒体播放栈
- 时域视频算法，例如光流、目标跟踪、背景建模、视频稳定

也就是说，媒体三件套整体对标 `OpenCV` + `FFmpeg`，而 `Sonic.Image` 只承担其中图像与静态视觉部分，不承担时域视频和媒体播放栈的角色。

### 1.4 与 `Sonic.Core` / `Sonic.Standard` 的关系

图像侧的正确分层应如下:

| 层         | 项目                   | 职责                                                 |
|:-----------|:-----------------------|:-----------------------------------------------------|
| 核心抽象层 | `Sonic.Core.Media`     | `IImage`、`PixelFormat`、`ImageAttribute` 等基础契约 |
| 标准实现层 | `Sonic.Standard.Media` | `Image<TPixel>` 等标准容器与基础内存模型             |
| 图像子域层 | `Sonic.Image`          | 图像处理、静态视觉、格式门面、扩展宿主               |

约束如下:

- 不再新建 `Sonic.Image.Abstractions`
- `Sonic.Image` 不重新定义最低层图像抽象
- `Sonic.Image` 依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- `Sonic.Image` 专注图像模态专属能力，而不是承担整个 `Media` 公共层

## 2. 当前代码盘点

目前 `Sonic` 体系内与图像直接相关的代码主要分为四类。

### 2.1 已落位到 `Sonic.Core` / `Sonic.Standard` 的基础部分

| 当前位置                                            | 当前职责                            | 规划动作                                                |
|:----------------------------------------------------|:------------------------------------|:--------------------------------------------------------|
| `projects/Sonic.Core/Media/PixelFormat.cs`          | 像素格式枚举                        | 保持在 `Sonic.Core.Media`，作为统一像素格式定义         |
| `projects/Sonic.Core/Media/Image/IImage.cs`         | 图像基础接口                        | 保持在 `Sonic.Core.Media`                               |
| `projects/Sonic.Core/Media/Image/ImageAttribute.cs` | 图像元数据标记                      | 保持在 `Sonic.Core.Media`                               |
| `projects/Sonic.Standard/Media/Image.cs`            | 泛型图像容器，提供 `resize`、`crop` | 保持在 `Sonic.Standard.Media`，并逐步增强为标准默认实现 |

### 2.2 保留在上层项目，但需要改为依赖 `Sonic.Image`

| 当前位置                                                 | 当前职责                                    | 规划动作                                                                              |
|:---------------------------------------------------------|:--------------------------------------------|:--------------------------------------------------------------------------------------|
| `projects/Sonic.Plotter/Rendering/BitmapRenderCanvas.cs` | 基于 `System.Drawing` 的位图渲染与 PNG 导出 | 保留在 `Sonic.Plotter`，但 PNG 导出改为输出像素缓冲后委托 `Sonic.Image` + `Acorn.Png` |
| `projects/Sonic.Plotter/Rendering/SvgRenderCanvas.cs`    | `SVG` 文本输出                              | 保留在 `Sonic.Plotter`，不并入 `Sonic.Image`                                          |

### 2.3 仅与图像消费有关，不属于 `Sonic.Image` 核心

| 当前位置                                                | 当前职责           | 说明                                     |
|:--------------------------------------------------------|:-------------------|:-----------------------------------------|
| `projects/Sonic/Net/Middleware/StaticFileMiddleware.cs` | 静态文件 MIME 分发 | 只消费图片文件扩展名，不属于图像领域模型 |

### 2.4 明确不纳入本次拆分

| 当前位置                                           | 原因                                          |
|:---------------------------------------------------|:----------------------------------------------|
| `projects/Hypersonic/Infra/ImageLayerAttribute.cs` | 这里的 `Image` 指容器镜像层，不是图像媒体领域 |

### 2.5 历史迁移与归位

从历史脉络看，这些抽象最初来自 `Hypersonic.Media`；但在当前仓库中，它们已经开始落位到 `Sonic.Core` 与 `Sonic.Standard`
。因此新的规划不再继续拆新的 `*.Abstractions` 项目，而是顺着现有的 `Core` / `Standard` 分层收口。

`Sonic.Image` 侧的归位目标如下:

| 旧位置                                             | 动作                         | 新位置                                                      |
|:---------------------------------------------------|:-----------------------------|:------------------------------------------------------------|
| `Hypersonic/Media/Image/IImage.cs`                 | 已归位                       | `projects/Sonic.Core/Media/Image/IImage.cs`                 |
| `Hypersonic/Media/Image/ImageAttribute.cs`         | 已归位                       | `projects/Sonic.Core/Media/Image/ImageAttribute.cs`         |
| `Hypersonic/Media/PixelFormat.cs`                  | 已归位                       | `projects/Sonic.Core/Media/PixelFormat.cs`                  |
| `projects/Sonic/Media/Image.cs`                    | 已归位并待增强               | `projects/Sonic.Standard/Media/Image.cs`                    |
| `Hypersonic/Media/HardwareAcceleratedAttribute.cs` | 保持在媒体公共层，后续再细分 | `projects/Sonic.Core/Media/HardwareAcceleratedAttribute.cs` |

迁移后依赖规则:

- `Sonic.Core.Media` 不依赖 `Sonic.Standard` / `Sonic.Image` / `Sonic.Audio` / `Sonic.Video`
- `Sonic.Standard.Media` 依赖 `Sonic.Core.Media`
- `Sonic.Image` 依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- `Sonic.Video` 通过 `Sonic.Core.Media` 复用 `PixelFormat`
- 不再新增 `Sonic.Media.*` 这种跨图像/音频/视频的大一统命名空间
- `Sonic.Core.Media` 保留为真正的媒体公共层，而不是删除

## 3. 现状问题

当前图像能力存在以下问题:

- 图像抽象分散在 `Hypersonic` 与 `Sonic` 两处，命名空间统一为 `Sonic.Media.*`，但项目边界不清晰。
- `Sonic` 只有一个最小化的 `Image<TPixel>` 容器，没有形成完整的图像子系统。
- `Sonic.Plotter` 直接通过 `System.Drawing.Bitmap.Save(..., ImageFormat.Png)` 输出 PNG，这会绕过 `Acorn`，与“格式编解码唯一
  Source”原则冲突。
- 缺少图像格式适配层，导致上层模块无法通过统一接口接入 `Acorn` 的现有 PNG/BMP/GIF/JPEG/TGA/DDS/PSD/EXR/Basis 能力。
- 像素格式、平面格式、元数据、尺寸、步长、裁剪区域等基础模型尚未系统化。
- 经典图像处理与传统计算机视觉能力基本空白，离 OpenCV 等级的算法覆盖还有很大差距。
- `Hypersonic.Media` 里的媒体抽象过于聚合，不利于图像/音频/视频子系统分别演进。

## 4. `Sonic.Image` 的职责边界

`Sonic.Image` 应只负责以下内容:

- 基于 `Sonic.Core.Media` 与 `Sonic.Standard.Media` 扩展图像子域，而不是重新定义最低层抽象
- 动画图像抽象: 多帧位图、帧延迟、循环次数、逐帧元数据
- 图像内存模型: 尺寸、区域、步长、像素缓冲、平面缓冲
- 图像处理: 裁剪、拷贝、翻转、通道转换、缩放、滤波、阈值、形态学、直方图、分割
- 传统视觉: 特征提取、匹配、轮廓、边缘、霍夫变换、标定、几何估计
- 编解码适配: 为上层提供统一的 `load` / `save` / `detect` API，但具体格式读写委托给 `Acorn`
- 上层集成: 为 `Sonic.Plotter`、截图模块、纹理导入流程提供统一入口

`Sonic.Image` 不负责以下内容:

- 自行实现 PNG/JPEG/GIF/BMP/TGA/DDS/PSD/EXR/Basis 格式规范
- 重新实现流式二进制扫描器、文件头解析器、块编码器
- `SVG` 之类文本格式的解析与格式化
- 视频封装、音视频同步、时间轴编辑、编码预设管理
- 摄像头驱动、系统视频采集后端、媒体播放管线
- 深度学习训练框架、模型运行时、张量编译器
- 光流、目标跟踪、背景建模、视频稳定等时域视频视觉算法
- 图表语法、绘图指令、坐标系、布局系统
- GPU 渲染器、驱动层、`RHI`

### 4.1 动画图像边界

`Sonic.Image` 应包含动画图像，但这里的“动画”只限于图像格式内部定义的多帧位图序列，不扩展到视频系统。

纳入 `Sonic.Image` 的类型:

- `GIF`
- animated `WebP`
- `APNG`
- 多帧 `EXR`
- 其他“单个文件内包含多帧图像”的位图格式

不纳入 `Sonic.Image` 的类型:

- `MP4`、`WebM`、`MOV` 这类视频容器
- 带音轨的媒体文件
- 非线性时间轴、转场、轨道、剪辑工程
- 实时流媒体

判断标准:

- 如果一个格式的核心语义是“多帧图像文件”，归 `Sonic.Image`
- 如果一个格式的核心语义是“视频/媒体时间序列”，归未来的 `Sonic.Media` 或专门的视频子系统

因此:

- `GIF` 属于 `Sonic.Image`
- animated `WebP` 属于 `Sonic.Image`
- `APNG` 属于 `Sonic.Image`
- 精灵图集不天然等于动画格式，它更偏资源组织，可由 `Sonic.Image` 提供读写辅助，但不作为核心动画模型本身

## 5. 目标项目结构

建议将图像侧组织为“`Core` 抽象层 + `Standard` 标准实现层 + `Image` 子域层”三层:

```text
projects/
├── Sonic.Core/
│   └── Media/
│       ├── Image/IImage.cs
│       ├── Image/ImageAttribute.cs
│       ├── PixelFormat.cs
│       ├── HardwareAcceleratedAttribute.cs
│       └── 后续公共媒体原语
├── Sonic.Standard/
│   └── Media/
│       ├── Image.cs
│       ├── 后续 AnimationFrame.cs
│       ├── 后续 ImageBuffer.cs
│       └── 后续 ImageMetadata.cs
└── Sonic.Image/
    ├── Pixels/
    ├── Processing/
    ├── Vision/
    ├── Formats/
    │   ├── IImageFormatHandler.cs
    │   ├── IImageFormatDetector.cs
    │   ├── ImageFormatRegistry.cs
    │   └── Acorn/
    ├── Extensions/
    └── Interop/
```

## 6. 与 `Acorn` 的关系

### 6.1 原则

`Sonic.Image` 只能做“上层格式门面与适配器”，不能做“第二套格式实现”。

也就是说:

- `Sonic.Image` 可以定义统一的 `IImageFormatHandler`、`IImageFormatDetector`、`ImageFormatRegistry`
- `Sonic.Image` 可以把 `Image<TPixel>` 映射到 `Acorn` 的数据模型
- `Sonic.Image` 可以根据文件头选择具体 `Acorn` 的 `Encode` / `Decode` / `Scanner` 实现
- `Sonic.Image` 不可以自己写 PNG chunk、JPEG marker、GIF LZW、BMP header
- `Acorn` 本身不承载 `Codec` 抽象、不承载 `Registry`，只提供具体实现

### 6.2 现有可直接复用的 `Acorn` 能力

当前仓库中已有以下图像相关二进制能力:

| 格式 | 类型 | `Acorn` 现状 | `Sonic.Image` 规划 | |:---|:---|:---| | PNG | 静态图像 | `Decode` / `Encode` / `Scanner`
已存在 | 第一批接入 | | BMP | 静态图像 | `Decode` / `Encode` / `Scanner` 已存在 | 第一批接入 | | GIF | 动画图像 |
`Decode` / `Encode` / `Scanner` 已存在 | 第一批接入，作为动画图像基线支持 | | TGA | 静态图像 | `Decode` / `Encode` /
`Scanner` 已存在 | 第二批接入 | | DDS | 静态图像 / 纹理 | `Decode` / `Encode` / `Scanner` 已存在 | 第二批接入 | | PSD |
分层图像 | `Decode` / `Encode` / `Scanner` 已存在 | 第二批接入 | | EXR | 静态图像 / 多帧图像 | `Decode` / `Encode` /
`Scanner` 已存在 | 第二批接入 | | Basis | 纹理压缩图像 | `Decode` / `Encode` / `Scanner` 已存在 | 第二批接入 | | JPEG |
静态图像 | `Decode` 已有，`Encode` 入口已建但当前未实现 | 先支持解码，编码等待 `Acorn` 完成 |

说明:

- 当前 `Acorn` 代码位于 `projects/Olympus.Acorn/`。
- 当前部分命名空间仍使用 `Nyar.Binary.*`，但从职责上它们属于 `Acorn` 层。
- `Sonic.Image` 规划以“职责归属”为准，而不是以现阶段命名为准。

### 6.3 `Sonic.Image` 的适配方式

建议采用“领域模型 + 映射器 + `Acorn` 编解码器”的三层模式:

```text
Sonic.Image.Image<TPixel>
    ↓ 映射
Sonic.Image.Interop.AcornDataMapper
    ↓ 转换
Acorn 对应格式的 Data 模型
    ↓ 编码/解码
Acorn.Xxx.Encode / Acorn.Xxx.Decode / Acorn.Xxx.Scanner
```

例如:

- `save_png(image)`:
  - `Image<Rgba32>` -> `PngImageData`
  - `PngImageData` -> `Acorn.PngEncoder.Encode(...)`
- `load_bmp(bytes)`:
  - `Acorn.BmpDecoder.Decode(...)` -> `BmpImageData`
  - `BmpImageData` -> `Image<Rgba32>`

## 7. 对外 API 方向

建议先依托 `Sonic.Core.Media` 提供稳定且克制的基础契约，再由 `Sonic.Standard.Media` 提供默认容器实现，最后由 `Sonic.Image`
暴露处理和 I/O 门面，不在第一版暴露过多概念。

### 7.1 基础模型

```csharp
public interface IImage
{
    int width { get; }
    int height { get; }
    PixelFormat format { get; }
}

public interface IAnimatedImage
{
    int width { get; }
    int height { get; }
    PixelFormat format { get; }
    int frame_count { get; }
    int loop_count { get; }
    AnimationFrame get_frame(int index);
}

public readonly struct Image<TPixel> : IImage
    where TPixel : unmanaged
{
    public int width { get; }
    public int height { get; }
    public PixelFormat format { get; }
    public Span<TPixel> span();
    public ReadOnlySpan<TPixel> readonly_span();
}
```

建议把动画图像模型定义为“帧集合 + 播放元数据”，而不是引入完整视频时间轴:

```csharp
public readonly struct AnimationFrame<TPixel>
    where TPixel : unmanaged
{
    public Image<TPixel> image { get; }
    public int delay_ms { get; }
    public ImageRegion? blend_region { get; }
}
```

### 7.2 处理入口

```csharp
public static class ImageOps
{
    public static Image<TPixel> crop<TPixel>(Image<TPixel> image, ImageRegion region)
        where TPixel : unmanaged;

    public static Image<TPixel> resize_nearest<TPixel>(Image<TPixel> image, int width, int height)
        where TPixel : unmanaged;

    public static Image<TTarget> convert<TSource, TTarget>(Image<TSource> image)
        where TSource : unmanaged
        where TTarget : unmanaged;
}
```

对于 OpenCV 级别的覆盖，建议在 `ImageOps` 之外再单独建立传统视觉门面，避免基础图像操作与高阶视觉算法混杂:

```csharp
public static class VisionOps
{
    public static Image<byte> canny(Image<byte> image, double low, double high);
    public static Contour[] find_contours(Image<byte> image);
    public static KeyPoint[] detect_orb(Image<byte> image);
    public static Match[] match_descriptors(DescriptorSet left, DescriptorSet right);
    public static Homography estimate_homography(Point2f[] src, Point2f[] dst);
    public static CalibrationResult calibrate_camera(IReadOnlyList<Point3f[]> world_points, IReadOnlyList<Point2f[]> image_points);
}
```

这种划分对应 OpenCV 的模块化思路:

- `ImageOps` 负责基础图像处理
- `VisionOps` 负责单帧与静态几何视觉
- `ImageIO` 负责格式与资源输入输出

### 7.3 编解码入口

```csharp
public static class ImageIO
{
    public static Image<Rgba32> load(ReadOnlySpan<byte> data);
    public static Image<Rgba32> load(string path);
    public static IAnimatedImage load_animated(ReadOnlySpan<byte> data);
    public static IAnimatedImage load_animated(string path);

    public static byte[] save_png(Image<Rgba32> image);
    public static byte[] save_bmp(Image<Rgba32> image);
    public static byte[] save_gif(IReadOnlyList<Image<Rgba32>> frames);
    public static byte[] save_gif(IReadOnlyList<AnimationFrame<Rgba32>> frames, int loop_count = 0);
}
```

这里的 `save_*` / `load` 只是 `Sonic.Image` 的门面，底层实现仍然必须走 `Acorn`。

## 8. 第三方可扩展性

`Sonic.Image` 需要从一开始就支持第三方扩展，否则后续一旦引入自定义格式、外部渲染后端、资产管线，就会被迫在核心项目里不断追加特例。

设计目标:

- 核心模型稳定，第三方能力通过注册而不是修改核心源码接入
- `Sonic.Image` 定义扩展接口，但不把第三方实现和核心模型耦死
- 扩展方可以新增“格式支持”“处理算子”“导入导出流程”“宿主集成”
- 扩展机制不能破坏“格式编解码唯一 Source 属于 `Acorn`”的规则
- `DL/ML` 能力必须通过扩展进入，不反向污染核心图像与传统视觉抽象

### 8.1 扩展类型

建议支持以下扩展类型:

| 扩展类型            | 说明                             | 示例                                     |
|:--------------------|:---------------------------------|:-----------------------------------------|
| `Format Provider`   | 提供新的图像格式接入             | `WebP`、`AVIF`、`KTX2`                   |
| `Processor`         | 提供新的图像处理算子             | 模糊、锐化、抖动、颜色查找表             |
| `Vision Operator`   | 提供新的传统视觉算子             | 自定义角点、匹配器、分割器               |
| `Importer`          | 从外部对象导入图像               | `SkiaSharp`、`ImageSharp`、游戏引擎纹理  |
| `Exporter`          | 输出到外部对象或资产管线         | 贴图打包器、测试快照、引擎资源           |
| `Pixel Adapter`     | 自定义像素结构与统一像素格式映射 | 第三方 `Rgba128Float`、压缩块纹理视图    |
| `Metadata Provider` | 注入额外元数据读取与写回能力     | EXIF、ICC、资产标签                      |
| `ML Bridge`         | 接入推理框架或模型前后处理       | `ONNX Runtime`、`TensorRT`、自定义检测器 |

### 8.2 核心扩展接口

建议在 `Sonic.Image.Extensions` 中定义如下接口:

```csharp
public interface IImageExtension
{
    string name { get; }
    void register(ImageExtensionContext context);
}

public interface IImageFormatProvider
{
    IEnumerable<IImageFormatDetector> create_detectors();
    IEnumerable<IImageFormatHandler> create_handlers();
}

public interface IImageProcessor
{
    string key { get; }
    bool can_process(ImageProcessingRequest request);
    IImage process(IImage source, ImageProcessingRequest request);
}

public interface IImageVisionOperator
{
    string key { get; }
    bool can_execute(ImageVisionRequest request);
    object execute(IReadOnlyList<IImage> inputs, ImageVisionRequest request);
}

public interface IImageImporter
{
    bool can_import(Type source_type);
    IImage import(object source);
}

public interface IImageExporter
{
    bool can_export(Type target_type, IImage image);
    object export(IImage image, Type target_type);
}
```

这些接口的作用是:

- `IImageExtension` 作为总装配入口，便于第三方包一次性注册多个组件
- `IImageFormatProvider` 用于接入新的格式探测器和格式处理器
- `IImageProcessor` 用于接入额外图像处理能力，而不是不断膨胀 `ImageOps`
- `IImageVisionOperator` 用于接入传统视觉算法或 `DL/ML` 桥接后的视觉算子
- `IImageImporter` / `IImageExporter` 用于与外部生态做桥接

### 8.3 注册与发现机制

建议采用“显式注册优先，自动发现可选”的策略。

```csharp
public sealed class ImageExtensionHost
{
    public void add(IImageExtension extension);
    public void add_format_handler(IImageFormatHandler handler);
    public void add_processor(IImageProcessor processor);
    public void add_vision_operator(IImageVisionOperator vision_operator);
    public void add_importer(IImageImporter importer);
    public void add_exporter(IImageExporter exporter);
}
```

注册规则建议如下:

- 核心运行时只保证显式注册可用，避免隐藏依赖
- 可额外提供基于程序集扫描的 `discover_from(Assembly)`，但只作为上层工具能力
- 注册表必须支持优先级和覆盖策略，避免多个第三方包声明同一格式时发生不确定行为
- 所有扩展都应带有稳定的 `key` 或 `name`

### 8.4 与 `Acorn` 的协作边界

第三方扩展可以扩展格式接入，但不能破坏格式归属规则。

允许的方式:

- 第三方包新增 `Sonic.Image.Formats.Acorn.WebPImageFormatHandler`
- 第三方包基于新的 `Acorn.WebP` 项目实现 `Data` / `Encode` / `Decode` / `Scanner`
- 第三方包在 `Sonic.Image` 层注册 `WebP` 格式探测器和门面 API

不允许的方式:

- 在 `Sonic.Image` 扩展包里手写 `WebP` 文件格式细节
- 在 `Sonic.Plotter`、`Sonic.Game` 等上层模块中顺手实现一份专用图片编码器
- 在扩展里绕过 `Acorn` 直接把文件格式逻辑塞进 `ImageIO`

也就是说，第三方可以扩格式，但仍然应遵守:

```text
新格式规范实现 -> Acorn.XXX
统一门面接入     -> Sonic.Image
业务消费         -> Sonic.Plotter / 其他上层
```

### 8.5 第三方包组织建议

建议把第三方扩展拆成独立包，而不是全部塞进 `Sonic.Image` 主包:

| 包名建议                             | 职责                          |
|:-------------------------------------|:------------------------------|
| `Sonic.Image`                        | 核心抽象、基础处理、扩展宿主  |
| `Sonic.Image.Acorn`                  | 官方维护的 `Acorn` 适配器集合 |
| `Sonic.Image.Extensions.SkiaSharp`   | 与 `SkiaSharp` 的导入导出桥接 |
| `Sonic.Image.Extensions.ImageSharp`  | 与 `ImageSharp` 的桥接        |
| `Sonic.Image.Extensions.GameTexture` | 引擎纹理与图集导入导出        |

这样可以保证:

- 核心包依赖保持轻量
- 第三方生态不会反向污染核心抽象
- 不同宿主可以按需安装扩展包

### 8.6 处理管线扩展

除了格式扩展，处理管线也要对第三方开放。

建议在第一版就预留:

- `ImageProcessingRequest`
- `ImageProcessingPipeline`
- 基于 `key` 的处理器选择与组合
- 统一的能力查询接口，例如 `can_process(...)`

这样后续第三方就可以安全加入:

- GPU 加速缩放器
- 领域特定滤镜
- AI 图像预处理步骤
- 纹理压缩前处理流程
- 基于 `DL/ML` 的检测、分割、姿态、OCR 前后处理

### 8.7 `DL/ML` 扩展边界

`Sonic.Image` 的核心必须覆盖 OpenCV 的经典能力，但 `DL/ML` 只以扩展形式接入。

核心内建:

- 经典图像处理
- 传统几何视觉
- 手工特征与匹配
- 标定、配准、匹配、重建等非深度学习静态视觉算法

扩展接入:

- 分类、检测、实例分割、关键点模型
- OCR 模型
- 文生图、图像增强模型
- 大模型视觉理解
- 推理后端适配，例如 `ONNX Runtime`、`OpenVINO`、`TensorRT`

推荐包组织:

- `Sonic.Image`：核心图像与传统视觉
- `Sonic.Image.Extensions.ONNX`：`ONNX Runtime` 桥接
- `Sonic.Image.Extensions.OpenVINO`：`OpenVINO` 桥接
- `Sonic.Image.Extensions.CV.DNN`：高级视觉模型算子封装

### 8.8 兼容性约束

为了让第三方扩展长期可维护，建议明确以下兼容规则:

- `IImage`、`PixelFormat`、`ImageRegion` 这类核心抽象一旦发布，保持二进制兼容优先
- 扩展接口新增成员时优先增加新接口，不随意破坏旧接口
- `ImageFormatRegistry` 的选择顺序、覆盖规则、错误行为要写入契约
- 所有扩展异常应归一到 `Sonic.Image` 自己的错误模型，不把底层异常类型直接泄露给业务层

### 8.9 示例场景

第三方扩展后的典型场景:

1. 第三方发布 `Acorn.WebP`
2. 第三方再发布 `Sonic.Image.Extensions.WebP`
3. 该扩展实现 `IImageExtension` 和 `IImageFormatProvider`
4. 宿主应用在启动时注册扩展
5. `ImageIO.load(...)` 自动识别 `WebP`
6. `Sonic.Plotter`、截图工具、资源管线无需修改即可消费

## 9. 与 `Sonic.Plotter` 的集成策略

`Sonic.Plotter` 不应承担图像格式编码职责，但它需要消费图像输出能力。

建议改造方向:

1. `BitmapRenderCanvas` 保留 GDI+ 或其他渲染后端，仅负责画布绘制。
2. 渲染结束后导出原始像素缓冲，而不是直接 `Save(..., ImageFormat.Png)`。
3. 通过 `PlotterBitmapAdapter` 将位图缓冲转换为 `Image<Rgba32>`。
4. 再由 `Sonic.Image` 调用 `Acorn.Png` 完成真正的 PNG 编码。

这样可以保证:

- 渲染和文件格式彻底解耦
- 图像导出逻辑可以复用到截图、资产管线、测试快照
- 不会在 `Sonic.Plotter` 内残留第二套 PNG 编码实现

## 10. 分阶段实施

### Phase 1: 抽象收口

- 收口 `Sonic.Core.Media` 中的图像基础契约
- 明确 `PixelFormat`、`IImage`、`ImageAttribute` 的唯一归属
- 在 `Sonic.Standard.Media` 中补齐并强化 `Image<TPixel>`
- 让 `Sonic.Image` 改为依赖 `Sonic.Core.Media` 与 `Sonic.Standard.Media`
- 在历史命名空间位置保留过渡转发或兼容层

### Phase 2: 基础模型补齐

- 增加 `ImageSize`
- 增加 `ImageRegion`
- 增加 `ImageMetadata`
- 增加 `ImageBuffer` / `ImagePlane`
- 补齐边界检查、步长、平面布局
- 增加动画图像基础模型

### Phase 3: `Acorn` 适配

- 接入 `PNG`
- 接入 `BMP`
- 接入 `GIF`
- 接入格式探测
- 提供统一的 `ImageIO`
- 建立可插拔 `Format Registry`

### Phase 4: OpenCV 核心能力补齐

- 滤波、卷积、阈值、形态学
- 边缘、轮廓、连通域、分割
- 几何变换、透视、重映射
- 特征点、描述子、匹配
- 标定、单应、基础几何估计
- 多图像配准与静态几何视觉

### Phase 5: 上层集成

- 改造 `Sonic.Plotter` 的 PNG 导出
- 为截图、测试快照、资源导入预留统一入口
- 为多帧图像和纹理格式建立扩展点
- 引入第三方扩展宿主与注册机制

### Phase 6: 增量格式支持

- `TGA`
- `DDS`
- `PSD`
- `EXR`
- `Basis`
- `JPEG` 编码能力跟随 `Acorn` 完成度接入
- 第三方格式与外部图形库桥接

### Phase 7: `DL/ML` 扩展生态

- 提供 `ONNX` / `OpenVINO` / `TensorRT` 等桥接扩展
- 提供检测、分割、OCR、姿态等模型算子适配
- 建立统一前处理 / 后处理协议

## 11. 不做事项

以下内容不作为 `Sonic.Image` 第一阶段目标:

- 图像编辑器 UI
- 图层式文档模型
- GPU 纹理上传与驱动封装
- 相机、视频流、直播推流
- `SVG` 解析器和 `SVG` 格式化器
- 自建 JPEG 编码器
- 为第三方扩展开放“任意修改核心内部状态”的不受控钩子
- 在核心包中直接绑定某个 `DL/ML` 推理框架

## 12. 验收标准

当以下条件满足时，可以认为 `Sonic.Image` 拆分完成:

- `Sonic` 体系中的图像抽象不再散落在多个项目
- `Image<TPixel>` 与 `IImage` 的唯一主实现位于 `Sonic.Image`
- `Sonic.Plotter` 不再直接负责 PNG 文件编码
- `Sonic.Image` 内不存在手写 PNG/JPEG/GIF/BMP 文件格式实现
- 二进制图像格式全部通过 `Acorn` 完成 `Encode` / `Decode` / `Scanner`
- `SVG` 不混入二进制图像编解码职责
- 第三方可通过稳定扩展接口注册新格式、处理器和桥接器，而无需修改核心源码
- 经典图像处理与传统视觉能力域可完整覆盖 OpenCV 的核心模块
- `DL/ML` 能力通过扩展接入，而非硬编码进 `Sonic.Image` 核心

## 13. 结论

本次拆分的重点不是“把所有带 `Image` 字样的代码都搬走”，而是建立一个清晰的图像子系统边界:

- `Sonic.Image` 统一图像领域模型与处理入口
- `Sonic.Image` 内建覆盖 OpenCV 级别的经典图像处理与传统视觉能力
- `Acorn` 统一图像二进制格式编解码
- `Sonic.Plotter` 等上层模块只消费图像能力，不重复实现格式细节
- 第三方能力通过扩展宿主和注册表接入，而不是侵入核心实现
- `DL/ML` 保持为扩展生态，不污染核心包职责边界

按照这个规划推进后，`Sonic` 的图像能力会从“一个容器类型 + 若干零散引用”升级为可复用、可扩展、职责明确的独立子系统。
