namespace Hermes.Generator;

public sealed class GeneratorResult
{
    public List<GeneratedFile> Files { get; init; } = [];
    public List<string> Dependencies { get; init; } = [];
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public bool Success => Errors.Count == 0;
}