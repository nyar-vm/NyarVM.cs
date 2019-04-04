using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.TypeScript;

public sealed class TypeScriptGenerator : IIncrementalGenerator
{
    public string Name => "typescript";
    public string[] SupportedTargets => ["types", "react", "fetch", "zod"];

    public GeneratorResult Generate(GeneratorContext context)
    {
        var result = new GeneratorResult();
        var ns = GetNamespace(context.Options);

        foreach (var classDef in context.Schema.Classes)
            result.Files.Add(GenerateClassInterface(classDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
            result.Files.Add(GenerateModelInterface(model, ns, context.SchemaPath, context.OutputPath));

        foreach (var enumDef in context.Schema.Enums)
            result.Files.Add(GenerateEnumType(enumDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var structDef in context.Schema.Structures)
            result.Files.Add(GenerateValueObjectType(structDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var flagsDef in context.Schema.Flags)
            result.Files.Add(GenerateFlagsType(flagsDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var unionDef in context.Schema.Unions)
            result.Files.Add(GenerateUnionType(unionDef, ns, context.SchemaPath, context.OutputPath));

        foreach (var service in context.Schema.Services)
            result.Files.Add(GenerateServiceClient(service, ns, context.SchemaPath, context.OutputPath));

        result.Files.Add(GenerateRpcError(ns, context.SchemaPath, context.OutputPath));
        result.Files.Add(GenerateIndexFile(context.Schema, ns, context.SchemaPath, context.OutputPath));

        return result;
    }

    public GeneratorResult GenerateIncremental(IncrementalGeneratorContext context)
    {
        var result = new GeneratorResult();
        var ns = GetNamespace(context.Options);

        foreach (var classDef in context.Schema.Classes)
        {
            if (!context.ChangedTypeNames.Contains(classDef.Name)) continue;

            var file = GenerateClassInterface(classDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var structDef in context.Schema.Structures)
        {
            if (!context.ChangedTypeNames.Contains(structDef.Name)) continue;

            var file = GenerateValueObjectType(structDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var storage in context.Schema.Storages)
        foreach (var model in storage.Models)
        {
            if (!context.ChangedTypeNames.Contains(model.Name)) continue;

            var file = GenerateModelInterface(model, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var enumDef in context.Schema.Enums)
        {
            if (!context.ChangedTypeNames.Contains(enumDef.Name)) continue;

            var file = GenerateEnumType(enumDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var flagsDef in context.Schema.Flags)
        {
            if (!context.ChangedTypeNames.Contains(flagsDef.Name)) continue;

            var file = GenerateFlagsType(flagsDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var unionDef in context.Schema.Unions)
        {
            if (!context.ChangedTypeNames.Contains(unionDef.Name)) continue;

            var file = GenerateUnionType(unionDef, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        foreach (var service in context.Schema.Services)
        {
            if (!context.ChangedTypeNames.Contains(service.Name)) continue;

            var file = GenerateServiceClient(service, ns, context.SchemaPath, context.OutputPath);
            file.ChangeKind = FileChangeKind.Modified;
            result.Files.Add(file);
        }

        return result;
    }

    #region 值对象类型生成

    /// <summary>
    ///     生成不可变值类型（readonly interface）
    /// </summary>
    private GeneratedFile GenerateValueObjectType(StructureDefinition structDef, string ns, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));

        var imports = new HashSet<string>();
        foreach (var field in structDef.fields) CollectTypeImportsFrom(field.FieldType, imports);

        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"import {{ {imp} }} from './{imp}';");

            sb.AppendLine();
        }

        sb.AppendLine("/**");
        sb.AppendLine($" * 值对象 {structDef.Name}——不可变、按值比较、无独立标识");
        sb.AppendLine(" */");
        sb.AppendLine($"export interface {structDef.Name} {{");

        foreach (var field in structDef.fields)
        {
            var tsType = MapType(field.FieldType);
            var optional = field.IsOptional ? "?" : "";

            if (field.DocComment is not null) sb.AppendLine($"  /** {field.DocComment} */");

            sb.AppendLine($"  readonly {ToCamelCase(field.Name)}{optional}: {tsType};");
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{structDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = structDef.Name
        };
    }

    #endregion

    #region RPC 错误类型

    private GeneratedFile GenerateRpcError(string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("/**");
        sb.AppendLine(" * Hermes RPC 错误类型");
        sb.AppendLine(" */");
        sb.AppendLine("export class HermesRpcError extends Error {");
        sb.AppendLine("  readonly statusCode: number;");
        sb.AppendLine("  readonly detail?: string;");
        sb.AppendLine();
        sb.AppendLine("  constructor(statusCode: number, message: string, detail?: string) {");
        sb.AppendLine("    super(message);");
        sb.AppendLine("    this.name = 'HermesRpcError';");
        sb.AppendLine("    this.statusCode = statusCode;");
        sb.AppendLine("    this.detail = detail;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/**");
        sb.AppendLine(" * RPC 参数验证错误");
        sb.AppendLine(" */");
        sb.AppendLine("export class HermesRpcValidationError extends HermesRpcError {");
        sb.AppendLine("  readonly errors: Record<string, string[]>;");
        sb.AppendLine();
        sb.AppendLine("  constructor(errors: Record<string, string[]>) {");
        sb.AppendLine("    super(400, '参数验证失败');");
        sb.AppendLine("    this.name = 'HermesRpcValidationError';");
        sb.AppendLine("    this.errors = errors;");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "HermesRpcError.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "HermesRpcError"
        };
    }

    #endregion

    #region Index 文件生成

    private GeneratedFile GenerateIndexFile(SchemaIR schema, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));

        var exports = new List<string>();

        foreach (var classDef in schema.Classes) exports.Add(classDef.Name);

        foreach (var structDef in schema.Structures) exports.Add(structDef.Name);

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
            exports.Add(model.Name);

        foreach (var enumDef in schema.Enums) exports.Add(enumDef.Name);

        foreach (var flagsDef in schema.Flags) exports.Add(flagsDef.Name);

        foreach (var unionDef in schema.Unions) exports.Add(unionDef.Name);

        foreach (var service in schema.Services) exports.Add($"{service.Name}Client");

        exports.Add("HermesRpcError");
        exports.Add("HermesRpcValidationError");

        foreach (var export in exports) sb.AppendLine($"export {{ {export} }} from './{export}';");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, "index.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = "index"
        };
    }

    #endregion

    #region 实体类型生成

    private GeneratedFile GenerateClassInterface(ClassDefinition classDef, string ns, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));

        if (classDef.Base != null)
        {
            sb.AppendLine($"import {{ {classDef.Base.Name} }} from './{classDef.Base.Name}';");
            sb.AppendLine();
        }

        sb.AppendLine(
            $"export interface {classDef.Name}{(classDef.Base != null ? $" extends {classDef.Base.Name}" : "")} {{");

        foreach (var field in classDef.fields)
        {
            var tsType = MapType(field.FieldType);
            var optional = field.IsOptional ? "?" : "";
            sb.AppendLine($"  {ToCamelCase(field.Name)}{optional}: {tsType};");
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{classDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = classDef.Name
        };
    }

    private GeneratedFile GenerateModelInterface(ModelDefinition modelDef, string ns, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));

        var imports = CollectTypeImports(modelDef);
        if (imports.Count > 0)
        {
            foreach (var imp in imports.OrderBy(i => i)) sb.AppendLine($"import {{ {imp} }} from './{imp}';");

            sb.AppendLine();
        }

        if (modelDef.DocComment is not null) sb.AppendLine($"/** {modelDef.DocComment} */");

        sb.AppendLine($"export interface {modelDef.Name} {{");

        foreach (var field in modelDef.fields)
        {
            var tsType = MapType(field.FieldType);
            var optional = field.IsOptional ? "?" : "";

            if (field.DocComment is not null) sb.AppendLine($"  /** {field.DocComment} */");

            sb.AppendLine($"  {ToCamelCase(field.Name)}{optional}: {tsType};");
        }

        foreach (var getter in modelDef.Getters)
        {
            var tsReturnType = getter.ReturnType != null ? MapType(getter.ReturnType) : "unknown";
            var tsBody = ConvertGetterBody(getter.Body);

            if (getter.DocComment is not null) sb.AppendLine($"  /** {getter.DocComment} */");

            sb.AppendLine($"  readonly {ToCamelCase(getter.Name)}: {tsReturnType};  // computed: {tsBody}");
        }

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{modelDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = modelDef.Name
        };
    }

    #endregion

    #region Enum/Flags/Union 生成

    private GeneratedFile GenerateEnumType(EnumDefinition enumDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"export enum {enumDef.Name} {{");

        foreach (var member in enumDef.Members) sb.AppendLine($"  {member.Name} = {member.Value},");

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{enumDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = enumDef.Name
        };
    }

    private GeneratedFile GenerateFlagsType(FlagsDefinition flagsDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine($"export enum {flagsDef.Name} {{");

        foreach (var member in flagsDef.Members) sb.AppendLine($"  {member.Name} = 1 << {member.Value},");

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{flagsDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = flagsDef.Name
        };
    }

    private GeneratedFile GenerateUnionType(UnionDefinition unionDef, string ns, string schemaPath, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));

        var variantTypes = new List<string>();
        foreach (var variant in unionDef.Variants)
            if (variant.Payload is null)
            {
                variantTypes.Add($"{{ kind: '{variant.Name}' }}");
            }
            else
            {
                sb.AppendLine($"export interface {unionDef.Name}{variant.Name} {{");
                sb.AppendLine($"  kind: '{variant.Name}';");
                sb.AppendLine($"  value: {MapType(variant.Payload)};");
                sb.AppendLine("}");
                sb.AppendLine();
                variantTypes.Add($"{unionDef.Name}{variant.Name}");
            }

        sb.AppendLine($"export type {unionDef.Name} =");
        for (var i = 0; i < variantTypes.Count; i++)
        {
            var suffix = i < variantTypes.Count - 1 ? " |" : ";";
            sb.AppendLine($"  {variantTypes[i]}{suffix}");
        }

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{unionDef.Name}.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = unionDef.Name
        };
    }

    #endregion

    #region RPC 客户端生成

    private GeneratedFile GenerateServiceClient(ServiceDefinition service, string ns, string schemaPath,
        string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine(GenerateFileHeader(schemaPath, outputPath));
        sb.AppendLine("import { HermesRpcError } from './HermesRpcError';");
        sb.AppendLine();

        var typeImports = new HashSet<string>();
        foreach (var endpoint in service.Endpoints) CollectEndpointTypeImports(endpoint, typeImports);

        if (typeImports.Count > 0)
        {
            foreach (var imp in typeImports.OrderBy(i => i)) sb.AppendLine($"import {{ {imp} }} from './{imp}';");

            sb.AppendLine();
        }

        sb.AppendLine("/**");
        sb.AppendLine($" * {service.Name} RPC 客户端");
        sb.AppendLine(" */");
        sb.AppendLine($"export class {service.Name}Client {{");
        sb.AppendLine("  private baseUrl: string;");
        sb.AppendLine("  private defaultHeaders: HeadersInit;");
        sb.AppendLine();
        sb.AppendLine("  constructor(baseUrl: string = '', defaultHeaders: HeadersInit = {}) {");
        sb.AppendLine("    this.baseUrl = baseUrl;");
        sb.AppendLine("    this.defaultHeaders = defaultHeaders;");
        sb.AppendLine("  }");

        foreach (var endpoint in service.Endpoints)
            if (endpoint is HttpEndpoint httpEp)
                GenerateHttpMethod(sb, httpEp);
            else if (endpoint is GrpcEndpoint grpcEp)
                GenerateGrpcMethod(sb, grpcEp);
            else if (endpoint is WsEndpoint wsEp)
                GenerateWsMethod(sb, wsEp);
            else if (endpoint is MessageEndpoint msgEp) GenerateMessageMethod(sb, msgEp);

        sb.AppendLine("}");

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{service.Name}Client.ts"),
            Content = sb.ToString(),
            Generator = Name,
            TypeName = $"{service.Name}Client"
        };
    }

    private void GenerateHttpMethod(StringBuilder sb, HttpEndpoint endpoint)
    {
        var methodName = ToCamelCase(endpoint.Name);
        var returnTsType = endpoint.ReturnType != null ? MapType(endpoint.ReturnType) : "void";
        var httpMethod = endpoint.HttpMethod.ToUpperInvariant();
        var path = endpoint.Path ?? $"/api/{ToKebabCase(endpoint.Name)}";
        var isVoid = endpoint.ReturnType == null || endpoint.ReturnType.TypeName == "unit";
        var hasBody = httpMethod is "POST" or "PUT" or "PATCH";

        sb.AppendLine();
        sb.AppendLine("  /**");
        sb.AppendLine($"   * {endpoint.Name}");
        sb.AppendLine("   */");

        var paramList = new List<string>();
        if (hasBody && endpoint.Parameters.Count > 0)
        {
            var bodyParam = endpoint.Parameters[0];
            paramList.Add($"body: {MapType(bodyParam.ParameterType)}");
        }

        var queryParams = hasBody ? endpoint.Parameters.Skip(1) : endpoint.Parameters;
        foreach (var param in queryParams)
        {
            var tsType = MapType(param.ParameterType);
            var optional = param.ParameterType is OptionType ? "?" : "";
            paramList.Add($"{ToCamelCase(param.Name)}{optional}: {tsType}");
        }

        paramList.Add("signal?: AbortSignal");

        sb.AppendLine($"  async {methodName}({string.Join(", ", paramList)}): Promise<{returnTsType}> {{");

        if (queryParams.Any())
        {
            sb.AppendLine("    const params = new URLSearchParams();");
            foreach (var param in queryParams)
            {
                var paramName = ToCamelCase(param.Name);
                sb.AppendLine(
                    $"    if ({paramName} !== undefined && {paramName} !== null) params.append('{param.Name}', String({paramName}));");
            }

            sb.AppendLine($"    const url = `${{this.baseUrl}}{path}?${{params.toString()}}`;");
        }
        else
        {
            sb.AppendLine($"    const url = `${{this.baseUrl}}{path}`;");
        }

        sb.AppendLine("    const headers: HeadersInit = { ...this.defaultHeaders };");

        if (hasBody) sb.AppendLine("    headers['Content-Type'] = 'application/json';");

        sb.AppendLine("    const response = await fetch(url, {");
        sb.AppendLine($"      method: '{httpMethod}',");
        sb.AppendLine("      headers,");

        if (hasBody && endpoint.Parameters.Count > 0) sb.AppendLine("      body: JSON.stringify(body),");

        sb.AppendLine("      signal,");
        sb.AppendLine("    });");
        sb.AppendLine();
        sb.AppendLine("    if (!response.ok) {");
        sb.AppendLine("      const errorBody = await response.text();");
        sb.AppendLine("      throw new HermesRpcError(response.status, `RPC 调用失败: ${methodName}`, errorBody);");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (isVoid)
            sb.AppendLine("    return;");
        else if (endpoint.ReturnType is PrimitiveType pt && pt.TypeName == "utf8")
            sb.AppendLine("    return await response.text();");
        else
            sb.AppendLine($"    return await response.json() as {returnTsType};");

        sb.AppendLine("  }");
    }

    private void GenerateGrpcMethod(StringBuilder sb, GrpcEndpoint endpoint)
    {
        var methodName = ToCamelCase(endpoint.Name);
        var returnTsType = endpoint.ReturnType != null ? MapType(endpoint.ReturnType) : "void";
        var modeDesc = GetStreamingModeDescription(endpoint.StreamingMode);

        sb.AppendLine();
        sb.AppendLine("  /**");
        sb.AppendLine($"   * {endpoint.Name}（gRPC {modeDesc}调用）");
        sb.AppendLine("   */");

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine($"  async {methodName}({string.Join(", ", paramList)}): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('gRPC 一元调用运行时集成待实现');");
                break;
            }
            case StreamingMode.Server:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine(
                    $"  async {methodName}({string.Join(", ", paramList)}): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('gRPC 服务端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('gRPC 客户端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('gRPC 双向流运行时集成待实现');");
                break;
            }
        }

        sb.AppendLine("  }");
    }

    private void GenerateWsMethod(StringBuilder sb, WsEndpoint endpoint)
    {
        var methodName = ToCamelCase(endpoint.Name);
        var returnTsType = endpoint.ReturnType != null ? MapType(endpoint.ReturnType) : "void";
        var modeDesc = GetStreamingModeDescription(endpoint.StreamingMode);

        sb.AppendLine();
        sb.AppendLine("  /**");
        sb.AppendLine($"   * {endpoint.Name}（WebSocket {modeDesc}调用）");
        sb.AppendLine("   */");

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine($"  async {methodName}({string.Join(", ", paramList)}): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('WebSocket 一元调用运行时集成待实现');");
                break;
            }
            case StreamingMode.Server:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine(
                    $"  async {methodName}({string.Join(", ", paramList)}): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('WebSocket 服务端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('WebSocket 客户端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('WebSocket 双向流运行时集成待实现');");
                break;
            }
        }

        sb.AppendLine("  }");
    }

    private void GenerateMessageMethod(StringBuilder sb, MessageEndpoint endpoint)
    {
        var methodName = ToCamelCase(endpoint.Name);
        var returnTsType = endpoint.ReturnType != null ? MapType(endpoint.ReturnType) : "void";
        var modeDesc = GetStreamingModeDescription(endpoint.StreamingMode);

        sb.AppendLine();
        sb.AppendLine("  /**");
        sb.AppendLine($"   * {endpoint.Name}（消息队列 {modeDesc}调用 — topic: {endpoint.Topic}）");
        sb.AppendLine("   */");

        switch (endpoint.StreamingMode)
        {
            case StreamingMode.Unary:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine($"  async {methodName}({string.Join(", ", paramList)}): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('消息队列一元调用运行时集成待实现');");
                break;
            }
            case StreamingMode.Server:
            {
                var paramList = endpoint.Parameters.Select(p => $"{ToCamelCase(p.Name)}: {MapType(p.ParameterType)}")
                    .ToList();
                paramList.Add("signal?: AbortSignal");
                sb.AppendLine(
                    $"  async {methodName}({string.Join(", ", paramList)}): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('消息队列服务端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Client:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): Promise<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('消息队列客户端流运行时集成待实现');");
                break;
            }
            case StreamingMode.Duplex:
            {
                var requestType = endpoint.Parameters.Count > 0
                    ? MapType(endpoint.Parameters[0].ParameterType)
                    : "unknown";
                sb.AppendLine(
                    $"  async {methodName}(requests: AsyncIterable<{requestType}>, signal?: AbortSignal): AsyncIterable<{returnTsType}> {{");
                sb.AppendLine("    throw new Error('消息队列双向流运行时集成待实现');");
                break;
            }
        }

        sb.AppendLine("  }");
    }

    /// <summary>
    ///     获取流式模式的中文描述
    /// </summary>
    private static string GetStreamingModeDescription(StreamingMode mode)
    {
        return mode switch
        {
            StreamingMode.Unary => "一元",
            StreamingMode.Server => "服务端流",
            StreamingMode.Client => "客户端流",
            StreamingMode.Duplex => "双向流",
            _ => ""
        };
    }

    #endregion

    #region 类型映射

    private string MapType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" or "i16" or "i32" or "i64" => "number",
                "u8" or "u16" or "u32" or "u64" => "number",
                "f32" or "f64" => "number",
                "bool" => "boolean",
                "utf8" or "utf16" => "string",
                "uuid" => "string",
                "unit" => "void",
                "object" => "Record<string, unknown>",
                _ => "unknown"
            },
            ListType l => $"Array<{MapType(l.ElementType)}>",
            ArrayType a => $"Array<{MapType(a.ElementType)}>",
            OptionType o => $"{MapType(o.InnerType)} | null",
            DictType d => $"Record<{MapType(d.KeyType)}, {MapType(d.ValueType)}>",
            RecordType r => $"Record<{MapType(r.KeyType)}, {MapType(r.ValueType)}>",
            ResultType r => r.ErrorType != null
                ? $"{{ ok: {MapType(r.OkType)}; error: {MapType(r.ErrorType)} }}"
                : $"{{ ok: {MapType(r.OkType)}; error?: string }}",
            StreamType s => $"AsyncIterable<{MapType(s.InnerType)}>",
            ReferenceType refT => MapType(refT.ReferencedType),
            NamedType n => n.Name switch
            {
                "datetime" => "string",
                "decimal" => "number",
                "bytes" => "Uint8Array",
                "any" => "unknown",
                _ => n.Name
            },
            _ => "unknown"
        };
    }

    private HashSet<string> CollectTypeImports(ModelDefinition model)
    {
        var imports = new HashSet<string>();
        foreach (var field in model.fields) CollectTypeImportsFrom(field.FieldType, imports);

        return imports;
    }

    private void CollectTypeImportsFrom(SchemaType type, HashSet<string> imports)
    {
        switch (type)
        {
            case NamedType n when n.Name is not ("string" or "number" or "boolean" or "void" or "unknown" or "datetime"
                or "decimal" or "bytes" or "any"):
                imports.Add(n.Name);
                break;
            case ListType l:
                CollectTypeImportsFrom(l.ElementType, imports);
                break;
            case ArrayType a:
                CollectTypeImportsFrom(a.ElementType, imports);
                break;
            case OptionType o:
                CollectTypeImportsFrom(o.InnerType, imports);
                break;
            case DictType d:
                CollectTypeImportsFrom(d.KeyType, imports);
                CollectTypeImportsFrom(d.ValueType, imports);
                break;
            case RecordType r:
                CollectTypeImportsFrom(r.KeyType, imports);
                CollectTypeImportsFrom(r.ValueType, imports);
                break;
            case ResultType r:
                CollectTypeImportsFrom(r.OkType, imports);
                if (r.ErrorType != null) CollectTypeImportsFrom(r.ErrorType, imports);

                break;
            case StreamType s:
                CollectTypeImportsFrom(s.InnerType, imports);
                break;
            case ReferenceType refT:
                CollectTypeImportsFrom(refT.ReferencedType, imports);
                break;
        }
    }

    private void CollectEndpointTypeImports(EndpointDefinition endpoint, HashSet<string> imports)
    {
        foreach (var param in endpoint.Parameters) CollectTypeImportsFrom(param.ParameterType, imports);

        if (endpoint.ReturnType != null) CollectTypeImportsFrom(endpoint.ReturnType, imports);
    }

    #endregion

    #region 工具方法

    private static string GetNamespace(IReadOnlyDictionary<string, object> options)
    {
        return options.TryGetValue("namespace", out var nsVal) ? nsVal.ToString() ?? "Models" : "Models";
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var parts = name.Split('_');
        if (parts.Length == 1) return char.ToLowerInvariant(name[0]) + name[1..];

        var sb = new StringBuilder();
        sb.Append(parts[0].ToLowerInvariant());
        for (var i = 1; i < parts.Length; i++)
            if (parts[i].Length > 0)
            {
                sb.Append(char.ToUpperInvariant(parts[i][0]));
                sb.Append(parts[i][1..].ToLowerInvariant());
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

    private static string ConvertGetterBody(string hermesBody)
    {
        var parts = hermesBody.Split('.');
        var tsParts = new List<string>();
        for (var i = 0; i < parts.Length; i++)
            if (i == 0 && parts[i] == "self")
                tsParts.Add("this");
            else
                tsParts.Add(ToCamelCase(parts[i]));

        return string.Join(".", tsParts);
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

    #endregion
}