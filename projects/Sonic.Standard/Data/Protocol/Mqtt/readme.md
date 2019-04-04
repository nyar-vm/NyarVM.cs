# MQTT 协议帧

## 定位

MQTT（Message Queuing Telemetry Transport）是 IoT 领域的事实标准消息协议。帧格式：1 字节固定头（包类型 + 标志）+ 变长剩余长度（LEB128
编码，最多 4 字节）+ 载荷。

Sonic 的 MQTT 实现提供帧封装/解帧，覆盖 MQTT 3.1.1 和 MQTT 5.0 共享的底层帧格式。

## 为什么 MQTT 用 LEB128 编码剩余长度？

IoT 设备的带宽极其有限。一个 `PINGREQ` 包的载荷长度是 0——用固定 4 字节编码意味着 75% 的带宽浪费。LEB128 让短消息（<128
字节）只需要 1 字节长度字段，长消息（最大 256MB）最多 4 字节。

MQTT 的 LEB128 与标准 LEB128 的区别： **每个字节的最高位是延续标志**，低 7 位是有效数据。最多 4 字节（最大值 268,435,455）。

## 为什么固定头只有 4 bit 包类型？

MQTT 设计于 1999 年，运行在 9600 bps 的卫星链路上。每一个 bit 都是宝贵的。15 种包类型对当时的需求绰绰有余——MQTT 3.1.1 只用了
14 种，MQTT 5.0 加了 AUTH（第 15 种）。

低 4 位标志在不同包类型中有不同含义：

- **PUBLISH**：DUP (1bit) + QoS (2bit) + RETAIN (1bit)——这是 MQTT 中唯一有实质标志的包类型。
- **其他包类型**：标志位保留，必须为协议规定的固定值。

`MqttFramer` 的 `flags` 参数让你精确控制这些位。对于 PUBLISH 包，你需要手动编码 QoS 等信息。

## 为什么 MQTT 5.0 的帧格式与 3.1.1 相同？

MQTT 5.0 在载荷中增加了 **可变头部属性**（Property Length + 属性键值对），但固定头和剩余长度编码没有变。这意味着
`MqttFramer`/`MqttUnframer` 同时适用于 3.1.1 和 5.0——版本差异全部在载荷内部。

## 不应该承担什么职责？

- **不应实现 CONNECT/CONNACK 握手**：协议版本协商、认证、Keep Alive 定时器是连接管理层的事。
- **不应做 QoS 状态机**：QoS 1/2 的 PUBACK/PUBREC/PUBREL/PUBCOMP 四步握手是上层状态机的事。
- **不应管理主题订阅**：SUBSCRIBE/SUBACK 的匹配和确认是客户端/服务端逻辑。
- **不应实现遗嘱消息**：Will Topic/Will Message 是 CONNECT 载荷的一部分，由上层构造。

## 与上下游的衔接

```
上游（连接管理层）→ 构造 MQTT 载荷（如 PUBLISH 的 topic + payload）
    ↓
MqttFramer(packetType: 3, flags: qos << 1).frame(payload, writer)
    ↓ 输出格式：
    [0x30 | qos<<1][LEB128 剩余长度][载荷]
下游（Socket.Send）→ 发送 TCP 字节

接收方向：
上游（Socket.Receive）→ 喂入 TCP 字节
    ↓
MqttUnframer.feed(data) → 累积
MqttUnframer.try_get_next_frame() → 完整 MQTT 帧
    ↓（通过 current_packet_type 判断包类型）
下游（连接管理层）→ packet_type=3 → PUBLISH 处理，packet_type=12 → PINGREQ 回 PINGRESP
```

## 典型场景

- **IoT 传感器网关**：传感器 → MQTT PUBLISH → 网关 → 聚合 → 上报云端。
- **MQTT Broker 实现**：接收客户端的 CONNECT/SUBSCRIBE/PUBLISH，路由消息到订阅者。
- **MQTT 代理/桥接**：透明转发 MQTT 帧，只解帧头检查包类型，不解析载荷。