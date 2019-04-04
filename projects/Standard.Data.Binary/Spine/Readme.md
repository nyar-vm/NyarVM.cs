# 📦 Acorn.Spine

Spine 骨骼动画二进制格式（`.skel`）编解码器。

## 📐 格式布局

### skel 二进制文件头

| 字段      | 偏移 | 大小 | 说明                      | 对应类                             |
|-----------|------|------|---------------------------|------------------------------------|
| Signature | 0x00 | 4    | `"sKel"` 或 `"skel"`      | `SpineProjectData`（内部解析）     |
| Version   | 0x04 | 变长 | 版本字符串（如 "4.1.00"） | `SpineProjectData.SkeletonVersion` |
| Hash      | 变长 | 变长 | 项目哈希                  | `SpineProjectData.Hash`            |

### 骨架信息（Skeleton）

| 字段         | 说明             | 对应类                          |
|--------------|------------------|---------------------------------|
| Width        | 画布宽度         | `SpineProjectData.Width`        |
| Height       | 画布高度         | `SpineProjectData.Height`       |
| SpineVersion | Spine 编辑器版本 | `SpineProjectData.SpineVersion` |

### 骨骼（Bones）

| 字段           | 说明       | 对应类                        |
|----------------|------------|-------------------------------|
| Name           | 骨骼名称   | `SpineBone.Name`              |
| Parent         | 父骨骼索引 | `SpineBone.Parent`            |
| X, Y           | 位置       | `SpineBone.X` / `Y`           |
| Rotation       | 旋转角度   | `SpineBone.Rotation`          |
| ScaleX, ScaleY | 缩放       | `SpineBone.ScaleX` / `ScaleY` |
| ShearX, ShearY | 剪切       | `SpineBone.ShearX` / `ShearY` |
| Length         | 长度       | `SpineBone.Length`            |
| TransformMode  | 变换模式   | `SpineBone.TransformMode`     |

### 插槽（Slots）

| 字段       | 说明                    | 对应类                 |
|------------|-------------------------|------------------------|
| Name       | 插槽名称                | `SpineSlot.Name`       |
| Bone       | 绑定骨骼索引            | `SpineSlot.Bone`       |
| Attachment | 默认附件名称            | `SpineSlot.Attachment` |
| Color      | 颜色（RGBA）            | `SpineSlot.Color`      |
| DarkColor  | 暗色（用于 Tint Black） | `SpineSlot.DarkColor`  |
| BlendMode  | 混合模式                | `SpineSlot.BlendMode`  |

### 皮肤（Skins）

| 字段        | 说明     | 对应类                  |
|-------------|----------|-------------------------|
| Name        | 皮肤名称 | `SpineSkin.Name`        |
| Attachments | 附件映射 | `SpineSkin.Attachments` |

### 动画（Animations）

| 字段       | 说明           | 对应类                      |
|------------|----------------|-----------------------------|
| Name       | 动画名称       | `SpineAnimation.Name`       |
| Bones      | 骨骼时间轴     | `SpineAnimation.Bones`      |
| Slots      | 插槽时间轴     | `SpineAnimation.Slots`      |
| Deforms    | 变形时间轴     | `SpineAnimation.Deforms`    |
| DrawOrders | 绘制顺序时间轴 | `SpineAnimation.DrawOrders` |
| Events     | 事件时间轴     | `SpineAnimation.Events`     |

### 时间轴类型

| 类型       | 说明     | 对应类                               |
|------------|----------|--------------------------------------|
| rotate     | 旋转     | `SpineTimeline`（Type="rotate"）     |
| translate  | 位移     | `SpineTimeline`（Type="translate"）  |
| scale      | 缩放     | `SpineTimeline`（Type="scale"）      |
| shear      | 剪切     | `SpineTimeline`（Type="shear"）      |
| attachment | 附件切换 | `SpineTimeline`（Type="attachment"） |
| color      | 颜色变化 | `SpineTimeline`（Type="color"）      |
| deform     | 网格变形 | `SpineTimeline`（Type="deform"）     |

## 🏗️ 核心类

| 类                 | 说明               | 文件                                                 |
|--------------------|--------------------|------------------------------------------------------|
| `SpineProjectData` | Spine 项目完整数据 | [Data/SpineProjectData.cs](Data/SpineProjectData.cs) |
| `SpineBone`        | 骨骼               | [Data/SpineBone.cs](Data/SpineBone.cs)               |
| `SpineSlot`        | 插槽               | [Data/SpineSlot.cs](Data/SpineSlot.cs)               |
| `SpineSkin`        | 皮肤               | [Data/SpineSkin.cs](Data/SpineSkin.cs)               |
| `SpineAnimation`   | 动画               | [Data/SpineAnimation.cs](Data/SpineAnimation.cs)     |
| `SpineTimeline`    | 时间轴             | [Data/SpineAnimation.cs](Data/SpineAnimation.cs)     |
| `SpineKeyframe`    | 关键帧             | [Data/SpineAnimation.cs](Data/SpineAnimation.cs)     |
| `SpineEvent`       | 事件               | [Data/SpineEvent.cs](Data/SpineEvent.cs)             |
| `SpineConstraint`  | 约束               | [Data/SpineConstraint.cs](Data/SpineConstraint.cs)   |
| `SpineAtlasData`   | 图集数据           | [Data/SpineAtlasData.cs](Data/SpineAtlasData.cs)     |
| `SpineDecoder`     | Spine 解码器       | [Decode/SpineDecoder.cs](Decode/SpineDecoder.cs)     |
| `SpineEncoder`     | Spine 编码器       | [Encode/SpineEncoder.cs](Encode/SpineEncoder.cs)     |
| `SpineScanner`     | Spine 扫描器       | [Scanner/SpineScanner.cs](Scanner/SpineScanner.cs)   |

## 📚 格式规范参考

- [Spine User Guide](http://esotericsoftware.com/spine-in-depth)
- [Spine Binary Format](http://esotericsoftware.com/spine-binary-format)
- [Spine Runtimes GitHub](https://github.com/EsotericSoftware/spine-runtimes)
