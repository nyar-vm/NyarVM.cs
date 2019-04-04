namespace Nyar.Language.Valkyrie.Compiler.Hir.Attributes;

/// <summary>
///     `[data]` 的最小派生请求。
///     这里严格区分：
///     1. `data_type_kind` 表示结构类型（如 `structure` / `class`）。
///     2. `container_kind` 表示中性的结构化容器形状。
///     3. 文本格式（如 `json` / `toml`）不属于该层。
/// </summary>
public sealed record HirDataDeriveRequest
{
    public HirDataDeriveOperationKind operation_kind { get; init; }
    public string requesting_callable_name { get; init; }
    public string data_type_name { get; init; }
    public HirTypeKind data_type_kind { get; init; }
    public HirDataContainerKind container_kind { get; init; }

    
    /// <summary>
    ///     `[data]` 的最小派生请求。
    ///     这里严格区分：
    ///     1. `data_type_kind` 表示结构类型（如 `structure` / `class`）。
    ///     2. `container_kind` 表示中性的结构化容器形状。
    ///     3. 文本格式（如 `json` / `toml`）不属于该层。
    /// </summary>
    public HirDataDeriveRequest(HirDataDeriveOperationKind operation_kind,
        string requesting_callable_name,
        string data_type_name,
        HirTypeKind data_type_kind,
        HirDataContainerKind container_kind)
    {
        this.operation_kind = operation_kind;
        this.requesting_callable_name = requesting_callable_name;
        this.data_type_name = data_type_name;
        this.data_type_kind = data_type_kind;
        this.container_kind = container_kind;
    }



}