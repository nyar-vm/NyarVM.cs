using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class TraitDispatchResolutionTests
{
    [Fact]
    public void BuildHir_WithInheritedClassMethod_ShouldResolveBaseMethod()
    {
        var source = """
                     namespace app;

                     class Animal {
                         micro speak(self) -> string {
                             "base"
                         }
                     }

                     class Dog(Animal) {
                     }

                     [main]
                     micro main() -> Unit {
                         let dog = Dog {}
                         dog.speak()
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.True(resolved);
        Assert.NotNull(resolution);
        Assert.Equal(HirDispatchKind.@static, resolution!.dispatch);
        Assert.True(resolution.inject_receiver);
        Assert.Equal("app.Animal.speak", resolution.target_name);
    }

    [Fact]
    public void BuildHir_WithClassInheritance_ShouldProjectPhysicalBaseEdge()
    {
        var source = """
                     namespace app;

                     class Animal {
                     }

                     class Dog(Animal) {
                     }
                     """;

        var module = build_hir(source);
        var dog = module.types.Single(type => type.name == "Dog");
        var edge = Assert.Single(dog.inheritance_edges);

        Assert.Equal("animal", edge.field_name);
        Assert.Equal("Animal", edge.base_type.name);
        Assert.Equal("Animal", edge.storage_type.name);
        Assert.Equal(HirInheritanceStorageKind.physical, edge.storage_kind);
    }

    [Fact]
    public void BuildHir_WithExplicitBaseCall_ShouldResolveBaseMethod()
    {
        var source = """
                     namespace app;

                     class Animal {
                         micro speak(self) -> string {
                             "animal"
                         }
                     }

                     class Dog(Animal) {
                         micro speak(self) -> string {
                             Animal::speak(self)
                         }
                     }

                     [main]
                     micro main() -> Unit {
                         let dog = Dog {}
                         dog.speak()
                     }
                     """;

        var module = build_hir(source);
        var dogMethod = module.types
            .Single(type => type.name == "Dog")
            .methods
            .Single(method => method.member_name == "speak");
        var call = Assert.IsType<TermCallExpression>(dogMethod.body!.Statements.Last());

        var resolved = module.try_resolve_call(dogMethod, call, out var resolution);

        Assert.True(resolved);
        Assert.NotNull(resolution);
        Assert.Equal(HirDispatchKind.@static, resolution!.dispatch);
        Assert.False(resolution.inject_receiver);
        Assert.Equal("app.Animal.speak", resolution.target_name);
    }

    [Fact]
    public void BuildHir_WithStructurallySatisfiedTraitDefaultMethod_ShouldResolveTraitMethod()
    {
        var source = """
                     namespace app;

                     trait Loggable {
                         micro log(self) -> string {
                             "default log"
                         }
                     }

                     class Data {
                     }

                     [main]
                     micro main() -> Unit {
                         let data = Data {}
                         data.log()
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.True(resolved);
        Assert.NotNull(resolution);
        Assert.Equal(HirDispatchKind.@static, resolution!.dispatch);
        Assert.True(resolution.inject_receiver);
        Assert.Equal("app.Loggable.log", resolution.target_name);
    }

    [Fact]
    public void BuildHir_WithoutClassOrTraitMethod_ShouldFallbackToVisibleMicro()
    {
        var source = """
                     namespace app;

                     class Dog {
                     }

                     micro greet(self: Dog) -> string {
                         "hello"
                     }

                     [main]
                     micro main() -> Unit {
                         let dog = Dog {}
                         dog.greet()
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.True(resolved);
        Assert.NotNull(resolution);
        Assert.Equal(HirDispatchKind.@static, resolution!.dispatch);
        Assert.True(resolution.inject_receiver);
        Assert.Equal("app.greet", resolution.target_name);
    }

    [Fact]
    public void BuildHir_WithMultipleTraitProviders_ShouldReportAmbiguity()
    {
        var source = """
                     namespace app;

                     trait Printable {
                         micro process(self) -> string {
                             "print"
                         }
                     }

                     trait Loggable {
                         micro process(self) -> string {
                             "log"
                         }
                     }

                     class Document {
                     }

                     [main]
                     micro main() -> Unit {
                         let doc = Document {}
                         doc.process()
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.False(resolved);
        Assert.Null(resolution);
        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK_TRAIT_AMBIGUOUS");
    }

    [Fact]
    public void BuildHir_WithQualifiedTraitCallOnStructurallySatisfiedExplicitImply_ShouldResolveProviderMethod()
    {
        var source = """
                     namespace app;

                     trait Printable {
                         micro process(self) -> string {
                             "print"
                         }
                     }

                     trait Loggable {
                         micro process(self) -> string {
                             "log"
                         }
                     }

                     class Document {
                     }

                     imply Document: Printable {
                     }

                     [main]
                     micro main() -> Unit {
                         let doc = Document {}
                         Loggable::process(doc)
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.True(resolved);
        Assert.NotNull(resolution);
        Assert.Equal(HirDispatchKind.@static, resolution!.dispatch);
        Assert.False(resolution.inject_receiver);
        Assert.Equal("app.Document.__trait_Printable_process", resolution.target_name);
        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK_TRAIT_SHAPE_ONLY");
    }

    [Fact]
    public void BuildHir_WithQualifiedTraitCallSatisfiedOnlyBySynthesizedDefault_ShouldFail()
    {
        var source = """
                     namespace app;

                     trait Loggable {
                         micro process(self) -> string {
                             "log"
                         }
                     }

                     class Document {
                     }

                     [main]
                     micro main() -> Unit {
                         let doc = Document {}
                         Loggable::process(doc)
                     }
                     """;

        var module = build_hir(source);
        var (mainCallable, call) = get_main_call(module);

        var resolved = module.try_resolve_call(mainCallable, call, out var resolution);

        Assert.False(resolved);
        Assert.Null(resolution);
    }

    [Fact]
    public void BuildHir_WithVirtualInheritanceModifier_ShouldProjectPhantomBaseEdge()
    {
        var baseClass = new DeclareClass
        {
            Name = new IdentifierNode("Base")
        };
        var derivedClass = new DeclareClass
        {
            Name = new IdentifierNode("Derived"),
            Inheritance = new InheritanceList
            {
                Bases =
                [
                    new InheritanceItem
                    {
                        Name = new IdentifierNode("base"),
                        Annotations = new Annotations
                        {
                            Modifiers = [new IdentifierNode("virtual")]
                        },
                        BaseType = new TypeLiteralNamePathNode
                        {
                            Path = new QualifiedPathNode
                            {
                                Segments = [new IdentifierNode("Base")]
                            }
                        }
                    }
                ]
            }
        };

        var syntax = new ProgramRoot([baseClass, derivedClass], "trait_dispatch.v", default);
        var semantics = new SemanticModel("trait_dispatch.v", new SymbolTable());
        var module = new HirBuilder().build(syntax, semantics, "trait_dispatch");
        var derived = module.types.Single(type => type.name == "Derived");
        var edge = Assert.Single(derived.inheritance_edges);

        Assert.Equal("base", edge.field_name);
        Assert.Equal("Base", edge.base_type.name);
        Assert.Equal("PhantomData<Base>", edge.storage_type.name);
        Assert.Equal(HirInheritanceStorageKind.phantom, edge.storage_kind);
    }

    [Fact]
    public void BuildHir_WithInvalidClassInheritance_ShouldReportDiagnostics()
    {
        var source = """
                     namespace app;

                     class Base {
                     }

                     class DuplicateBase(Base, Base) {
                     }

                     class FieldConflict(base: Base) {
                         base: i32
                     }

                     class CycleA(CycleB) {
                     }

                     class CycleB(CycleA) {
                     }
                     """;

        var module = build_hir(source);

        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK3001");
        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK3002");
        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK3004");
    }

    [Fact]
    public void BuildHir_WithNamedTypeArgumentsInTypePosition_ShouldPreserveAssociatedTypeBinding()
    {
        var source = """
                     namespace app;

                     trait IntoIterator {
                         type Item;
                     }

                     micro consume(items: IntoIterator<Item = i32>) -> Unit {
                     }
                     """;

        var module = build_hir(source);
        var consume = module.functions.Single(function => function.name == "app.consume");
        var parameter = Assert.Single(consume.parameters);
        var binding = Assert.Single(parameter.type.named_type_arguments!);

        Assert.Equal("IntoIterator", parameter.type.name);
        Assert.Equal("Item", binding.slot_name);
        Assert.Equal("i32", binding.type.name);
    }

    [Fact]
    public void BuildHir_LoopInWithUseSiteItemConstraintMismatch_ShouldReportDiagnostic()
    {
        var source = """
                     namespace app;

                     trait Iterator {
                         type Item;
                         micro has_next(self) -> bool;
                         micro next(mut self) -> i32;
                     }

                     trait IntoIterator {
                         type Item;
                         micro into_iterator(self) -> IntIterator;
                     }

                     structure IntIterator {
                     }

                     imply IntIterator: Iterator {
                         type Item = i32;

                         micro has_next(self) -> bool {
                             false
                         }

                         micro next(mut self) -> i32 {
                             0
                         }
                     }

                     [main]
                     micro main(items: IntoIterator<Item = bool>) -> Unit {
                         loop item in items {
                             item
                         }
                     }
                     """;

        var module = build_hir(source);

        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK_LOOP_ITEM_MISMATCH");
    }

    [Fact]
    public void BuildHir_LoopInWithGenericTraitBoundItemConstraintMismatch_ShouldReportDiagnostic()
    {
        var source = """
                     namespace app;

                     trait Iterator {
                         type Item;
                         micro has_next(self) -> bool;
                         micro next(mut self) -> i32;
                     }

                     trait IntoIterator {
                         type Item;
                         micro into_iterator(self) -> IntIterator;
                     }

                     structure IntIterator {
                     }

                     imply IntIterator: Iterator {
                         type Item = i32;

                         micro has_next(self) -> bool {
                             false
                         }

                         micro next(mut self) -> i32 {
                             0
                         }
                     }

                     micro consume<T>(items: T) -> Unit where T: IntoIterator<Item = bool> {
                         loop item in items {
                             item
                         }
                     }
                     """;

        var module = build_hir(source);

        Assert.Contains(module.semantics.diagnostics, diagnostic => diagnostic.code == "VALK_LOOP_ITEM_MISMATCH");
    }

    private static HirModule build_hir(string source)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("trait_dispatch", "jvm-openjdk-linux-managed", "trait_dispatch.v");
        var parseResult = compiler.parse_source(source, "trait_dispatch.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        return compiler.build_hir(stagedAst, semantics, plan);
    }

    private static (HirFunction MainCallable, TermCallExpression Call) get_main_call(HirModule module)
    {
        var mainCallable = module.functions.Single(function => function.is_logical_entry);
        var call = Assert.IsType<TermCallExpression>(mainCallable.body!.Statements.Last());
        return (mainCallable, call);
    }
}
