namespace Valkyrie.Tests.ParserTests;

public class GenericDeclarationParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_TraitMethodWithFunctionTypeParameter_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     trait Dict<K, V> {
                         micro iter(self, f: micro(K, V) -> unit): unit
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Single(unit.Declarations);
    }

    [Fact]
    public void Parse_ClassFieldWithNestedGenericType_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     structure Entry<K, V> {
                         key: K
                         value: V
                     }

                     class HashMap<K, V> {
                         buckets: List<List<Entry<K, V>>>
                         size: usize
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Equal(2, unit.Declarations.Count);
    }

    [Fact]
    public void Parse_ImplyMethodsAfterGenericReturnType_ShouldStayNested()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     class RingBuffer<T> {
                         count: usize
                     }

                     imply RingBuffer<T> {
                         micro peek(self): Option<T> {
                             return None
                         }
                         micro len(self): usize {
                             return self.count
                         }
                         micro is_empty(self): bool {
                             return self.count == 0
                         }
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var imply = Assert.Single(unit.Declarations.OfType<DeclareImply>());
        Assert.Equal(3, imply.Methods.Count);
        Assert.DoesNotContain(unit.Declarations.OfType<DeclareMicro>(),
            method => method.Name?.Name is "peek" or "len" or "is_empty");
    }

    [Fact]
    public void Parse_GenericTraitStructureClassAndImply_ShouldSucceed()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     trait Dict<K, V> {
                         micro get(self, key: K): Option<V>
                         micro clear(mut self): unit
                         micro iter(self, f: micro(K, V) -> unit): unit
                     }

                     structure Entry<K, V> {
                         key: K
                         value: V
                     }

                     class HashMap<K, V> {
                         buckets: List<List<Entry<K, V>>>
                         size: usize
                     }

                     imply HashMap<K, V>: Dict<K, V> {
                         micro get(self, key: K): Option<V> {
                             return None
                         }
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Equal(4, unit.Declarations.Count);
    }

    [Fact]
    public void Parse_ImplyWithObjectInitializer_ShouldStayNested()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     class RingBuffer<T> {
                         data: List<T>
                         capacity: usize
                         head: usize
                         tail: usize
                         count: usize
                     }

                     imply RingBuffer<T> {
                         micro new(capacity: usize): Self {
                             return Self { data: data, capacity: capacity, head: 0, tail: 0, count: 0 }
                         }
                         micro is_empty(self): bool {
                             return self.count == 0
                         }
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);

        var imply = Assert.Single(unit.Declarations.OfType<DeclareImply>());
        Assert.Equal(2, imply.Methods.Count);
        Assert.DoesNotContain(unit.Declarations.OfType<DeclareMicro>(),
            method => method.Name?.Name is "new" or "is_empty");
    }

    [Fact]
    public void Parse_TwoImplyBlocks_ShouldStayAsDeclarations()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     class HashMap<K, V> {
                         buckets: List<List<i32>>
                         size: usize
                     }

                     trait Map<K, V> {
                         micro get(self, key: K): Option<V>
                     }

                     imply HashMap<K, V>: Map<K, V> {
                         micro get(self, key: K): Option<V> {
                             return None
                         }
                     }

                     imply HashMap<K, V> {
                         micro new(capacity: usize): Self {
                             return Self { buckets: buckets, size: 0 }
                         }
                         micro length(self): usize {
                             return self.size
                         }
                     }
                     """;

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Equal(4, unit.Declarations.Count);
        Assert.Equal(2, unit.Declarations.OfType<DeclareImply>().Count());
        Assert.DoesNotContain(unit.Declarations.OfType<DeclareMicro>(),
            method => method.Name?.Name is "new" or "length");
    }

    [Fact]
    public void Parse_RealHashMapFile_ShouldKeepImplyBlocks()
    {
        var diagnostics = new DiagnosticSink();
        var source = File.ReadAllText(@"e:\RiderProjects\Valkyrie.cs\examples\std\source\collections\hash_map.v");

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Equal(2, unit.Declarations.OfType<DeclareImply>().Count());
        Assert.DoesNotContain(unit.Declarations.OfType<DeclareMicro>(),
            method => method.Name?.Name is "contains_key" or "length" or "is_empty");
    }

    [Fact]
    public void Parse_RealRingBufferFile_ShouldKeepImplyBlocks()
    {
        var diagnostics = new DiagnosticSink();
        var source = File.ReadAllText(@"e:\RiderProjects\Valkyrie.cs\examples\std\source\collections\ring_buffer.v");

        var unit = parse_with_timeout(source, diagnostics: diagnostics);

        assert_parse_result_not_null(unit);
        assert_no_errors(diagnostics);
        Assert.Single(unit.Declarations.OfType<DeclareImply>());
        Assert.DoesNotContain(unit.Declarations.OfType<DeclareMicro>(),
            method => method.Name?.Name is "enqueue" or "dequeue" or "is_empty");
    }
}
