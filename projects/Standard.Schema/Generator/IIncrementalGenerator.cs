namespace Hermes.Generator;

public interface IIncrementalGenerator : IGenerator
{
    GeneratorResult GenerateIncremental(IncrementalGeneratorContext context);
}