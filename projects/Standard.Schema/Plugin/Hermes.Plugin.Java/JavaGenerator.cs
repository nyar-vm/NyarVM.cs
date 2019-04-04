using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.Java;

public sealed class JavaGenerator : IGenerator
{
    public string Name => "java";
    public string[] SupportedTargets => ["pojo", "jackson", "spring-data", "jpa"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var package = context.Options.TryGetValue("package", out var pkgVal)
            ? pkgVal.ToString() ?? "com.hermes.models"
            : "com.hermes.models";
        var target = context.Options.TryGetValue("targets", out var targetVal)
            ? targetVal.ToString() ?? "pojo"
            : "pojo";

        foreach (var classDef in context.Schema.Classes)
            result.Files.Add(GenerateClass(classDef, package, target, context));

        foreach (var structDef in context.Schema.Structures)
            result.Files.Add(GenerateValueObject(structDef, package, target, context));

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
            result.Files.Add(GenerateModelClass(model, package, target, context));

        foreach (var enumDef in context.Schema.Enums) result.Files.Add(GenerateEnum(enumDef, package, context));

        foreach (var flagsDef in context.Schema.Flags) result.Files.Add(GenerateFlags(flagsDef, package, context));

        foreach (var unionDef in context.Schema.Unions)
            result.Files.Add(GenerateUnion(unionDef, package, target, context));

        if (target is "jpa" or "spring-data")
            foreach (var storage in context.Schema.Storages)
            foreach (var model in storage.Models)
                result.Files.Add(GenerateJpaRepository(model, storage, package, context));

        if (target == "jpa")
        {
            foreach (var service in context.Schema.Services)
                result.Files.Add(GenerateSpringController(service, package, context));

            result.Files.Add(GenerateJpaDdlScript(context.Schema, context.SchemaPath, context.OutputPath));
        }

        return result;
    }

    private GeneratedFile GenerateClass(ClassDefinition classDef, string package, string target,
        GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();

        var imports = CollectImports(classDef, target);
        foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"import {imp};");

        if (imports.Count > 0) sb.AppendLine();

        if (target == "jackson")
            sb.AppendLine(
                "@com.fasterxml.jackson.annotation.JsonInclude(com.fasterxml.jackson.annotation.JsonInclude.Include.NON_NULL)");

        sb.AppendLine($"public class {classDef.Name} {{");
        sb.AppendLine();

        #region 字段声明

        foreach (var field in classDef.fields)
        {
            var javaType = MapType(field.FieldType);
            var fieldName = ToCamelCase(field.Name);

            if (target == "jackson" && field.IsOptional)
                sb.AppendLine($"    @com.fasterxml.jackson.annotation.JsonProperty(\"{fieldName}\")");

            if (target == "spring-data" && field.Attributes.Any(a => a.Name == "id" || a.Name == "Id"))
                sb.AppendLine("    @org.springframework.data.annotation.Id");

            sb.AppendLine($"    private {javaType} {fieldName};");
            sb.AppendLine();
        }

        #endregion

        #region 构造函数

        sb.AppendLine($"    public {classDef.Name}() {{");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (classDef.fields.Count > 0)
        {
            var paramList = string.Join(", ",
                classDef.fields.Select(f => $"{MapType(f.FieldType)} {ToCamelCase(f.Name)}"));
            sb.AppendLine($"    public {classDef.Name}({paramList}) {{");
            foreach (var field in classDef.fields)
                sb.AppendLine($"        this.{ToCamelCase(field.Name)} = {ToCamelCase(field.Name)};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        #region Getter/Setter

        foreach (var field in classDef.fields)
        {
            var javaType = MapType(field.FieldType);
            var fieldName = ToCamelCase(field.Name);
            var capitalName = ToPascalCase(field.Name);

            sb.AppendLine($"    public {javaType} get{capitalName}() {{");
            sb.AppendLine($"        return {fieldName};");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine($"    public void set{capitalName}({javaType} {fieldName}) {{");
            sb.AppendLine($"        this.{fieldName} = {fieldName};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        #region equals/hashCode/toString

        sb.AppendLine("    @Override");
        sb.AppendLine("    public boolean equals(Object o) {");
        sb.AppendLine("        if (this == o) return true;");
        sb.AppendLine("        if (o == null || getClass() != o.getClass()) return false;");
        sb.AppendLine($"        {classDef.Name} that = ({classDef.Name}) o;");
        var eqChecks = classDef.fields.Select(f =>
            $"java.util.Objects.equals(this.{ToCamelCase(f.Name)}, that.{ToCamelCase(f.Name)})");
        sb.AppendLine($"        return {string.Join(" && ", eqChecks)};");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    @Override");
        sb.AppendLine("    public int hashCode() {");
        var hashFields = classDef.fields.Select(f => ToCamelCase(f.Name)).ToList();
        sb.AppendLine($"        return java.util.Objects.hash({string.Join(", ", hashFields)});");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    @Override");
        sb.AppendLine("    public String toString() {");
        var fieldStrs = classDef.fields.Select(f => $"\"{ToCamelCase(f.Name)}=\" + {ToCamelCase(f.Name)}");
        sb.AppendLine($"        return \"{classDef.Name}{{\" + {string.Join(" + \", \" + ", fieldStrs)} + \"}}\";");
        sb.AppendLine("    }");

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{classDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name
        };
    }

    /// <summary>
    ///     生成不可变值类型（final record）
    /// </summary>
    private GeneratedFile GenerateValueObject(StructureDefinition structDef, string package, string target,
        GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();

        var imports = new HashSet<string>();
        foreach (var field in structDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "jackson")
        {
            imports.Add("com.fasterxml.jackson.annotation.JsonInclude");
            imports.Add("com.fasterxml.jackson.annotation.JsonProperty");
        }

        foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"import {imp};");

        if (imports.Count > 0) sb.AppendLine();

        if (target == "jackson")
            sb.AppendLine(
                "@com.fasterxml.jackson.annotation.JsonInclude(com.fasterxml.jackson.annotation.JsonInclude.Include.NON_NULL)");

        var paramList = string.Join(", ", structDef.fields.Select(f =>
        {
            var javaType = MapType(f.FieldType);
            var fieldName = ToCamelCase(f.Name);
            var jacksonProp = target == "jackson" && f.IsOptional
                ? $"@com.fasterxml.jackson.annotation.JsonProperty(\"{fieldName}\") "
                : "";
            return $"{jacksonProp}{javaType} {fieldName}";
        }));

        sb.AppendLine("/**");
        sb.AppendLine($" * 值对象 {structDef.Name}——不可变、按值比较、无独立标识");
        sb.AppendLine(" */");
        sb.AppendLine($"public final record {structDef.Name}({paramList}) {{");

        #region 验证逻辑

        var hasValidation = false;
        foreach (var field in structDef.fields)
        {
            var minAttr = field.Attributes.FirstOrDefault(a => a.Name is "min" or "Min");
            var maxAttr = field.Attributes.FirstOrDefault(a => a.Name is "max" or "Max");
            var lenAttr = field.Attributes.FirstOrDefault(a =>
                a.Name is "len" or "Len" or "maxLength" or "maxlength" or "max_length");

            if (minAttr != null || maxAttr != null || lenAttr != null)
            {
                hasValidation = true;
                break;
            }
        }

        if (hasValidation)
        {
            sb.AppendLine();
            sb.AppendLine($"    public {structDef.Name} {{");

            foreach (var field in structDef.fields)
            {
                var fieldName = ToCamelCase(field.Name);
                var propName = ToPascalCase(field.Name);

                var minAttr = field.Attributes.FirstOrDefault(a => a.Name is "min" or "Min");
                var maxAttr = field.Attributes.FirstOrDefault(a => a.Name is "max" or "Max");
                var lenAttr = field.Attributes.FirstOrDefault(a =>
                    a.Name is "len" or "Len" or "maxLength" or "maxlength" or "max_length");

                if (minAttr != null && minAttr.Arguments.Count > 0 &&
                    int.TryParse(minAttr.Arguments[0].Value, out var minVal))
                {
                    sb.AppendLine($"        if ({fieldName} < {minVal}) {{");
                    sb.AppendLine($"            throw new IllegalArgumentException(\"{propName} 不能小于 {minVal}\");");
                    sb.AppendLine("        }");
                }

                if (maxAttr != null && maxAttr.Arguments.Count > 0 &&
                    int.TryParse(maxAttr.Arguments[0].Value, out var maxVal))
                {
                    sb.AppendLine($"        if ({fieldName} > {maxVal}) {{");
                    sb.AppendLine($"            throw new IllegalArgumentException(\"{propName} 不能大于 {maxVal}\");");
                    sb.AppendLine("        }");
                }

                if (lenAttr != null && lenAttr.Arguments.Count > 0 &&
                    int.TryParse(lenAttr.Arguments[0].Value, out var maxLen))
                {
                    sb.AppendLine($"        if ({fieldName} != null && {fieldName}.length() > {maxLen}) {{");
                    sb.AppendLine($"            throw new IllegalArgumentException(\"{propName} 长度不能超过 {maxLen}\");");
                    sb.AppendLine("        }");
                }
            }

            sb.AppendLine("    }");
        }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{structDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = structDef.Name
        };
    }

    private GeneratedFile GenerateModelClass(ModelDefinition modelDef, string package, string target,
        GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();

        var imports = new HashSet<string>();
        foreach (var field in modelDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "jackson")
        {
            imports.Add("com.fasterxml.jackson.annotation.JsonInclude");
            imports.Add("com.fasterxml.jackson.annotation.JsonProperty");
        }

        if (target == "spring-data") imports.Add("org.springframework.data.annotation.Id");

        if (target == "jpa")
        {
            imports.Add("jakarta.persistence.Entity");
            imports.Add("jakarta.persistence.Table");
            imports.Add("jakarta.persistence.Column");
            imports.Add("jakarta.persistence.Id");
            imports.Add("jakarta.persistence.GeneratedValue");
            imports.Add("jakarta.persistence.GenerationType");
        }

        foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"import {imp};");

        if (imports.Count > 0) sb.AppendLine();

        if (target == "jackson")
            sb.AppendLine(
                "@com.fasterxml.jackson.annotation.JsonInclude(com.fasterxml.jackson.annotation.JsonInclude.Include.NON_NULL)");

        if (target == "jpa")
        {
            sb.AppendLine("@Entity");
            sb.AppendLine($"@Table(name = \"{ToSnakeCase(modelDef.Name)}\")");
        }

        if (modelDef.DocComment is not null)
        {
            sb.AppendLine("/**");
            sb.AppendLine($" * {modelDef.DocComment}");
            sb.AppendLine(" */");
        }

        sb.AppendLine($"public class {modelDef.Name} {{");
        sb.AppendLine();

        #region 字段声明

        foreach (var field in modelDef.fields)
        {
            var javaType = MapType(field.FieldType);
            var fieldName = ToCamelCase(field.Name);

            if (field.DocComment is not null) sb.AppendLine($"    /** {field.DocComment} */");

            if (target == "jackson" && field.IsOptional)
                sb.AppendLine($"    @com.fasterxml.jackson.annotation.JsonProperty(\"{fieldName}\")");

            var isKey =
                field.Attributes.Any(a => a.Name == "key" || a.Name == "Key" || a.Name == "id" || a.Name == "Id");
            if (target == "spring-data" && isKey) sb.AppendLine("    @org.springframework.data.annotation.Id");

            if (target == "jpa" && isKey)
            {
                sb.AppendLine("    @Id");
                sb.AppendLine("    @GeneratedValue(strategy = GenerationType.IDENTITY)");
            }

            if (target == "jpa" && !isKey)
            {
                var colName = ToSnakeCase(field.Name);
                sb.AppendLine(
                    $"    @Column(name = \"{colName}\"{(field.IsOptional ? ", nullable = true" : ", nullable = false")})");
            }

            sb.AppendLine($"    private {javaType} {fieldName};");
            sb.AppendLine();
        }

        #endregion

        #region 构造函数

        sb.AppendLine($"    public {modelDef.Name}() {{");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (modelDef.fields.Count > 0)
        {
            var paramList = string.Join(", ",
                modelDef.fields.Select(f => $"{MapType(f.FieldType)} {ToCamelCase(f.Name)}"));
            sb.AppendLine($"    public {modelDef.Name}({paramList}) {{");
            foreach (var field in modelDef.fields)
                sb.AppendLine($"        this.{ToCamelCase(field.Name)} = {ToCamelCase(field.Name)};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        #region Getter/Setter

        foreach (var field in modelDef.fields)
        {
            var javaType = MapType(field.FieldType);
            var fieldName = ToCamelCase(field.Name);
            var capitalName = ToPascalCase(field.Name);

            sb.AppendLine($"    public {javaType} get{capitalName}() {{");
            sb.AppendLine($"        return {fieldName};");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine($"    public void set{capitalName}({javaType} {fieldName}) {{");
            sb.AppendLine($"        this.{fieldName} = {fieldName};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        #region 计算属性

        foreach (var getter in modelDef.Getters)
        {
            var javaReturnType = getter.ReturnType != null ? MapType(getter.ReturnType) : "Object";
            var capitalName = ToPascalCase(getter.Name);
            var javaBody = ConvertGetterBody(getter.Body);

            if (getter.DocComment is not null) sb.AppendLine($"    /** {getter.DocComment} */");

            sb.AppendLine($"    public {javaReturnType} get{capitalName}() {{");
            sb.AppendLine($"        return {javaBody};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        #endregion

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{modelDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name
        };
    }

    private GeneratedFile GenerateEnum(EnumDefinition enumDef, string package, GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();
        sb.AppendLine($"public enum {enumDef.Name} {{");
        sb.AppendLine();

        var members = enumDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var suffix = i < members.Count - 1 ? "," : ";";
            sb.AppendLine($"    {member.Name}({member.Value}){suffix}");
        }

        sb.AppendLine();
        sb.AppendLine("    private final int value;");
        sb.AppendLine();
        sb.AppendLine($"    {enumDef.Name}(int value) {{");
        sb.AppendLine("        this.value = value;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public int getValue() {");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{enumDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name
        };
    }

    private GeneratedFile GenerateFlags(FlagsDefinition flagsDef, string package, GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();
        sb.AppendLine($"public enum {flagsDef.Name} {{");
        sb.AppendLine();

        var members = flagsDef.Members.ToList();
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            var suffix = i < members.Count - 1 ? "," : ";";
            sb.AppendLine($"    {member.Name}(1 << {member.Value}){suffix}");
        }

        sb.AppendLine();
        sb.AppendLine("    private final int value;");
        sb.AppendLine();
        sb.AppendLine($"    {flagsDef.Name}(int value) {{");
        sb.AppendLine("        this.value = value;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public int getValue() {");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public boolean hasFlag(int flags) {");
        sb.AppendLine("        return (flags & value) != 0;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static int combine({flagsDef.Name}... flags) {");
        sb.AppendLine("        int result = 0;");
        sb.AppendLine("        for (var flag : flags) {");
        sb.AppendLine("            result |= flag.value;");
        sb.AppendLine("        }");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{flagsDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name
        };
    }

    private GeneratedFile GenerateUnion(UnionDefinition unionDef, string package, string target,
        GeneratorContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package};");
        sb.AppendLine();
        sb.AppendLine("import java.util.Optional;");
        sb.AppendLine();

        if (target == "jackson")
        {
            sb.AppendLine(
                "@com.fasterxml.jackson.annotation.JsonTypeInfo(use = com.fasterxml.jackson.annotation.JsonTypeInfo.Id.NAME, include = com.fasterxml.jackson.annotation.JsonTypeInfo.As.PROPERTY, property = \"kind\")");
            sb.AppendLine("@com.fasterxml.jackson.annotation.JsonSubTypes({");
            var subTypes = unionDef.Variants
                .Select(v =>
                    $"    @com.fasterxml.jackson.annotation.JsonSubTypes.type(value = {unionDef.Name}{v.Name}.class, name = \"{v.Name}\")")
                .ToList();
            sb.AppendLine(string.Join(",\n", subTypes));
            sb.AppendLine("})");
        }

        sb.AppendLine(
            $"public sealed interface {unionDef.Name} permits {string.Join(", ", unionDef.Variants.Select(v => $"{unionDef.Name}{v.Name}"))} {{");
        sb.AppendLine();
        sb.AppendLine("    String kind();");
        sb.AppendLine();

        foreach (var variant in unionDef.Variants)
        {
            sb.AppendLine(
                $"    static {unionDef.Name} {ToCamelCase(variant.Name)}({(variant.Payload != null ? MapType(variant.Payload) + " value" : "")}) {{");
            sb.AppendLine(
                $"        return new {unionDef.Name}{variant.Name}({(variant.Payload != null ? "value" : "")});");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        foreach (var variant in unionDef.Variants)
        {
            sb.AppendLine();
            sb.AppendLine(
                $"public record {unionDef.Name}{variant.Name}({(variant.Payload != null ? MapType(variant.Payload) + " value" : "")}) implements {unionDef.Name} {{");

            if (target == "jackson") sb.AppendLine("    @com.fasterxml.jackson.annotation.JsonProperty(\"kind\")");

            sb.AppendLine("    @Override");
            sb.AppendLine($"    public String kind() {{ return \"{variant.Name}\"; }}");
            sb.AppendLine("}");
        }

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, $"{unionDef.Name}.java"),
            Content = sb.ToString(),
            Generator = Name
        };
    }

    private HashSet<string> CollectImports(ClassDefinition classDef, string target)
    {
        var imports = new HashSet<string>();

        foreach (var field in classDef.fields) CollectTypeImports(field.FieldType, imports);

        if (target == "jackson")
        {
            imports.Add("com.fasterxml.jackson.annotation.JsonInclude");
            imports.Add("com.fasterxml.jackson.annotation.JsonProperty");
        }

        if (target == "spring-data") imports.Add("org.springframework.data.annotation.Id");

        return imports;
    }

    private void CollectTypeImports(SchemaType type, HashSet<string> imports)
    {
        switch (type)
        {
            case ListType:
                imports.Add("java.util.List");
                break;
            case DictType:
                imports.Add("java.util.Map");
                break;
            case OptionType:
                imports.Add("java.util.Optional");
                break;
            case ResultType:
                imports.Add("java.util.Optional");
                break;
            case ArrayType:
                break;
            case RecordType:
                imports.Add("java.util.Map");
                break;
        }
    }

    private string MapType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "byte",
                "i16" => "short",
                "i32" => "int",
                "i64" => "long",
                "u8" => "short",
                "u16" => "int",
                "u32" => "long",
                "u64" => "long",
                "f32" => "float",
                "f64" => "double",
                "bool" => "boolean",
                "utf8" => "String",
                "utf16" => "String",
                "uuid" => "java.util.UUID",
                "unit" => "void",
                "object" => "Object"
            },
            ListType l => $"List<{MapType(l.ElementType)}>",
            ArrayType a => $"{MapTypeBoxed(a.ElementType)}[]",
            OptionType o => $"Optional<{MapTypeBoxed(o.InnerType)}>",
            DictType d => $"Map<{MapTypeBoxed(d.KeyType)}, {MapTypeBoxed(d.ValueType)}>",
            RecordType r => $"Map<{MapTypeBoxed(r.KeyType)}, {MapTypeBoxed(r.ValueType)}>",
            ResultType r => r.ErrorType != null
                ? $"Optional<{MapTypeBoxed(r.OkType)}>"
                : $"Optional<{MapTypeBoxed(r.OkType)}>",
            StreamType s => $"java.util.stream.Stream<{MapTypeBoxed(s.InnerType)}>",
            ReferenceType refT => MapType(refT.ReferencedType),
            NamedType n => n.Name,
            _ => "Object"
        };
    }

    private string MapTypeBoxed(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => "Byte",
                "i16" => "Short",
                "i32" => "Integer",
                "i64" => "Long",
                "u8" => "Short",
                "u16" => "Integer",
                "u32" => "Long",
                "u64" => "Long",
                "f32" => "Float",
                "f64" => "Double",
                "bool" => "Boolean",
                "utf8" => "String",
                "utf16" => "String",
                "uuid" => "java.util.UUID",
                "unit" => "Void",
                "object" => "Object"
            },
            ListType l => $"List<{MapTypeBoxed(l.ElementType)}>",
            ArrayType a => $"{MapTypeBoxed(a.ElementType)}[]",
            OptionType o => $"Optional<{MapTypeBoxed(o.InnerType)}>",
            DictType d => $"Map<{MapTypeBoxed(d.KeyType)}, {MapTypeBoxed(d.ValueType)}>",
            RecordType r => $"Map<{MapTypeBoxed(r.KeyType)}, {MapTypeBoxed(r.ValueType)}>",
            ResultType r => $"Optional<{MapTypeBoxed(r.OkType)}>",
            StreamType s => $"java.util.stream.Stream<{MapTypeBoxed(s.InnerType)}>",
            ReferenceType refT => MapTypeBoxed(refT.ReferencedType),
            NamedType n => n.Name,
            _ => "Object"
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string ConvertGetterBody(string hermesBody)
    {
        var parts = hermesBody.Split('.');
        var javaParts = new List<string>();
        for (var i = 0; i < parts.Length; i++)
            if (i == 0 && parts[i] == "self")
                javaParts.Add("this");
            else
                javaParts.Add(ToCamelCase(parts[i]));

        return string.Join(".", javaParts);
    }

    private string GenerateFileHeader(string schemaPath, string outputPath)
    {
        var relativePath = schemaPath;
        if (!string.IsNullOrEmpty(outputPath))
            try
            {
                relativePath = Path.GetRelativePath(outputPath, schemaPath);
            }
            catch
            {
            }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// 本文件由 atlas/hermes 命令自动生成，修改无效");
        sb.AppendLine($"// 源文件: {relativePath}");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        return sb.ToString();
    }

    #region JPA Repository 生成

    private GeneratedFile GenerateJpaRepository(ModelDefinition modelDef, StorageDefinition storage, string package,
        GeneratorContext context)
    {
        var sb = new StringBuilder();
        var modelName = modelDef.Name;
        var repoName = $"{modelName}Repository";
        var keyType = GetJavaKeyType(modelDef);

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package}.repository;");
        sb.AppendLine();
        sb.AppendLine($"import {package}.{modelName};");
        sb.AppendLine("import org.springframework.data.jpa.repository.JpaRepository;");
        sb.AppendLine("import org.springframework.data.jpa.repository.Query;");
        sb.AppendLine("import org.springframework.data.repository.query.Param;");
        sb.AppendLine("import org.springframework.stereotype.Repository;");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine($" * {modelName} JPA Repository");
        sb.AppendLine(" */");
        sb.AppendLine("@Repository");
        sb.AppendLine($"public interface {repoName} extends JpaRepository<{modelName}, {keyType}> {{");

        var keyField = GetKeyField(modelDef);
        var keyPropName = keyField != null ? ToPascalCase(keyField.Name) : "Id";

        sb.AppendLine();
        sb.AppendLine("    /**");
        sb.AppendLine("     * 根据主键查找");
        sb.AppendLine("     */");
        sb.AppendLine(
            $"    boolean existsBy{keyPropName}(@Param(\"{ToCamelCase(keyPropName)}\") {keyType} {ToCamelCase(keyPropName)});");
        sb.AppendLine();

        foreach (var field in modelDef.fields)
            if (field.IsForeignKey)
            {
                var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
                if (refAttr != null && refAttr.Arguments.Count > 0)
                {
                    var targetEntity = refAttr.Arguments[0].Value;
                    var propName = ToPascalCase(field.Name);
                    sb.AppendLine("    /**");
                    sb.AppendLine($"     * 根据 {propName} 查找列表");
                    sb.AppendLine("     */");
                    sb.AppendLine(
                        $"    java.util.List<{modelName}> findBy{propName}(@Param(\"{ToCamelCase(field.Name)}\") {MapType(field.FieldType)} {ToCamelCase(field.Name)});");
                    sb.AppendLine();
                }
            }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, "repository", $"{repoName}.java"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = repoName
        };
    }

    #endregion

    #region Spring Controller 生成

    private GeneratedFile GenerateSpringController(ServiceDefinition service, string package, GeneratorContext context)
    {
        var sb = new StringBuilder();
        var controllerName = $"{service.Name}Controller";

        sb.AppendLine(GenerateFileHeader(context.SchemaPath, context.OutputPath));
        sb.AppendLine($"package {package}.controller;");
        sb.AppendLine();
        sb.AppendLine("import org.springframework.http.ResponseEntity;");
        sb.AppendLine("import org.springframework.web.bind.annotation.*;");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine($" * {service.Name} 控制器");
        sb.AppendLine(" */");
        sb.AppendLine("@RestController");
        sb.AppendLine($"@RequestMapping(\"/api/{ToKebabCase(service.Name)}\")");
        sb.AppendLine($"public class {controllerName} {{");

        foreach (var endpoint in service.Endpoints)
        {
            var methodName = ToCamelCase(endpoint.Name);
            var returnTypeName = endpoint.ReturnType != null ? MapType(endpoint.ReturnType) : "void";
            var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";

            sb.AppendLine();
            sb.AppendLine("    /**");
            sb.AppendLine($"     * {endpoint.Name}");
            sb.AppendLine("     */");

            if (endpoint is HttpEndpoint httpEp)
            {
                var httpMethod = httpEp.HttpMethod.ToUpperInvariant();
                var mappingAttr = httpMethod switch
                {
                    "GET" => "GetMapping",
                    "POST" => "PostMapping",
                    "PUT" => "PutMapping",
                    "DELETE" => "DeleteMapping",
                    "PATCH" => "PatchMapping",
                    _ => "GetMapping"
                };

                var routePath = httpEp.Path ?? $"/{ToKebabCase(endpoint.Name)}";
                sb.AppendLine($"    @{mappingAttr}(\"{routePath}\")");
            }
            else
            {
                sb.AppendLine("    @PostMapping");
            }

            var paramList = new List<string>();
            var hasBody = endpoint is HttpEndpoint ep && ep.HttpMethod.ToUpperInvariant() is "POST" or "PUT" or "PATCH";

            for (var i = 0; i < endpoint.Parameters.Count; i++)
            {
                var param = endpoint.Parameters[i];
                var paramType = MapType(param.ParameterType);
                var paramName = ToCamelCase(param.Name);

                if (hasBody && i == 0)
                    paramList.Add($"@RequestBody {paramType} {paramName}");
                else
                    paramList.Add($"@RequestParam {paramType} {paramName}");
            }

            var responseType =
                isVoid ? "ResponseEntity<Void>" : "ResponseEntity<" + MapTypeBoxed(endpoint.ReturnType!) + ">";
            sb.AppendLine($"    public {responseType} {methodName}({string.Join(", ", paramList)}) {{");
            sb.AppendLine("        // TODO: 实现业务逻辑");
            sb.AppendLine("        return ResponseEntity.ok().build();");
            sb.AppendLine("    }");
        }

        sb.AppendLine();
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(context.OutputPath, "controller", $"{controllerName}.java"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = controllerName
        };
    }

    #endregion

    #region JPA DDL 生成

    private GeneratedFile GenerateJpaDdlScript(SchemaIR schema, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Hermes 自动生成的 JPA DDL 脚本");
        sb.AppendLine($"-- Schema: {schema.Namespace}");
        sb.AppendLine($"-- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        foreach (var storage in schema.Storages)
        {
            sb.AppendLine("-- ========================================");
            sb.AppendLine($"-- Storage: {storage.Name}");
            sb.AppendLine("-- ========================================");
            sb.AppendLine();

            foreach (var model in storage.Models)
            {
                GenerateCreateTable(model, sb);
                sb.AppendLine();
            }

            foreach (var model in storage.Models) GenerateJpaIndexes(model, sb);

            foreach (var model in storage.Models) GenerateJpaForeignKeys(model, sb);
        }

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{ToPascalCase(schema.Namespace ?? "hermes")}_ddl.sql"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "JpaDdl"
        };
    }

    private void GenerateCreateTable(ModelDefinition model, StringBuilder sb)
    {
        var tableName = ToSnakeCase(model.Name);
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS \"{tableName}\"");
        sb.AppendLine("(");

        var columns = new List<string>();

        foreach (var field in model.fields)
        {
            var colName = ToSnakeCase(field.Name);
            var sqlType = MapJavaSqlType(field.FieldType);
            var nullable = field.IsOptional ? "" : " NOT NULL";

            if (IsKeyField(field))
            {
                columns.Add($"    \"{colName}\" {sqlType}{nullable} PRIMARY KEY AUTOINCREMENT");
            }
            else
            {
                var defaultValue = field.IsOptional ? " DEFAULT NULL" : "";
                columns.Add($"    \"{colName}\" {sqlType}{nullable}{defaultValue}");
            }
        }

        sb.AppendLine(string.Join(",\n", columns));
        sb.AppendLine(");");
    }

    private void GenerateJpaIndexes(ModelDefinition model, StringBuilder sb)
    {
        var tableName = ToSnakeCase(model.Name);

        foreach (var field in model.fields)
        {
            var uniqueAttr = field.Attributes.FirstOrDefault(a => a.Name is "unique" or "Unique");
            if (uniqueAttr != null)
            {
                var colName = ToSnakeCase(field.Name);
                sb.AppendLine(
                    $"CREATE UNIQUE INDEX IF NOT EXISTS \"ix_{tableName}_{colName}\" ON \"{tableName}\" (\"{colName}\");");
            }
        }
    }

    private void GenerateJpaForeignKeys(ModelDefinition model, StringBuilder sb)
    {
        var tableName = ToSnakeCase(model.Name);

        foreach (var field in model.fields)
        {
            if (!field.IsForeignKey) continue;

            var refAttr = field.Attributes.FirstOrDefault(a => a.Name is "ref" or "Ref" or "fk" or "Fk");
            if (refAttr != null && refAttr.Arguments.Count > 0)
            {
                var targetEntity = refAttr.Arguments[0].Value;
                var colName = ToSnakeCase(field.Name);
                var targetTable = ToSnakeCase(targetEntity);
                var targetCol = "id";

                if (refAttr.Arguments.Count > 1) targetCol = ToSnakeCase(refAttr.Arguments[1].Value);

                var fkName = $"fk_{tableName}_{colName}";
                sb.AppendLine($"ALTER TABLE \"{tableName}\" ADD CONSTRAINT \"{fkName}\"");
                sb.AppendLine($"    FOREIGN KEY (\"{colName}\") REFERENCES \"{targetTable}\" (\"{targetCol}\");");
            }
        }
    }

    private static string MapJavaSqlType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" or "i16" => "SMALLINT",
                "i32" => "INTEGER",
                "i64" => "BIGINT",
                "u8" or "u16" => "INTEGER",
                "u32" or "u64" => "BIGINT",
                "f32" => "REAL",
                "f64" => "DOUBLE",
                "bool" => "BOOLEAN",
                "utf8" or "utf16" => "TEXT",
                "uuid" => "TEXT",
                "bytes" => "BLOB",
                _ => "TEXT"
            },
            ListType => "TEXT",
            DictType => "TEXT",
            NamedType n => n.Name switch
            {
                "datetime" => "TEXT",
                "decimal" => "REAL",
                _ => "TEXT"
            },
            _ => "TEXT"
        };
    }

    #endregion

    #region JPA 辅助方法

    private string GetJavaKeyType(ModelDefinition model)
    {
        var keyField = GetKeyField(model);
        if (keyField != null) return MapTypeBoxed(keyField.FieldType);

        return "Long";
    }

    private static FieldDefinition? GetKeyField(ModelDefinition model)
    {
        foreach (var field in model.fields)
            if (IsKeyField(field))
                return field;

        return null;
    }

    private static bool IsKeyField(FieldDefinition field)
    {
        return field.Attributes.Any(a => a.Name is "key" or "Key" or "id" or "Id");
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('-');

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    #endregion
}