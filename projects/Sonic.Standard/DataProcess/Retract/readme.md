# Restore 层 — 类型还原

## 定位

Restore 层是 Project 层的读取侧对称操作：从原始字节序列中按固定字段顺序还原对象。

## 为什么 Restore 要独立于 Project？

表面上看，把 `project` 和 `restore` 写在同一个类里最方便——事实也是如此，`IProjection<T>` 组合了二者。那为什么还要单独定义
`IRestorer<T>`？

**单向消费场景不需要写入能力**。大量应用只从网络/文件读取二进制协议包，从不构造发送。如果强行绑定 `IProjection<T>`
，要么被迫实现一个空写的 `project` 方法，要么在接口层面就多了一个"我不需要的依赖"。作为基础设施，提供只读的 `IRestorer<T>`
让使用者精确表达需求——"我只读，别让我实现不用的方法"。

## 为什么还原器需要 `out bytesConsumed`？

与解码层同样的问题： **二进制数据可能不完整**。给你一段字节说"按 Point 还原"，但只有 6 字节（Point 是 2 × f64 = 16 字节）。

数据不足时返回 `bytesConsumed = 0` 和 `default(T)`。这个约定的一致性贯穿整个读取链路：

```
解码层：bytesConsumed == 0 → 数据不足
还原层：bytesConsumed == 0 → 数据不足（透传自解码层）
解帧层：try_get_next_frame == false → 等更多数据
```

链路上的每一层都用同一个语义表示"再等等"，不需要抛异常来传递流控信号。

## 为什么还原器不参与格式检测？

还原器假设"给我的是正确格式的二进制帧"。格式检测（Detect 层）应该在从网络收到字节后、还没决定用哪个还原器之前完成。一旦选定了还原器，字节就按该
schema 解析——如果 parse 失败，那是 schema 不匹配（格式错误），不是格式未知。

## 不应该承担什么职责？

- **不应校验值域**：`age` 字段是 `i32`，解析出了 `-100` —— 还原层不认为这是错误。负数 `age` 是业务规则，属于 DataContract。
- **不应跳过不明字段**：序列化路径（JSON）可以 `skip unknown key`，因为键名天然知道。投射路径没有键名——如果字节流比预期长，说明
  schema 不匹配，属于格式错误。
- **不应管理缓冲区**：还原器接受 `ReadOnlySpan<byte>`，不持有缓冲区。缓冲区管理由解帧层负责。

## 与上下游的衔接

```
上游（Unframe 输出的帧载荷）→ ReadOnlySpan<byte>
    ↓
IRestorer<T>.restore(buffer, out consumed) → 逐字段解码，构造 T
    ↓（调用 IDecoder 解码基本类型）
下游（业务层）→ 拿到完整的 T 实例
```

## 典型场景

- **游戏位置同步**：收到 16 字节的二进制包，`PointRestorer` 还原为 `(x, y, z, yaw)`。不经过 JSON 解析，不分配字符串。
- **传感器数据采集**：温度传感器每秒上报 8 字节（`timestamp: i64, temperature: f32`），`SensorRestorer` 还原后存入时序数据库。
- **IPC 共享内存**：进程间通过共享内存交换 `struct`，`project`/`restore` 在共享内存 buffer 上操作，零拷贝。