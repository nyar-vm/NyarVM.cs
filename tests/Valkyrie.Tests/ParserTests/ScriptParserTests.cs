using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Nyar.Language.Valkyrie.Formatter;

namespace Valkyrie.Tests.ParserTests;

public sealed class ScriptParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_TopLevelExpressionWithoutSemicolon_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     print("Hello Script!")
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Single(unit.Declarations);
        Assert.IsType<TermNode>(unit.Declarations[0]);
    }

    [Fact]
    public void Parse_ImplyWithRawIdentifierOperatorMethod_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     structure Utf8Builder {}

                     imply Utf8Builder {
                         infix `+=`(mut self, c: char) {
                             self.append(c)
                         }
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var imply = unit.Declarations.OfType<DeclareImply>().Single();
        Assert.Contains(imply.Methods, method => method.Name?.Name == "+=");
    }

    [Fact]
    public void Parse_RawIdentifierMemberCall_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro test(mut x: Counter) {
                         x.`+=`(1)
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.Declarations));
        var statement = Assert.IsType<TermCallExpression>(Assert.Single(function.Body!.Statements));
        var member = Assert.IsType<TermDotExpression>(statement.Caller);
        Assert.Equal("+=", member.Callee.Name);
        Assert.Equal(MemberAccessSeparatorKind.dot, member.separator_kind);
    }

    [Fact]
    public void Parse_RealUtf8BuilderStdFile_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var path = @"e:\RiderProjects\Valkyrie.cs\examples\std\source\text\Utf8Builder.v";
        var source = File.ReadAllText(path);

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var imply = unit.Declarations.OfType<DeclareImply>().Single();
        Assert.Contains(imply.Methods, method => method.Name?.Name == "+=");
    }

    [Fact]
    public void Parse_DocComments_ShouldTreatHashQuestionAndAplLampAsDocumentComments()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     # 普通注�?                    ///  ascii doc
                     �?apl doc
                     structure Utf8Builder {}
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var structure = Assert.IsType<DeclareStructure>(Assert.Single(unit.Declarations));
        var documents = structure.Annotations.Documents;
        Assert.Collection(
            documents,
            doc => Assert.Equal(" ascii doc", doc.Content),
            doc => Assert.Equal(" apl doc", doc.Content));
    }

    [Fact]
    public void Parse_QualifiedStaticMemberCall_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro test() {
                         Vector::new(16)
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.Declarations));
        var statement = Assert.IsType<TermCallExpression>(Assert.Single(function.Body!.Statements));
        var member = Assert.IsType<TermDotExpression>(statement.Caller);
        Assert.Equal("new", member.Callee.Name);
        Assert.Equal(MemberAccessSeparatorKind.double_colon, member.separator_kind);
    }

    [Fact]
    public void Parse_RealCoreTextFileWithDoubleColonNamespace_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var path = @"e:\RiderProjects\Valkyrie.cs\examples\core\source\text\_.v";
        var source = File.ReadAllText(path);

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var namespaceDecl = Assert.IsType<DeclareNamespace>(unit.Declarations[0]);
        Assert.Equal("core.text", namespaceDecl.Name.Name);
    }

    [Fact]
    public void Parse_TraitAssociatedTypeConstraint_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     trait Text {
                         type View: TextView<Text = Self>;
                         micro view(self) -> Self::View;
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var trait = Assert.IsType<DeclareTrait>(Assert.Single(unit.Declarations));
        Assert.NotNull(trait.Inheritance);
        var inherited = Assert.Single(trait.Inheritance!.Bases);
        Assert.Equal("Text", ((TypeLiteralNamePathNode)inherited.BaseType).Path.Name);
        var associatedType = Assert.Single(trait.Body!.AssociatedTypes);
        Assert.Equal("View", associatedType.Name?.Name);

        var constraintType = Assert.IsType<TypeExpressionBinaryNode>(associatedType.Constraint);
        Assert.Equal("TextView", ((TypeLiteralNamePathNode)constraintType.Lhs).Path.Name);
        var binding = Assert.IsType<TypeArgumentItem>(constraintType.Rhs);
        Assert.Equal("Text", binding.Slot?.Name);
        Assert.Equal("Self", ((TypeLiteralNamePathNode)binding.Argument).Path.Name);

        var method = Assert.Single(trait.Body.Methods);
        Assert.Equal("Self::View", ((TypeLiteralNamePathNode)method.ReturnType!).Path.Name);
    }

    [Fact]
    public void Parse_NamedTypeArgumentsInTypePosition_ShouldPreserveSlot()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro consume(items: IntoIterator<Item = i32>) -> Unit {
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var formatted = ValkyrieFormatter.Format(unit);
        Assert.Contains("IntoIterator<Item = i32>", formatted);
    }
}
