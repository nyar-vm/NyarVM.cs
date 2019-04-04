using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class DataAttributeHirTests
{
    [Fact]
    public void BuildHir_DataStructure_ShouldExposeNormalizedDataShape()
    {
        const string source = """
                              namespace app;

                              structure Profile {
                                  display_name: utf8
                              }

                              [data]
                              structure User {
                                  [field("user_name")]
                                  [alias("login")]
                                  name: utf8

                                  [flatten]
                                  profile: Profile
                              }
                              """;

        var module = build_hir(source, "data_shape.v");
        var type = Assert.NotNull(module.find_data_type(HirNamePath.parse("app.User")));
        var dataShape = Assert.NotNull(type.data_shape);
        Assert.Equal(HirDataContainerKind.object_fields, dataShape.container_kind);

        var nameField = Assert.Single(dataShape.fields.Where(field => field.name == "name"));
        Assert.Equal("user_name", nameField.binding_name);
        Assert.Contains("login", nameField.aliases);
        Assert.Equal("utf8", nameField.type.name);

        var profileField = Assert.Single(dataShape.fields.Where(field => field.name == "profile"));
        Assert.True(profileField.flatten);
        Assert.Equal("Profile", profileField.type.name);
    }

    [Fact]
    public void BuildHir_DataShapeHelpers_ShouldResolveSerializableAndDeserializableFields()
    {
        const string source = """
                              namespace app;

                              structure Profile {
                                  display_name: utf8
                              }

                              [data]
                              structure User {
                                  [field("user_name")]
                                  [alias("login")]
                                  name: utf8

                                  [ignore]
                                  secret: utf8

                                  [flatten]
                                  profile: Profile
                              }
                              """;

        var module = build_hir(source, "data_shape_helpers.v");

        var serializableFields = module.enumerate_serializable_fields(HirNamePath.parse("app.User"));
        Assert.DoesNotContain(serializableFields, field => field.name == "secret");
        Assert.Contains(serializableFields, field => field.name == "name");
        Assert.Contains(serializableFields, field => field.name == "profile");

        var byBindingName = Assert.NotNull(module.find_deserializable_field(HirNamePath.parse("app.User"), "user_name"));
        Assert.Equal("name", byBindingName.name);

        var byAlias = Assert.NotNull(module.find_deserializable_field(HirNamePath.parse("app.User"), "login"));
        Assert.Equal("name", byAlias.name);
    }

    [Fact]
    public void BuildHir_DataDeriveRequests_ShouldSeparateTypeKindAndContainerKind()
    {
        const string source = """
                              namespace app;

                              [data]
                              structure User {
                                  name: utf8
                              }

                              [data]
                              class Session {
                                  token: utf8
                              }

                              micro helper() -> Unit {
                                  let shadow = User {}
                                  shadow.serialize()
                              }

                              [main]
                              micro main() -> Unit {
                                  let user = User {}
                                  let session = Session {}
                                  user.serialize()
                                  Session::deserialize(session)
                              }
                              """;

        var module = build_hir(source, "data_derive_requests.v");
        var requests = module.enumerate_reachable_data_derive_requests();

        Assert.DoesNotContain(requests, request => request.requesting_callable_name.EndsWith(".helper"));

        var serializeRequest = Assert.Single(requests.Where(request =>
            request.operation_kind == HirDataDeriveOperationKind.serialize));
        Assert.Equal("app.User", serializeRequest.data_type_name);
        Assert.Equal(HirTypeKind.structure, serializeRequest.data_type_kind);
        Assert.Equal(HirDataContainerKind.object_fields, serializeRequest.container_kind);

        var deserializeRequest = Assert.Single(requests.Where(request =>
            request.operation_kind == HirDataDeriveOperationKind.deserialize));
        Assert.Equal("app.Session", deserializeRequest.data_type_name);
        Assert.Equal(HirTypeKind.@class, deserializeRequest.data_type_kind);
        Assert.Equal(HirDataContainerKind.object_fields, deserializeRequest.container_kind);
    }

    private static HirModule build_hir(string source, string fileName)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("data_shape", "jvm-openjdk-linux-managed", fileName);
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
        return compiler.build_hir(stagedAst, semantics, plan);
    }
}
