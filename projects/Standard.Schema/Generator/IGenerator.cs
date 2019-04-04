namespace Hermes.Generator;

public interface IGenerator
{
    string Name { get; }
    string[] SupportedTargets { get; }
    GeneratorResult Generate(GeneratorContext context);
}