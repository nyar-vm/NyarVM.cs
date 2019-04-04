using Nyar.Analyzer.Semantic;
using Std.Data.Text.Valkyrie.Semantic;
using TypeCheckResult = Nyar.Language.Valkyrie.TypeChecker.TypeCheckResult;

namespace Valkyrie.Tests.LanguageTests;

public sealed class ObjectMemberModelTests
{
    [Fact]
    public void Analyze_ClassMembers_PublicFieldProducesAccessorsButPrivateFieldDoesNot()
    {
        var semantics = Analyze("""
                                class Box {
                                    value: i32
                                    private secret: i32
                                }
                                """);

        var box = Assert.IsType<NamedType>(semantics.resolve_symbol("Box")?.type);
        Assert.Contains(box.members, member => member.Kind == SymbolKind.property && member.Name == "value");
        Assert.Contains(box.members, member => member.Kind == SymbolKind.method && member.Name == "get_value");
        Assert.Contains(box.members, member => member.Kind == SymbolKind.method && member.Name == "set_value");
        Assert.DoesNotContain(box.members, member => member.Kind == SymbolKind.method && member.Name == "get_secret");
        Assert.DoesNotContain(box.members, member => member.Kind == SymbolKind.method && member.Name == "set_secret");
    }

    [Fact]
    public void Analyze_ClassMethods_MethodTypeDoesNotExposeSelfParameter()
    {
        var semantics = Analyze("""
                                class Box {
                                    micro scale(self, factor: i32) -> i32 {
                                        return factor
                                    }
                                }
                                """);

        var box = Assert.IsType<NamedType>(semantics.resolve_symbol("Box")?.type);
        var scale = Assert.IsType<FunctionType>(box.members.Single(member => member.Name == "scale").type);
        Assert.Single(scale.parameter_types);
        Assert.Equal("i32", scale.parameter_types[0].Name);
    }

    [Fact]
    public void Analyze_MultiFileTypeAnnotation_PreservesDeclaredTraitMembers()
    {
        var semantics = Analyze(
            ("consumer.v", """
                           micro read(input: Readable) -> i32 {
                               return input.get_value()
                           }
                           """),
            ("trait.v", """
                        trait Readable {
                            micro get_value(self) -> i32
                        }
                        """));

        var readable = Assert.IsType<NamedType>(semantics.resolve_symbol("Readable")?.type);
        Assert.Equal("trait", readable.kind_tag);
        Assert.Contains(readable.members, member => member.Kind == SymbolKind.method && member.Name == "get_value");
        Assert.DoesNotContain(semantics.diagnostics, diagnostic => diagnostic.level == DiagnosticSeverity.Error);
    }

    private static SemanticModel Analyze(string source)
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(source, "member_model.v").Value!;
        var bridge = new ValkyrieSemanticBridge();
        return bridge.build_semantic_model(new TypeCheckResult([]), ast, "member_model.v");
    }

    private static SemanticModel Analyze(params (string filePath, string source)[] sources)
    {
        var analyzer = new SourceAnalyzer();
        var compilationUnits = sources
            .Select(source => analyzer.Parse(source.source, source.filePath).Value!)
            .ToArray();
        var bridge = new ValkyrieSemanticBridge();
        return bridge.build_semantic_model(new TypeCheckResult([]), compilationUnits, "multi_file_member_model");
    }
}

