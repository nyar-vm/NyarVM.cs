using Nyar.Analyzer.Semantic;
using Std.Data.Text.Valkyrie.Semantic;
using ValkyrieTypeChecker = Nyar.Language.Valkyrie.TypeChecker.TypeChecker;

namespace Valkyrie.Tests.SemanticTests;

public sealed class DataAttributeSemanticTests
{
    [Fact]
    public void BuildSemanticModel_DataStructure_ShouldCollectFieldMetadata()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            structure Profile {
                display_name: utf8
            }

            [data]
            structure User {
                [field("user_name")]
                name: utf8

                [field("id")]
                [alias("user_id")]
                id: i32 = 1

                [ignore]
                secret: utf8

                [flatten]
                profile: Profile
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        var dataType = model.find_data_type(ValkyrieNamePath.parse("app.User"));
        Assert.NotNull(dataType);
        Assert.Equal("structure", dataType.declaration_kind);

        var nameField = Assert.Single(dataType.fields.Where(field => field.name == "name"));
        Assert.Equal("user_name", nameField.binding_name);
        Assert.Empty(nameField.aliases);
        Assert.False(nameField.ignore);
        Assert.False(nameField.flatten);

        var idField = Assert.Single(dataType.fields.Where(field => field.name == "id"));
        Assert.Equal("id", idField.binding_name);
        Assert.Contains("user_id", idField.aliases);
        Assert.True(idField.has_default_value);
        Assert.Equal("1", idField.default_value_text);

        var secretField = Assert.Single(dataType.fields.Where(field => field.name == "secret"));
        Assert.True(secretField.ignore);

        var profileField = Assert.Single(dataType.fields.Where(field => field.name == "profile"));
        Assert.True(profileField.flatten);
    }

    [Fact]
    public void BuildSemanticModel_DataTrait_ShouldReportUnsupportedTarget()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            [data]
            trait ViewModel {
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DATA_UNSUPPORTED_TARGET");
    }

    [Fact]
    public void BuildSemanticModel_DataClassWithMethod_ShouldReportError()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            [data]
            class User {
                name: utf8

                micro touch(self) -> Unit {
                }
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DATA_CLASS_WITH_METHODS");
    }

    [Fact]
    public void BuildSemanticModel_DataFieldsWithConflictingBindingNames_ShouldReportError()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            [data]
            structure User {
                [field("name")]
                login: utf8

                [alias("name")]
                display_name: utf8
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DATA_FIELD_NAME_CONFLICT");
    }

    [Fact]
    public void BuildSemanticModel_DataFieldWithDuplicateAlias_ShouldReportError()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            [data]
            structure User {
                [alias("user_name")]
                [alias("user_name")]
                name: utf8
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DATA_DUPLICATE_ALIAS");
    }

    [Fact]
    public void BuildSemanticModel_DataFieldWithIgnoreAndFlatten_ShouldReportError()
    {
        var analyzer = new SourceAnalyzer();
        var unit = assert_parse_success(analyzer.parse(
            """
            namespace app;

            structure Profile {
                display_name: utf8
            }

            [data]
            structure User {
                [ignore]
                [flatten]
                profile: Profile
            }
            """,
            "app.v"));

        var bridge = new ValkyrieSemanticBridge();
        var model = bridge.build_semantic_model(new TypeCheckResult([]), [unit], "workspace");

        Assert.Contains(model.diagnostics, diagnostic => diagnostic.code == "VALK_DATA_IGNORE_FLATTEN_CONFLICT");
    }

    private static CompilationUnit assert_parse_success(ParseResult<CompilationUnit> result)
    {
        Assert.True(result.success, string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }
}
