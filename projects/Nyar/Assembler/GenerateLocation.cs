namespace Nyar.Assembler;

/// <summary>
///     元编译位置。
/// </summary>
public sealed record GenerateLocation(string? file_path, int line, int column);