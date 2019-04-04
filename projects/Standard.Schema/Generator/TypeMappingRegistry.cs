namespace Hermes.Generator;

public sealed class TypeMappingRegistry
{
    private readonly Dictionary<string, Dictionary<string, string>> _mappings = new();

    public void Register(string targetLanguage, string SchemaType, string targetType)
    {
        if (!_mappings.TryGetValue(targetLanguage, out var langMap))
        {
            langMap = new Dictionary<string, string>();
            _mappings[targetLanguage] = langMap;
        }

        langMap[SchemaType] = targetType;
    }

    public string? Map(string targetLanguage, string SchemaType)
    {
        if (_mappings.TryGetValue(targetLanguage, out var langMap) &&
            langMap.TryGetValue(SchemaType, out var targetType)) return targetType;
        return null;
    }

    public static TypeMappingRegistry CreateDefault()
    {
        var registry = new TypeMappingRegistry();

        var ts = "typescript";
        registry.Register(ts, "i8", "number");
        registry.Register(ts, "i16", "number");
        registry.Register(ts, "i32", "number");
        registry.Register(ts, "i64", "number");
        registry.Register(ts, "u8", "number");
        registry.Register(ts, "u16", "number");
        registry.Register(ts, "u32", "number");
        registry.Register(ts, "u64", "number");
        registry.Register(ts, "f32", "number");
        registry.Register(ts, "f64", "number");
        registry.Register(ts, "bool", "boolean");
        registry.Register(ts, "utf8", "string");
        registry.Register(ts, "utf16", "string");
        registry.Register(ts, "uuid", "string");
        registry.Register(ts, "datetime", "Date");
        registry.Register(ts, "decimal", "number");
        registry.Register(ts, "unit", "void");

        var java = "java";
        registry.Register(java, "i32", "Integer");
        registry.Register(java, "i64", "Long");
        registry.Register(java, "f32", "Float");
        registry.Register(java, "f64", "Double");
        registry.Register(java, "bool", "Boolean");
        registry.Register(java, "utf8", "String");
        registry.Register(java, "utf16", "String");
        registry.Register(java, "uuid", "java.util.UUID");
        registry.Register(java, "datetime", "java.time.LocalDateTime");
        registry.Register(java, "decimal", "java.math.BigDecimal");

        var go = "go";
        registry.Register(go, "i32", "int32");
        registry.Register(go, "i64", "int64");
        registry.Register(go, "u32", "uint32");
        registry.Register(go, "u64", "uint64");
        registry.Register(go, "f32", "float32");
        registry.Register(go, "f64", "float64");
        registry.Register(go, "bool", "bool");
        registry.Register(go, "utf8", "string");
        registry.Register(go, "utf16", "string");
        registry.Register(go, "uuid", "github.com/google/uuid.UUID");
        registry.Register(go, "datetime", "time.Time");
        registry.Register(go, "decimal", "github.com/shopspring/decimal.Decimal");

        var py = "python";
        registry.Register(py, "i32", "int");
        registry.Register(py, "i64", "int");
        registry.Register(py, "f32", "float");
        registry.Register(py, "f64", "float");
        registry.Register(py, "bool", "bool");
        registry.Register(py, "utf8", "str");
        registry.Register(py, "utf16", "str");
        registry.Register(py, "uuid", "uuid.UUID");
        registry.Register(py, "datetime", "datetime.datetime");
        registry.Register(py, "decimal", "decimal.Decimal");

        return registry;
    }
}