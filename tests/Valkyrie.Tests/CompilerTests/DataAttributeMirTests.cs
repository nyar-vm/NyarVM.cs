using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class DataAttributeMirTests
{
    [Fact]
    public void BuildMir_DataDeriveRequests_ShouldPreserveTypeKindAndContainerBoundary()
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

        var mir = build_mir(source, "data_derive_requests_mir.v");
        var requests = mir.enumerate_data_derive_requests();

        Assert.DoesNotContain(requests, request => request.requesting_callable_name.EndsWith(".helper"));

        var serializeRequest = Assert.Single(requests.Where(request =>
            request.operation_kind == Nyar.Language.Valkyrie.Compiler.Hir.HirDataDeriveOperationKind.serialize));
        Assert.Equal("app.User", serializeRequest.data_type_name);
        Assert.Equal(Nyar.Language.Valkyrie.Compiler.Hir.HirTypeKind.structure, serializeRequest.data_type_kind);
        Assert.Equal(Nyar.Language.Valkyrie.Compiler.Hir.HirDataContainerKind.object_fields,
            serializeRequest.container_kind);

        var deserializeRequest = Assert.Single(requests.Where(request =>
            request.operation_kind == Nyar.Language.Valkyrie.Compiler.Hir.HirDataDeriveOperationKind.deserialize));
        Assert.Equal("app.Session", deserializeRequest.data_type_name);
        Assert.Equal(Nyar.Language.Valkyrie.Compiler.Hir.HirTypeKind.@class, deserializeRequest.data_type_kind);
        Assert.Equal(Nyar.Language.Valkyrie.Compiler.Hir.HirDataContainerKind.object_fields,
            deserializeRequest.container_kind);
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("data_shape", "jvm-openjdk-linux-managed", fileName);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        return compiler.build_mir(hir, plan, targetProfile);
    }
}
