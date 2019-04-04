using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using TypeCheckResult = Nyar.Language.Valkyrie.TypeChecker.TypeCheckResult;

namespace Valkyrie.Tests.LanguageTests;

public sealed class CallArgumentTypeValidationTests
{
    [Fact]
    public void Analyze_UnionParameter_AllowsMatchingBranch()
    {
        var semantics = Analyze("""
                                micro accept(value: i32 | string) -> unit {
                                }

                                micro demo() -> unit {
                                    accept(1)
                                }
                                """);

        Assert.DoesNotContain(semantics.diagnostics, diagnostic => diagnostic.Code == "VALK_CALL_ARGUMENT_TYPE");
    }

    [Fact]
    public void Analyze_UnionParameter_RejectsUnmatchedArgument()
    {
        var semantics = Analyze("""
                                micro accept(value: i32 | string) -> unit {
                                }

                                micro demo() -> unit {
                                    accept(true)
                                }
                                """);

        var diagnostic = Assert.Single(semantics.diagnostics, item => item.Code == "VALK_CALL_ARGUMENT_TYPE");
        Assert.Contains("i32 | string", diagnostic.Message);
        Assert.Contains("bool", diagnostic.Message);
    }

    [Fact]
    public void Analyze_IntersectionParameter_RejectsMissingConstraint()
    {
        var semantics = Analyze("""
                                micro accept(value: i32 & string) -> unit {
                                }

                                micro demo() -> unit {
                                    accept(1)
                                }
                                """);

        var diagnostic = Assert.Single(semantics.diagnostics, item => item.Code == "VALK_CALL_ARGUMENT_TYPE");
        Assert.Contains("i32 & string", diagnostic.Message);
        Assert.Contains("i32", diagnostic.Message);
    }

    [Fact]
    public void Analyze_LetInitializer_AllowsLiteralTextToChar_WhenSingleTextElement()
    {
        var semantics = Analyze("""
                                micro demo() -> unit {
                                    let c: char = "x"
                                }
                                """);

        Assert.DoesNotContain(semantics.diagnostics, diagnostic => diagnostic.Code == "VALK_LET_INITIALIZER_TYPE");
    }

    [Fact]
    public void Analyze_LetInitializer_AllowsEmojiLiteralTextToChar_WhenSingleTextElement()
    {
        var semantics = Analyze("""
                                micro demo() -> unit {
                                    let c: char = "😀"
                                }
                                """);

        Assert.DoesNotContain(semantics.diagnostics, diagnostic => diagnostic.Code == "VALK_LET_INITIALIZER_TYPE");
    }

    [Fact]
    public void Analyze_LetInitializer_RejectsLiteralTextToChar_WhenMultipleTextElements()
    {
        var semantics = Analyze("""
                                micro demo() -> unit {
                                    let c: char = "ab"
                                }
                                """);

        var diagnostic = Assert.Single(semantics.diagnostics, item => item.Code == "VALK_LET_INITIALIZER_TYPE");
        Assert.Contains("char", diagnostic.Message);
        Assert.Contains("literal_text", diagnostic.Message);
    }

    private static SemanticModel Analyze(string source)
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(source, "call_validation.v").Value!;
        var bridge = new ValkyrieSemanticBridge();
        return bridge.BuildSemanticModel(new TypeCheckResult([]), ast, "call_validation.v");
    }
}
