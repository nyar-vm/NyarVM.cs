namespace Std.Net.Http;

/// <summary>
///     HTTP 状态码�?///
/// </summary>
public enum HttpStatusCode
{
    /// <summary>
    ///     200 OK
    /// </summary>
    ok = 200,

    /// <summary>
    ///     201 Created
    /// </summary>
    created = 201,

    /// <summary>
    ///     204 No Content
    /// </summary>
    no_content = 204,

    /// <summary>
    ///     301 Moved Permanently
    /// </summary>
    moved_permanently = 301,

    /// <summary>
    ///     302 Found
    /// </summary>
    found = 302,

    /// <summary>
    ///     304 Not Modified
    /// </summary>
    not_modified = 304,

    /// <summary>
    ///     400 Bad Request
    /// </summary>
    bad_request = 400,

    /// <summary>
    ///     401 Unauthorized
    /// </summary>
    unauthorized = 401,

    /// <summary>
    ///     403 Forbidden
    /// </summary>
    forbidden = 403,

    /// <summary>
    ///     404 Not Found
    /// </summary>
    not_found = 404,

    /// <summary>
    ///     405 Method Not Allowed
    /// </summary>
    method_not_allowed = 405,

    /// <summary>
    ///     409 Conflict
    /// </summary>
    conflict = 409,

    /// <summary>
    ///     422 Unprocessable Entity
    /// </summary>
    unprocessable_entity = 422,

    /// <summary>
    ///     429 Too Many Requests
    /// </summary>
    too_many_requests = 429,

    /// <summary>
    ///     500 Internal Server Error
    /// </summary>
    internal_server_error = 500,

    /// <summary>
    ///     502 Bad Gateway
    /// </summary>
    bad_gateway = 502,

    /// <summary>
    ///     503 Service Unavailable
    /// </summary>
    service_unavailable = 503,

    /// <summary>
    ///     101 Switching Protocols
    /// </summary>
    switching_protocols = 101
}