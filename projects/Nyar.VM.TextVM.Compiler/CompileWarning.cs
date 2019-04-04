namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 编译警告，包含警告代码、消息及可选的位置信息。
/// </summary>
public readonly record struct CompileWarning(String Code, String Message, Int32 Position = -1);
