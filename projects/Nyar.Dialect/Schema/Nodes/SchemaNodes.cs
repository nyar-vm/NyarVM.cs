using Nyar.IR.Intent;

namespace Nyar.Dialect.Schema.Nodes;

[AlgebraNode]
public sealed partial record SchemaModel(string name, Id keyType, Id fields) : AlgebraNode;

[AlgebraNode]
public sealed partial record SchemaField(string name, Id fieldType, bool isOptional) : AlgebraNode;

[AlgebraNode]
public sealed partial record SchemaStorage(string name, Id models, Id streams, Id caches) : AlgebraNode;

[AlgebraNode]
public sealed partial record SchemaService(string name, Id endpoints) : AlgebraNode;

[AlgebraNode]
public sealed partial record SchemaHttpEndpoint(string method, string path, Id parameters, Id returnType) : AlgebraNode;

[AlgebraNode]
public sealed partial record SchemaGrpcEndpoint(string serviceName, string methodName, Id parameters, Id returnType)
    : AlgebraNode;