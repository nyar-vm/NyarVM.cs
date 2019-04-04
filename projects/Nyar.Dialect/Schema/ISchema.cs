using Nyar.ObjectAlgebra;

namespace Nyar.Dialect.Schema;

/// <summary>
///     Schema 方言的 OA 接口定义。
///     该接口声明模式、字段、存储与服务端点等结构化描述操作。
/// </summary>
[Dialect("schema")]
public interface ISchema<T>
{
    /// <summary>
    ///     声明模式模型。
    /// </summary>
    [Operator("schema_model")]
    Term<T> SchemaModel(string name, Term<T> keyType, Term<T> fields);

    /// <summary>
    ///     声明模式字段。
    /// </summary>
    [Operator("schema_field")]
    Term<T> SchemaField(string name, Term<T> fieldType, bool isOptional);

    /// <summary>
    ///     声明模式存储。
    /// </summary>
    [Operator("schema_storage")]
    Term<T> SchemaStorage(string name, Term<T> models, Term<T> streams, Term<T> caches);

    /// <summary>
    ///     声明模式服务。
    /// </summary>
    [Operator("schema_service")]
    Term<T> SchemaService(string name, Term<T> endpoints);

    /// <summary>
    ///     声明 HTTP 端点。
    /// </summary>
    [Operator("schema_http_endpoint")]
    Term<T> SchemaHttpEndpoint(string method, string path, Term<T> parameters, Term<T> returnType);

    /// <summary>
    ///     声明 gRPC 端点。
    /// </summary>
    [Operator("schema_grpc_endpoint")]
    Term<T> SchemaGrpcEndpoint(string serviceName, string methodName, Term<T> parameters, Term<T> returnType);
}