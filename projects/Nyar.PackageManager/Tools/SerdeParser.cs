using Std.DataProcess.Serialize;

namespace Nyar.PackageManager.Tools;

/// <summary>
///     将配置文本解析为 `SerdeValue` 中性字面量树。
///     PackageManager 依赖此抽象，而不依赖任何具体序列化格式（VON、JSON 等）。
/// </summary>
/// <param name="content">配置文本内容</param>
/// <returns>解析后的 SerdeValue 树</returns>
public delegate SerdeValue SerdeParser(string content);
